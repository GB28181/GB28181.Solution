//-----------------------------------------------------------------------------
// Filename: SIPTransport.cs
//
// Description: SIP transport layer implementation. Handles different network
// transport options, retransmits, timeouts and transaction matching.
// 
// History:
// 14 Feb 2006	Aaron Clauson	Created.
// 26 Apr 2008  Aaron Clauson   Added TCP support.
// 30 May 2020	Edward Chen     Updated.
// 06 Sep 2020  Edward Chen     Refactoring DNS
// License: 
// BSD 3-Clause "New" or "Revised" License, see included LICENSE.md file.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using GB28181.Logger4Net;
using GB28181.Sys;
using SIPSorcery.SIP;
using SIPSorcery.Sys;

namespace GB28181
{

    public class SIPTransport : SIPSorcery.SIP.SIPTransport, ISIPTransport
    {
        private const string RECEIVE_THREAD_NAME = "siptransport-receive";
        private const string RELIABLES_THREAD_NAME = "siptransport-reliables";
        private const int MAX_QUEUEWAIT_PERIOD = 2000;              // Maximum time to wait to check the message received queue if no events are received.
        private const int PENDINGREQUESTS_CHECK_PERIOD = 500;       // Time between checking the pending requests queue to resend reliable requests that have not been responded to.
        private const int MAX_INMESSAGE_QUEUECOUNT = 5000;          // The maximum number of messages that can be stored in the incoming message queue.
        private const int MAX_RELIABLETRANSMISSIONS_COUNT = 5000;   // The maximum number of messages that can be maintained for reliable transmissions.

        public const string ALLOWED_SIP_METHODS = "ACK, BYE, CANCEL, INFO, INVITE, NOTIFY, OPTIONS, REFER, REGISTER, SUBSCRIBE";

        private static readonly int m_t1 = SIPTimings.T1;
        private static readonly int m_t2 = SIPTimings.T2;
        private static readonly int m_t6 = SIPTimings.T6;
        private static string m_looseRouteParameter = SIPConstants.SIP_LOOSEROUTER_PARAMETER;
                          
        private static ILog logger = AssemblyState.logger;

        private bool m_queueIncoming = true;     // Dictates whether the transport later will queue incoming requests for processing on a separate thread of process immediately on the same thread.
        // Most SIP elements with the exception of Stateless Proxies would typically want to queue incoming SIP messages.

        private bool m_transportThreadStarted = false;
        private Queue<IncomingMessage> m_inMessageQueue = new Queue<IncomingMessage>();
        private ManualResetEvent m_inMessageArrived = new ManualResetEvent(false);
        private bool m_closed = false;

        private Dictionary<string, SIPChannel> m_sipChannels = new Dictionary<string, SIPChannel>();    // List of the physical channels that have been opened and are under management by this instance.


        private ISIPTransactionEngine _transactionEngine;
        public event SIPTransportRequestDelegate SIPTransportRequestReceived;
        public event SIPTransportResponseDelegate SIPTransportResponseReceived;
        public event STUNRequestReceivedDelegate STUNRequestReceived;
        //  private ResolveSIPEndPointDelegate ResolveSIPEndPoint_External = App.SIPDNSManager.ResolveSIPService;

        public event SIPTransportRequestDelegate SIPRequestInTraceEvent;
        public event SIPTransportRequestDelegate SIPRequestOutTraceEvent;
        public event SIPTransportResponseDelegate SIPResponseInTraceEvent;
        public event SIPTransportResponseDelegate SIPResponseOutTraceEvent;
        public event SIPTransportSIPBadMessageDelegate SIPBadRequestInTraceEvent;
        public event SIPTransportSIPBadMessageDelegate SIPBadResponseInTraceEvent;

        public string PerformanceMonitorPrefix { get; set; }                            // Allows an application to set the prefix for the performance monitor counter it wants to use for tracking the SIP transport metrics.
        public string MsgEncode { get; set; }

        public IPAddress ContactIPAddress;          // If set this address will be passed to the UAS Invite transaction so it can be used as the Contact address in Ok responses.

        // Contains a list of the SIP Requests/Response that are being monitored or responses and retransmitted on when none is recieved to attempt a more reliable delivery
        // rather then just relying on the initial request to get through.
        private Dictionary<string, SIPTransaction> m_reliableTransmissions = new Dictionary<string, SIPTransaction>();
        private bool m_reliablesThreadRunning = false;   // Only gets started when a request is made to send a reliable request.

        public int ReliableTrasmissionsCount => m_reliableTransmissions.Count;

        public SIPTransport(ISIPTransactionEngine transactionEngine) : base()
        {
            //  ResolveSIPEndPoint_External = sipResolver ?? throw new ArgumentNullException("The SIP end point resolver must be set when creating a SIPTransport object.");
            _transactionEngine = transactionEngine;
        }

        public SIPTransport(ISIPTransactionEngine transactionEngine, bool queueIncoming = false) : base()
        {
            //ResolveSIPEndPoint_External = sipResolver ?? throw new ArgumentNullException("The SIP end point resolver must be set when creating a SIPTransport object.");
            _transactionEngine = transactionEngine;
            m_queueIncoming = queueIncoming;
        }


        public SIPTransport(SIPTransactionEngine transactionEngine, bool queueIncoming) : base()
        {
            _transactionEngine = transactionEngine;
            m_queueIncoming = queueIncoming;
        }


        public SIPTransport(ISIPTransactionEngine transactionEngine, SIPChannel sipChannel, bool queueIncoming) : base()
        {
            //  ResolveSIPEndPoint_External = sipResolver ?? throw new ArgumentNullException("The SIP end point resolver must be set when creating a SIPTransport object.");
            _transactionEngine = transactionEngine;
            AddSIPChannel(sipChannel);

            m_queueIncoming = queueIncoming;
        }

        /// <summary>
        /// Adds additional SIP Channels to the transport layer.
        /// </summary>
        public void AddSIPChannel(List<SIPChannel> sipChannels)
        {
            foreach (SIPChannel sipChannel in sipChannels)
            {
                AddSIPChannel(sipChannel);
            }
        }

        /// <summary>
        /// Adds an additional SIP Channel to the transport layer.
        /// </summary>
        /// <param name="localEndPoint"></param>
        public void AddSIPChannel(SIPChannel sipChannel)
        {
            try
            {
                m_sipChannels.Add(sipChannel.SIPChannelEndPoint.ToString(), sipChannel);

                // Wire up the SIP transport to the SIP channel.
                sipChannel.SIPMessageReceived += ReceiveMessage;

                if (m_queueIncoming && !m_transportThreadStarted)
                {
                    StartTransportThread();
                }
            }
            catch (Exception excp)
            {
                logger.Error("Exception AddSIPChannel. " + excp.Message);
                throw excp;
            }
        }

        public void RemoveSIPChannel(SIPChannel sipChannel)
        {
            if (m_sipChannels.ContainsKey(sipChannel.SIPChannelEndPoint.ToString()))
            {
                m_sipChannels.Remove(sipChannel.SIPChannelEndPoint.ToString());
                sipChannel.SIPMessageReceived -= ReceiveMessage;
            }
        }

        private void StartTransportThread()
        {
            if (!m_transportThreadStarted)
            {
                m_transportThreadStarted = true;

                var inMessageThread = new Thread(() => ProcessInMessage())
                {
                    Name = RECEIVE_THREAD_NAME
                };
                inMessageThread.Start();
            }
        }

        private void StartReliableTransmissionsThread()
        {
            m_reliablesThreadRunning = true;

            var reliableTransmissionsThread = new Thread(() => ProcessPendingReliableTransactions())
            {
                Name = RELIABLES_THREAD_NAME
            };
            reliableTransmissionsThread.Start();
        }

        public void ReceiveMessage(SIPChannel sipChannel, SIPEndPoint remoteEndPoint, byte[] buffer)
        {
            try
            {
                if (!m_queueIncoming)
                {
                    SIPMessageReceived(sipChannel, remoteEndPoint, buffer);
                }
                else
                {
                    var incomingMessage = new IncomingMessage(sipChannel, remoteEndPoint, buffer);

                    // Keep the queue within size limits 
                    if (m_inMessageQueue.Count >= MAX_INMESSAGE_QUEUECOUNT)
                    {
                        logger.Warn("SIPTransport queue full new message from " + remoteEndPoint + " being discarded.");
                    }
                    else
                    {
                        lock (m_inMessageQueue)
                        {
                            m_inMessageQueue.Enqueue(incomingMessage);
                        }
                    }

                    m_inMessageArrived.Set();
                }
            }
            catch (Exception excp)
            {
                logger.Error("Exception SIPTransport ReceiveMessage. " + excp.Message);
                throw excp;
            }
        }

        public void Shutdown()
        {
            try
            {
                m_closed = true;

                foreach (SIPChannel channel in m_sipChannels.Values)
                {
                    channel.Close();
                }

                m_inMessageArrived.Set();
                m_inMessageArrived.Set();

                logger.Debug("SIPTransport Shutdown Complete.");
            }
            catch (Exception excp)
            {
                logger.Error("Exception SIPTransport Shutdown. " + excp.Message);
            }
        }

        public SIPEndPoint GetDefaultTransportContact(SIPProtocolsEnum protocol)
        {
            SIPChannel defaultChannel = GetDefaultChannel(protocol);

            if (defaultChannel != null)
            {
                return defaultChannel.SIPChannelEndPoint;
            }
            else
            {
                return null;
            }
        }

        public SIPEndPoint GetDefaultSIPEndPoint()
        {
            foreach (SIPChannel sipChannel in m_sipChannels.Values)
            {
                if (sipChannel.SIPChannelEndPoint.Protocol == SIPProtocolsEnum.udp)
                {
                    return sipChannel.SIPChannelEndPoint;
                }
            }

            return m_sipChannels.First().Value.SIPChannelEndPoint;
        }

        public SIPEndPoint GetDefaultSIPEndPoint(SIPProtocolsEnum protocol)
        {
            foreach (SIPChannel sipChannel in m_sipChannels.Values)
            {
                if (sipChannel.SIPChannelEndPoint.Protocol == protocol)
                {
                    return sipChannel.SIPChannelEndPoint;
                }
            }

            return null;
        }

        public SIPEndPoint GetDefaultSIPEndPoint(SIPEndPoint destinationEP)
        {
            bool isDestLoopback = IPAddress.IsLoopback(destinationEP.Address);

            foreach (SIPChannel sipChannel in m_sipChannels.Values)
            {
                if (sipChannel.SIPChannelEndPoint.Protocol == destinationEP.Protocol)
                {
                    if (isDestLoopback)
                    {
                        if (IPAddress.IsLoopback(sipChannel.SIPChannelEndPoint.Address))
                        {
                            return sipChannel.SIPChannelEndPoint;
                        }
                    }
                    else
                    {
                        return sipChannel.SIPChannelEndPoint;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// This function performs processing on a request to handle any actions that need to be taken based on the Route header.
        /// </summary>
        /// <remarks>
        /// The main sections in the RFC3261 dealing with Route header processing are sections 12.2.1.1 for request processing and
        /// 16.4 for proxy processing.
        /// The steps to process requests for Route headers are:
        ///  1. If route set is empty no further action is required, forward to destination resolved from request URI,
        ///  2. If the request URI is identified as a value that was previously set as a Route by this SIP agent it means the
        ///     previous hop was a strict router. Replace the reqest URI with the last Route header and go to next step,
        ///  3. If the top most route header was set by this SIP agent then remove it and go to next step,
        ///  4. If the top most route set does contain the lr parameter then forward to the destination resolved by it,
        ///  5. If the top most route header does NOT contain the lr parameter is must be popped and inserted as the request URI
        ///     and the original request URI must be added to the end of the route set, forward to destination resolved from request URI,
        /// </remarks>
        public void PreProcessRouteInfo(SIPRequest sipRequest)
        {
            // If there are no routes defined then there is nothing to do.
            if (sipRequest.Header.Routes != null && sipRequest.Header.Routes.Length > 0)
            {
                // If this stack's route URI is being used as the request URI then it will have the loose route parameter (see remarks step 2).
                if (sipRequest.URI.Parameters.Has(m_looseRouteParameter))
                {
                    foreach (SIPChannel sipChannel in m_sipChannels.Values)
                    {
                        if (sipRequest.URI.ToSIPEndPoint() == sipChannel.SIPChannelEndPoint)
                        {
                            // The request URI was this router's address so it was set by a strict router.
                            // Replace the URI with the original SIP URI that is stored at the end of the route header.
                            sipRequest.URI = sipRequest.Header.Routes.BottomRoute.URI;
                            sipRequest.Header.Routes.RemoveBottomRoute();
                        }
                    }
                }

                // The possibility of a strict router on the previous hop has now been handled. 
                if (sipRequest.Header.Routes != null && sipRequest.Header.Routes.Length > 0)
                {
                    // Check whether the top route header belongs to this proxy (see remarks step 3).
                    if (!sipRequest.Header.Routes.TopRoute.IsStrictRouter)
                    {
                        foreach (SIPChannel sipChannel in m_sipChannels.Values)
                        {
                            if (sipRequest.Header.Routes.TopRoute.ToSIPEndPoint() == sipChannel.SIPChannelEndPoint)
                            {
                                // Remove the top route as it belongs to this proxy.
                                sipRequest.ReceivedRoute = sipRequest.Header.Routes.PopRoute();
                                break;
                            }
                        }
                    }

                    // Check whether the top route header is a strict router and if so adjust the request accordingly (see remarks step 5).
                    if (sipRequest.Header.Routes != null && sipRequest.Header.Routes.Length > 0)
                    {
                        if (sipRequest.Header.Routes.TopRoute.IsStrictRouter)
                        {
                            // Put the strict router's uri into the request URI and place the original request URI at the end of the route set.
                            SIPRoute strictRoute = sipRequest.Header.Routes.PopRoute();
                            SIPRoute uriRoute = new SIPRoute(sipRequest.URI);
                            sipRequest.Header.Routes.AddBottomRoute(uriRoute);
                            sipRequest.URI = strictRoute.URI;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Allows raw bytes to be sent from one of the SIPTransport sockets. This should not be used for SIP payloads and instead is
        /// provided to allow other types of payloads to be multi-plexed on the SIP socket. Examples are sending NAT keep alives and
        /// STUN responses where it's useful to use the same socket as the SIP packets.
        /// </summary>
        public void SendRaw(SIPEndPoint localSIPEndPoint, SIPEndPoint destinationEndPoint, byte[] buffer)
        {
            if (destinationEndPoint != null && destinationEndPoint.Address.Equals(BlackholeAddress))
            {
                // Ignore packet, it's destined for the blackhole.
                return;
            }

            if (m_sipChannels.Count == 0)
            {
                throw new ApplicationException("No channels are configured in the SIP transport layer. The data could not be sent.");
            }

            SIPChannel sendSIPChannel = FindSIPChannel(localSIPEndPoint);
            if (sendSIPChannel != null)
            {
                sendSIPChannel.Send(destinationEndPoint.GetIPEndPoint(), buffer);
            }
            else
            {
                logger.Warn("No SIPChannel could be found for " + localSIPEndPoint + " in SIPTransport.SendRaw, sending to " + destinationEndPoint.ToString() + ".");
                //logger.Warn(Encoding.UTF8.GetString(buffer));
            }
        }

        public void SendRequest(SIPRequest sipRequest)
        {
            SendRequestAsync(sipRequest);
        }

        public void SendRequest(SIPEndPoint dstEndPoint, SIPRequest sipRequest)
        {
            if (dstEndPoint != null && dstEndPoint.Address.Equals(BlackholeAddress))
            {
                // Ignore packet, it's destined for the blackhole.
                return;
            }

            if (m_sipChannels.Count == 0)
            {
                throw new ApplicationException("No channels are configured in the SIP transport layer. The request could not be sent.");
            }

            SIPChannel sipChannel = null;

            if (sipRequest.LocalSIPEndPoint != null)
            {
                sipChannel = FindSIPChannel(sipRequest.LocalSIPEndPoint);
                sipChannel = sipChannel ?? GetDefaultChannel(sipRequest.LocalSIPEndPoint.Protocol);
            }
            else
            {
                sipChannel = GetDefaultChannel(dstEndPoint.Protocol);
            }

            if (sipChannel != null)
            {
                SendRequest(sipChannel, dstEndPoint, sipRequest);
            }
            else
            {
                throw new ApplicationException("A default SIP channel could not be found for protocol " + sipRequest.LocalSIPEndPoint.Protocol + " when sending SIP request.");
            }
        }

        public static byte[] ConvertUnicodeToUTF8(string message)
        {

            var utf8 = Encoding.GetEncoding("utf-8");
            byte[] array = Encoding.GetEncoding("gb2312").GetBytes(message);
            //byte[] s4 = System.Text.Encoding.Convert(System.Text.Encoding.GetEncoding("gb2312"), System.Text.Encoding.UTF8, array);
            return array;

        }

        private void SendRequest(SIPChannel sipChannel, SIPEndPoint dstEndPoint, SIPRequest sipRequest)
        {
            try
            {
                if (dstEndPoint != null && dstEndPoint.Address.Equals(BlackholeAddress))
                {
                    // Ignore packet, it's destined for the blackhole.
                    return;
                }

                if (m_sipChannels.Count == 0)
                {
                    throw new ApplicationException("No channels are configured in the SIP transport layer. The request could not be sent.");
                }

                sipRequest.Header.ContentLength = (sipRequest.Body.NotNullOrBlank()) ? Encoding.UTF8.GetByteCount(sipRequest.Body) : 0;

                if (sipChannel.IsTLS)
                {
                    sipChannel.Send(dstEndPoint.GetIPEndPoint(), Encoding.UTF8.GetBytes(sipRequest.ToString()), sipRequest.URI.Host);
                }
                else
                {
                    sipChannel.Send(dstEndPoint.GetIPEndPoint(), Encoding.UTF8.GetBytes(sipRequest.ToString()));

                }

                if (SIPRequestOutTraceEvent != null)
                {
                    FireSIPRequestOutTraceEvent(sipChannel.SIPChannelEndPoint, dstEndPoint, sipRequest);
                }
            }
            catch (ApplicationException appExcp)
            {
                logger.Warn("ApplicationException SIPTransport SendRequest. " + appExcp.Message);

                SIPResponse errorResponse = GetResponse(sipRequest, SIPResponseStatusCodesEnum.InternalServerError, appExcp.Message);

                // Remove any Via headers, other than the last one, that are for sockets hosted by this process.
                while (errorResponse.Header.Vias.Length > 0)
                {
                    if (IsLocalSIPEndPoint(SIPEndPoint.ParseSIPEndPoint(errorResponse.Header.Vias.TopViaHeader.ReceivedFromAddress)))
                    {
                        errorResponse.Header.Vias.PopTopViaHeader();
                    }
                    else
                    {
                        break;
                    }
                }

                if (errorResponse.Header.Vias.Length == 0)
                {
                    logger.Warn("Could not send error response for " + appExcp.Message + " as no non-local Via headers were available.");
                }
                else
                {
                    SendResponse(errorResponse);
                }
            }
        }

        /// <summary>
        /// Sends a SIP request/response and keeps track of whether a response/acknowledgement has been received. If no response is received then periodic retransmits are made
        /// for up to T1 x 64 seconds.
        /// </summary>
        public void SendSIPReliable(SIPTransaction sipTransaction)
        {
            if (sipTransaction.RemoteEndPoint != null && sipTransaction.RemoteEndPoint.Address.Equals(BlackholeAddress))
            {
                sipTransaction.Retransmits = 1;
                sipTransaction.InitialTransmit = DateTime.Now;
                sipTransaction.LastTransmit = DateTime.Now;
                sipTransaction.DeliveryPending = false;
                return;
            }

            if (m_sipChannels.Count == 0)
            {
                throw new ApplicationException("No channels are configured in the SIP transport layer. The request could not be sent.");
            }
            else if (m_reliableTransmissions.Count >= MAX_RELIABLETRANSMISSIONS_COUNT)
            {
                throw new ApplicationException("Cannot send reliable SIP message as the reliable transmissions queue is full.");
            }

            //logger.Debug("SendSIPReliable transaction URI " + sipTransaction.TransactionRequest.URI.ToString() + ".");

            if (sipTransaction.TransactionType == SIPTransactionTypesEnum.InviteServer &&
                sipTransaction.TransactionState == SIPTransactionStatesEnum.Completed)
            {
                // This is an INVITE transaction that wants to send a reliable response.
                if (sipTransaction.LocalSIPEndPoint == null)
                {
                    throw new ApplicationException("The SIPTransport layer cannot send a reliable SIP response because the send from socket has not been set for the transaction.");
                }
                else
                {
                    SIPViaHeader topViaHeader = sipTransaction.TransactionFinalResponse.Header.Vias.TopViaHeader;
                    SendResponse(sipTransaction.TransactionFinalResponse);
                }
            }
            else
            {
                if (sipTransaction.OutboundProxy != null)
                {
                    SendRequest(sipTransaction.OutboundProxy, sipTransaction.TransactionRequest);
                }
                else if (sipTransaction.RemoteEndPoint != null)
                {
                    SendRequest(sipTransaction.RemoteEndPoint, sipTransaction.TransactionRequest);
                }
                else
                {
                    SendRequest(sipTransaction.TransactionRequest);
                }
            }

            sipTransaction.Retransmits = 1;
            sipTransaction.InitialTransmit = DateTime.Now;
            sipTransaction.LastTransmit = DateTime.Now;
            sipTransaction.DeliveryPending = true;

            if (!m_reliableTransmissions.ContainsKey(sipTransaction.TransactionId))
            {
                lock (m_reliableTransmissions)
                {
                    m_reliableTransmissions.Add(sipTransaction.TransactionId, sipTransaction);
                }
            }

            if (!m_reliablesThreadRunning)
            {
                StartReliableTransmissionsThread();
            }
        }

        public void SendResponse(SIPEndPoint dstEndPoint, SIPResponse sipResponse)
        {
            if (dstEndPoint != null && dstEndPoint.Address.Equals(BlackholeAddress))
            {
                // Ignore packet, it's destined for the blackhole.
            }
            else
            {
                if (m_sipChannels.Count == 0)
                {
                    throw new ApplicationException("No channels are configured in the SIP transport layer. The response could not be sent.");
                }

                SIPChannel sipChannel = FindSIPChannel(sipResponse.LocalSIPEndPoint);
                sipChannel = sipChannel ?? GetDefaultChannel(dstEndPoint.Protocol);

                if (sipChannel != null)
                {
                    SendResponse(sipChannel, dstEndPoint, sipResponse);
                }
                else
                {
                    logger.Warn("Could not find channel to send SIP Response in SendResponse.");
                }
            }
        }

        public void SendResponse(SIPResponse sipResponse)
        {
            if (sipResponse.LocalSIPEndPoint != null && sipResponse.LocalSIPEndPoint.Address.Equals(BlackholeAddress))
            {
                // Ignore packet, it's destined for the blackhole.
                return;
            }
            if (m_sipChannels.Count == 0)
            {
                throw new ApplicationException("No channels are configured in the SIP transport layer. The response could not be sent.");
            }

            //SIPChannel sipChannel = GetChannelForSocketId(sipResponse.SocketId);
            SIPViaHeader topViaHeader = sipResponse.Header.Vias.TopViaHeader;
            if (topViaHeader == null)
            {
                logger.Warn("There was no top Via header on a SIP response from " + sipResponse.RemoteSIPEndPoint + " when attempting to send it, response dropped.");
                //logger.Warn(sipResponse.ToString());
            }
            else
            {
                var sipChannel = FindSIPChannel(sipResponse.LocalSIPEndPoint) ?? GetDefaultChannel(topViaHeader.Transport);

                if (sipChannel != null)
                {
                    var callURI = SIPURI.ParseSIPURI(sipResponse.Header.Vias.TopViaHeader.ReceivedFromAddress);
                    SIPEndPoint lookupResult = ResolveSIPUriAsync(callURI).GetAwaiter().GetResult();
                    SendResponse(sipChannel, lookupResult, sipResponse);
                }
                else
                {
                    throw new ApplicationException("Could not find a SIP channel to send SIP Response to " + topViaHeader.ReceivedFromAddress + ".");
                }
            }
        }


        private void SendResponse(SIPChannel sipChannel, SIPEndPoint dstEndPoint, SIPResponse sipResponse)
        {
            try
            {
                if (dstEndPoint != null && dstEndPoint.Address.Equals(BlackholeAddress))
                {
                    // Ignore packet, it's destined for the blackhole.
                    return;
                }

                if (m_sipChannels.Count == 0)
                {
                    throw new ApplicationException("No channels are configured in the SIP transport layer. The response could not be sent.");
                }

                sipResponse.Header.ContentLength = (sipResponse.Body.NotNullOrBlank()) ? Encoding.UTF8.GetByteCount(sipResponse.Body) : 0;
                sipChannel.Send(dstEndPoint.GetIPEndPoint(), Encoding.UTF8.GetBytes(sipResponse.ToString()));

                if (SIPRequestOutTraceEvent != null)
                {
                    FireSIPResponseOutTraceEvent(sipChannel.SIPChannelEndPoint, dstEndPoint, sipResponse);
                }
            }
            catch (ApplicationException appExcp)
            {
                logger.Warn("ApplicationException SIPTransport SendResponse. " + appExcp.Message);
            }
        }

        private void ProcessInMessage()
        {
            try
            {
                while (!m_closed)
                {
                    _transactionEngine.RemoveExpiredTransactions();

                    while (m_inMessageQueue.Count > 0)
                    {
                        IncomingMessage incomingMessage = null;

                        lock (m_inMessageQueue)
                        {
                            incomingMessage = m_inMessageQueue.Dequeue();
                        }

                        if (incomingMessage != null)
                        {
                            SIPMessageReceived(incomingMessage.LocalSIPChannel, incomingMessage.RemoteEndPoint, incomingMessage.Buffer);
                        }
                    }

                    m_inMessageArrived.Reset();
                    //m_inMessageArrived.WaitOne(MAX_QUEUEWAIT_PERIOD, false);
                    m_inMessageArrived.WaitOne(MAX_QUEUEWAIT_PERIOD);
                }

                logger.Warn("SIPTransport process received messsages thread stopped.");
            }
            catch (Exception excp)
            {
                logger.Error("Exception SIPTransport ProcessInMessage. " + excp.Message);
            }
        }


        private void ProcessPendingReliableTransactions()
        {
            try
            {
                m_reliablesThreadRunning = true;

                while (!m_closed)
                {
                    if (m_reliableTransmissions.Count == 0)
                    {
                        // No request retransmissions in progress close down thread until next one required.
                        m_reliablesThreadRunning = false;
                        break;
                    }

                    try
                    {
                        List<string> completedTransactions = new List<string>();

                        lock (m_reliableTransmissions)
                        {
                            foreach (SIPTransaction transaction in m_reliableTransmissions.Values)
                            {
                                if (!transaction.DeliveryPending)
                                {
                                    completedTransactions.Add(transaction.TransactionId);
                                }
                                else if (transaction.TransactionState == SIPTransactionStatesEnum.Terminated ||
                                        transaction.TransactionState == SIPTransactionStatesEnum.Confirmed ||
                                        transaction.TransactionState == SIPTransactionStatesEnum.Cancelled ||
                                        transaction.HasTimedOut)
                                {
                                    transaction.DeliveryPending = false;
                                    completedTransactions.Add(transaction.TransactionId);
                                }
                                else
                                {
                                    if (DateTime.Now.Subtract(transaction.InitialTransmit).TotalMilliseconds >= m_t6)
                                    {
                                        //logger.Debug("Request timed out " + transaction.TransactionRequest.Method + " " + transaction.TransactionRequest.URI.ToString() + ".");

                                        transaction.DeliveryPending = false;
                                        transaction.DeliveryFailed = true;
                                        transaction.TimedOutAt = DateTime.Now;
                                        transaction.HasTimedOut = true;
                                        // transaction.FireTransactionTimedOut();
                                        transaction.Expire(DateTime.Now);
                                        completedTransactions.Add(transaction.TransactionId);
                                    }
                                    else
                                    {
                                        double nextTransmitMilliseconds = Math.Pow(2, transaction.Retransmits - 1) * m_t1;
                                        nextTransmitMilliseconds = (nextTransmitMilliseconds > m_t2) ? m_t2 : nextTransmitMilliseconds;
                                        //logger.Debug("Time since retransmit " + transaction.Retransmits + " for " + transaction.TransactionRequest.Method + " " + transaction.TransactionRequest.URI.ToString() + " " + DateTime.Now.Subtract(transaction.LastTransmit).TotalMilliseconds + ".");

                                        if (DateTime.Now.Subtract(transaction.LastTransmit).TotalMilliseconds >= nextTransmitMilliseconds)
                                        {
                                            transaction.Retransmits = transaction.Retransmits + 1;
                                            transaction.LastTransmit = DateTime.Now;

                                            if (transaction.TransactionType == SIPTransactionTypesEnum.InviteServer && transaction.TransactionState == SIPTransactionStatesEnum.Completed)
                                            {
                                                //logger.Debug("Retransmit " + transaction.Retransmits + "(" + transaction.TransactionId + ") for INVITE reponse " + transaction.TransactionRequest.URI.ToString() + ", last=" + DateTime.Now.Subtract(transaction.LastTransmit).TotalMilliseconds + "ms, first=" + DateTime.Now.Subtract(transaction.InitialTransmit).TotalMilliseconds + "ms.");

                                                // This is an INVITE transaction that wants to send a reliable response, once the ACK is received it will change the transaction state to confirmed.
                                                //SIPViaHeader topViaHeader = transaction.TransactionFinalResponse.Header.Vias.TopViaHeader;
                                                //SendResponse(transaction.TransactionFinalResponse);
                                                //transaction.ResponseRetransmit();
                                                transaction.RetransmitFinalResponse();
                                            }
                                            else
                                            {
                                                //logger.Debug("Retransmit " + transaction.Retransmits + " for request " + transaction.TransactionRequest.Method + " " + transaction.TransactionRequest.URI.ToString() + ", last=" + DateTime.Now.Subtract(transaction.LastTransmit).TotalMilliseconds + "ms, first=" + DateTime.Now.Subtract(transaction.InitialTransmit).TotalMilliseconds + "ms.");
                                                if (transaction.OutboundProxy != null)
                                                {
                                                    SendRequest(transaction.OutboundProxy, transaction.TransactionRequest);
                                                }
                                                else
                                                {
                                                    SendRequest(transaction.RemoteEndPoint, transaction.TransactionRequest);
                                                }
                                                transaction.RequestRetransmit();
                                            }
                                        }
                                    }
                                }
                            }

                            // Remove timed out or complete transactions from reliable transmissions list.
                            if (completedTransactions.Count > 0)
                            {
                                foreach (string transactionId in completedTransactions)
                                {
                                    if (m_reliableTransmissions.ContainsKey(transactionId))
                                    {
                                        m_reliableTransmissions.Remove(transactionId);
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception excp)
                    {
                        logger.Error("Exception SIPTransport ProcessPendingRequests checking pendings. " + excp.Message);
                    }

                    Thread.Sleep(PENDINGREQUESTS_CHECK_PERIOD);
                }

                //logger.Warn("SIPTransport process reliable transmissions thread stopped.");
            }
            catch (Exception excp)
            {
                logger.Error("Exception SIPTransport ProcessPendingRequests. " + excp.Message);
            }
            finally
            {
                m_reliablesThreadRunning = false;
            }
        }

        private void SIPMessageReceived(SIPChannel sipChannel, SIPEndPoint remoteEndPoint, byte[] buffer)
        {
            string rawSIPMessage = null;

            try
            {
                if (buffer != null && buffer.Length > 0)
                {
                    if ((buffer[0] == 0x0 || buffer[0] == 0x1) && buffer.Length >= 20)
                    {
                        // Treat any messages that cannot be SIP as STUN requests.
                        if (STUNRequestReceived != null)
                        {
                            STUNRequestReceived(sipChannel.SIPChannelEndPoint.GetIPEndPoint(), remoteEndPoint.GetIPEndPoint(), buffer, buffer.Length);

                            if (PerformanceMonitorPrefix != null)
                            {
                                SIPSorceryPerformanceMonitor.IncrementCounter(PerformanceMonitorPrefix + SIPSorceryPerformanceMonitor.SIP_TRANSPORT_STUN_REQUESTS_PER_SECOND_SUFFIX);
                            }
                        }
                    }
                    else
                    {
                        // Treat all messages that don't match STUN requests as SIP.
                        if (buffer.Length > SIPConstants.SIP_MAXIMUM_RECEIVE_LENGTH)
                        {
                            string rawErrorMessage = Encoding.UTF8.GetString(buffer, 0, 1024) + "\r\n..truncated";
                            FireSIPBadRequestInTraceEvent(sipChannel.SIPChannelEndPoint, remoteEndPoint, "SIP message too large, " + buffer.Length + " bytes, maximum allowed is " + SIPConstants.SIP_MAXIMUM_RECEIVE_LENGTH + " bytes.", SIPValidationFieldsEnum.Request, rawErrorMessage);
                            SIPResponse tooLargeResponse = GetResponse(sipChannel.SIPChannelEndPoint, remoteEndPoint, SIPResponseStatusCodesEnum.MessageTooLarge, null);
                            SendResponse(tooLargeResponse);
                            if (PerformanceMonitorPrefix != null)
                            {
                                SIPSorceryPerformanceMonitor.IncrementCounter(PerformanceMonitorPrefix + SIPSorceryPerformanceMonitor.SIP_TRANSPORT_SIP_BAD_MESSAGES_PER_SECOND_SUFFIX);
                            }
                        }
                        else
                        {
                            //rawSIPMessage = Encoding.Default.GetString(buffer, 0, buffer.Length);
                            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);//函数进行注册,解使用GB2312编码异常，配合System.Text.Encoding.CodePages包使用
                            rawSIPMessage = Encoding.GetEncoding("GB2312").GetString(buffer, 0, buffer.Length);
                            if (!rawSIPMessage.StartsWith("REGISTER", StringComparison.CurrentCultureIgnoreCase) && !rawSIPMessage.StartsWith("MESSAGE", StringComparison.CurrentCultureIgnoreCase))
                            {
                                string debug = rawSIPMessage;
                            }
                            if (rawSIPMessage.IsNullOrBlank())
                            {
                                // An emptry transmission has been received. More than likely this is a NAT keep alive and can be disregarded.
                                //FireSIPBadRequestInTraceEvent(sipChannel.SIPChannelEndPoint, remoteEndPoint, "No printable characters, length " + buffer.Length + " bytes.", SIPValidationFieldsEnum.Unknown, null);

                                if (PerformanceMonitorPrefix != null)
                                {
                                    SIPSorceryPerformanceMonitor.IncrementCounter(PerformanceMonitorPrefix + SIPSorceryPerformanceMonitor.SIP_TRANSPORT_SIP_BAD_MESSAGES_PER_SECOND_SUFFIX);
                                }

                                return;
                            }
                            else if (!rawSIPMessage.Contains("SIP", StringComparison.CurrentCultureIgnoreCase))
                            {
                                FireSIPBadRequestInTraceEvent(sipChannel.SIPChannelEndPoint, remoteEndPoint, "Missing SIP string.", SIPValidationFieldsEnum.NoSIPString, rawSIPMessage);

                                if (PerformanceMonitorPrefix != null)
                                {
                                    SIPSorceryPerformanceMonitor.IncrementCounter(PerformanceMonitorPrefix + SIPSorceryPerformanceMonitor.SIP_TRANSPORT_SIP_BAD_MESSAGES_PER_SECOND_SUFFIX);
                                }

                                return;
                            }

                            SIPMessage sipMessage = (SIPMessage)SIPMessage.ParseSIPMessage(rawSIPMessage, sipChannel.SIPChannelEndPoint, remoteEndPoint);

                            if (sipMessage != null)
                            {
                                if (sipMessage.SIPMessageType == SIPMessageTypesEnum.Response)
                                {
                                    #region SIP Response.

                                    try
                                    {
                                        if (PerformanceMonitorPrefix != null)
                                        {
                                            SIPSorceryPerformanceMonitor.IncrementCounter(PerformanceMonitorPrefix + SIPSorceryPerformanceMonitor.SIP_TRANSPORT_SIP_RESPONSES_PER_SECOND_SUFFIX);
                                        }

                                        SIPResponse sipResponse = SIPResponse.ParseSIPResponse(sipMessage);

                                        if (SIPResponseInTraceEvent != null)
                                        {
                                            FireSIPResponseInTraceEvent(sipChannel.SIPChannelEndPoint, remoteEndPoint, sipResponse);
                                        }

                                        if (_transactionEngine != null && _transactionEngine.Exists(sipResponse))
                                        {
                                            var transaction = _transactionEngine.GetTransaction(sipResponse);

                                            if (transaction.TransactionState != SIPTransactionStatesEnum.Completed)
                                            {
                                                transaction.DeliveryPending = false;
                                                if (m_reliableTransmissions.ContainsKey(transaction.TransactionId))
                                                {
                                                    lock (m_reliableTransmissions)
                                                    {
                                                        m_reliableTransmissions.Remove(transaction.TransactionId);
                                                    }
                                                }
                                            }

                                            transaction.GotResponse(sipChannel.SIPChannelEndPoint, remoteEndPoint, sipResponse);
                                        }
                                        else
                                        {
                                            SIPTransportResponseReceived?.Invoke(sipChannel.SIPChannelEndPoint, remoteEndPoint, sipResponse);
                                        }
                                    }
                                    catch (SIPValidationException sipValidationException)
                                    {
                                        //logger.Warn("Invalid SIP response from " + sipMessage.ReceivedFrom + ", " + sipResponse.ValidationError + " , ignoring.");
                                        //logger.Warn(sipMessage.RawMessage);
                                        FireSIPBadResponseInTraceEvent(sipChannel.SIPChannelEndPoint, remoteEndPoint, sipMessage.RawMessage, sipValidationException.SIPErrorField, sipMessage.RawMessage);
                                    }

                                    #endregion
                                }
                                else
                                {
                                    #region SIP Request.
                                    if (PerformanceMonitorPrefix != null)
                                    {
                                        SIPSorceryPerformanceMonitor.IncrementCounter(PerformanceMonitorPrefix + SIPSorceryPerformanceMonitor.SIP_TRANSPORT_SIP_REQUESTS_PER_SECOND_SUFFIX);
                                    }

                                    try
                                    {
                                        SIPRequest sipRequest = SIPRequest.ParseSIPRequest(sipMessage);

                                        SIPValidationFieldsEnum sipRequestErrorField = SIPValidationFieldsEnum.Unknown;
                                        if (!sipRequest.IsValid(out sipRequestErrorField, out string sipRequestValidationError))
                                        {
                                            throw new SIPValidationException(sipRequestErrorField, sipRequestValidationError);
                                        }

                                        if (SIPRequestInTraceEvent != null)
                                        {
                                            FireSIPRequestInTraceEvent(sipChannel.SIPChannelEndPoint, remoteEndPoint, sipRequest);
                                        }

                                        // Stateful cores will create transactions once they get the request and the transport layer will use those transactions.
                                        // Stateless cores will not be affected by this step as the transaction layer will always return false.
                                        SIPTransaction requestTransaction = _transactionEngine?.GetTransaction(sipRequest);
                                        if (requestTransaction != null)
                                        {
                                            if (requestTransaction.TransactionState == SIPTransactionStatesEnum.Completed && sipRequest.Method != SIPMethodsEnum.ACK)
                                            {
                                                logger.Warn("Resending final response for " + sipRequest.Method + ", " + sipRequest.URI.ToString() + ", cseq=" + sipRequest.Header.CSeq + ".");
                                                requestTransaction.RetransmitFinalResponse();
                                            }
                                            else if (sipRequest.Method == SIPMethodsEnum.ACK)
                                            {
                                                //logger.Debug("ACK received for " + requestTransaction.TransactionRequest.URI.ToString() + ".");

                                                if (requestTransaction.TransactionState == SIPTransactionStatesEnum.Completed)
                                                {
                                                    //logger.Debug("ACK received for INVITE, setting state to Confirmed, " + sipRequest.URI.ToString() + " from " + sipRequest.Header.From.FromURI.User + " " + remoteEndPoint + ".");
                                                    //requestTransaction.UpdateTransactionState(SIPTransactionStatesEnum.Confirmed);
                                                    requestTransaction.ACKReceived(sipChannel.SIPChannelEndPoint, remoteEndPoint, sipRequest);
                                                }
                                                else if (requestTransaction.TransactionState == SIPTransactionStatesEnum.Confirmed)
                                                {
                                                    // ACK retransmit, ignore as a previous ACK was received and the transaction has already been confirmed.
                                                }
                                                else
                                                {
                                                    //logger.Warn("ACK recieved from " + remoteEndPoint.ToString() + " on " + requestTransaction.TransactionState + " transaction, ignoring.");
                                                    FireSIPBadRequestInTraceEvent(sipChannel.SIPChannelEndPoint, remoteEndPoint, "ACK recieved on " + requestTransaction.TransactionState + " transaction, ignoring.", SIPValidationFieldsEnum.Request, null);
                                                }
                                            }
                                            else
                                            {
                                                logger.Warn("Transaction already exists, ignoring duplicate request, " + sipRequest.Method + " " + sipRequest.URI.ToString() + ".");
                                                //FireSIPBadRequestInTraceEvent(sipChannel.SIPChannelEndPoint, remoteEndPoint, "Transaction already exists, ignoring duplicate request, " + sipRequest.Method + " " + sipRequest.URI.ToString() + " from " + remoteEndPoint + ".", SIPValidationFieldsEnum.Request);
                                            }
                                        }
                                        else if (SIPTransportRequestReceived != null)
                                        {
                                            // This is a new SIP request and if the validity checks are passed it will be handed off to all subscribed new request listeners.

                                            #region Check for invalid SIP requests.

                                            if (sipRequest.Header.MaxForwards == 0 && sipRequest.Method != SIPMethodsEnum.OPTIONS)
                                            {
                                                // Check the MaxForwards value, if equal to 0 the request must be discarded. If MaxForwards is -1 it indicates the
                                                // header was not present in the request and that the MaxForwards check should not be undertaken.
                                                //logger.Warn("SIPTransport responding with TooManyHops due to 0 MaxForwards.");
                                                FireSIPBadRequestInTraceEvent(sipChannel.SIPChannelEndPoint, remoteEndPoint, "Zero MaxForwards on " + sipRequest.Method + " " + sipRequest.URI.ToString() + " from " + sipRequest.Header.From.FromURI.User + " " + remoteEndPoint.ToString(), SIPValidationFieldsEnum.Request, sipRequest.ToString());
                                                SIPResponse tooManyHops = GetResponse(sipRequest, SIPResponseStatusCodesEnum.TooManyHops, null);
                                                SendResponse(sipChannel, remoteEndPoint, tooManyHops);
                                                return;
                                            }
                                            /*else if (sipRequest.IsLoop(sipChannel.SIPChannelEndPoint.SocketEndPoint.Address.ToString(), sipChannel.SIPChannelEndPoint.SocketEndPoint.Port, sipRequest.CreateBranchId()))
                                            {
                                                //logger.Warn("SIPTransport Dropping looped request.");
                                                FireSIPBadRequestInTraceEvent(sipChannel.SIPChannelEndPoint, remoteEndPoint, "Dropping looped request, " + sipRequest.Method + " " + sipRequest.URI.ToString() + " from " + sipRequest.Header.From.FromURI.User + " " + IPSocket.GetSocketString(remoteEndPoint), SIPValidationFieldsEnum.Request);
                                                SIPResponse loopResponse = GetResponse(sipRequest, SIPResponseStatusCodesEnum.LoopDetected, null);
                                                SendResponse(loopResponse);
                                                return;
                                            }*/

                                            #endregion

                                            #region Route pre-processing.

                                            if (sipRequest.Header.Routes.Length > 0)
                                            {
                                                PreProcessRouteInfo(sipRequest);
                                            }

                                            #endregion

                                            // Request has passed validity checks, adjust the client Via header to reflect the socket the request was received on.
                                            //SIPViaHeader originalTopViaHeader = sipRequest.Header.Via.TopViaHeader;
                                            sipRequest.Header.Vias.UpateTopViaHeader(remoteEndPoint.GetIPEndPoint());

                                            // Stateful cores should create a transaction once they receive this event, stateless cores should not.
                                            SIPTransportRequestReceived?.Invoke(sipChannel.SIPChannelEndPoint, remoteEndPoint, sipRequest);
                                        }
                                    }
                                    catch (SIPValidationException sipRequestExcp)
                                    {
                                        FireSIPBadRequestInTraceEvent(sipChannel.SIPChannelEndPoint, remoteEndPoint, sipRequestExcp.Message, sipRequestExcp.SIPErrorField, sipMessage.RawMessage);
                                        SIPResponse errorResponse = GetResponse(sipChannel.SIPChannelEndPoint, remoteEndPoint, sipRequestExcp.SIPResponseErrorCode, sipRequestExcp.Message);
                                        SendResponse(sipChannel, remoteEndPoint, errorResponse);
                                    }

                                    #endregion
                                }
                            }
                            else
                            {
                                FireSIPBadRequestInTraceEvent(sipChannel.SIPChannelEndPoint, remoteEndPoint, "Not parseable as SIP message.", SIPValidationFieldsEnum.Unknown, rawSIPMessage);

                                if (PerformanceMonitorPrefix != null)
                                {
                                    SIPSorceryPerformanceMonitor.IncrementCounter(PerformanceMonitorPrefix + SIPSorceryPerformanceMonitor.SIP_TRANSPORT_SIP_BAD_MESSAGES_PER_SECOND_SUFFIX);
                                }

                            }
                        }
                    }
                }
            }
            catch (Exception excp)
            {
                logger.Warn("SIPMessageReceived exception : " + excp.Message);
                FireSIPBadRequestInTraceEvent(sipChannel.SIPChannelEndPoint, remoteEndPoint, "Exception SIPTransport. " + excp.Message, SIPValidationFieldsEnum.Unknown, rawSIPMessage);
                if (PerformanceMonitorPrefix != null)
                {
                    SIPSorceryPerformanceMonitor.IncrementCounter(PerformanceMonitorPrefix + SIPSorceryPerformanceMonitor.SIP_TRANSPORT_SIP_BAD_MESSAGES_PER_SECOND_SUFFIX);
                }
            }
        }

        /// <summary>
        /// Attempts to match a SIPChannel for this process that has the specified local end point and protocol.
        /// </summary>
        /// <param name="localEndPoint">The local socket endpoint of the SIPChannel to find.</param>
        /// <returns>A matching SIPChannel if found otherwise null.</returns>
        public SIPChannel FindSIPChannel(SIPEndPoint localSIPEndPoint)
        {
            //bool isEqual = (localSIPEndPoint == m_sipChannels.Keys.First<SIPEndPoint>());
            //logger.Debug("Searching for SIP channel for endpoint " + localSIPEndPoint.ToString() + ". First channel in transport list is " + m_sipChannels.Keys.First().ToString() + ". " + m_sipChannels.Keys.Contains(localSIPEndPoint) + ", " + isEqual);
            if (localSIPEndPoint == null)
            {
                return null;
            }
            else
            {
                if (m_sipChannels.ContainsKey(localSIPEndPoint.ToString()))
                {
                    return m_sipChannels[localSIPEndPoint.ToString()];
                }
                else
                {
                    logger.Warn("No SIP channel could be found for local SIP end point " + localSIPEndPoint.ToString() + ".");
                    return null;
                }
            }
        }

        /// <summary>
        /// Returns the first SIPChannel found for the requested protocol.
        /// </summary>
        /// <param name="protocol"></param>
        /// <returns></returns>
        private SIPChannel GetDefaultChannel(SIPProtocolsEnum protocol)
        {
            // Channels that are not on a loopback address take priority.
            foreach (SIPChannel sipChannel in m_sipChannels.Values)
            {
                if (sipChannel.SIPChannelEndPoint.Protocol == protocol && !IPAddress.IsLoopback(sipChannel.SIPChannelEndPoint.Address))
                {
                    return sipChannel;
                }
            }
            foreach (SIPChannel sipChannel in m_sipChannels.Values)
            {
                if (sipChannel.SIPChannelEndPoint.Protocol == protocol)
                {
                    return sipChannel;
                }
            }

            logger.Warn("No default SIP channel could be found for " + protocol + ".");
            return null;
        }

        public bool IsLocalSIPEndPoint(SIPEndPoint sipEndPoint)
        {
            return m_sipChannels.ContainsKey(sipEndPoint.ToString());
        }

        public bool DoesTransactionExist(SIPRequest sipRequest)
        {
            if (_transactionEngine == null)
            {
                return false;
            }
            else if (_transactionEngine.GetTransaction(sipRequest) != null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public List<SIPEndPoint> GetListeningSIPEndPoints()
        {
            try
            {
                List<SIPEndPoint> endPointsList = new List<SIPEndPoint>();

                foreach (SIPChannel channel in m_sipChannels.Values)
                {
                    endPointsList.Add(channel.SIPChannelEndPoint);
                }

                return endPointsList;
            }
            catch (Exception excp)
            {
                logger.Error("Exception GetListeningSIPEndPoints. " + excp.Message);
                throw;
            }
        }

        #region Logging.

        private void FireSIPRequestInTraceEvent(SIPEndPoint localSIPEndPoint, SIPEndPoint remoteEndPoint, SIPRequest sipRequest)
        {
            try
            {
                SIPRequestInTraceEvent?.Invoke(localSIPEndPoint, remoteEndPoint, sipRequest);
            }
            catch (Exception excp)
            {
                logger.Error("Exception FireSIPRequestInTraceEvent. " + excp.Message);
            }
        }

        private void FireSIPRequestOutTraceEvent(SIPEndPoint localSIPEndPoint, SIPEndPoint remoteEndPoint, SIPRequest sipRequest)
        {
            try
            {
                SIPRequestOutTraceEvent?.Invoke(localSIPEndPoint, remoteEndPoint, sipRequest);
            }
            catch (Exception excp)
            {
                logger.Error("Exception FireSIPRequestOutTraceEvent. " + excp.Message);
            }
        }

        private void FireSIPResponseInTraceEvent(SIPEndPoint localSIPEndPoint, SIPEndPoint remoteEndPoint, SIPResponse sipResponse)
        {
            try
            {
                SIPResponseInTraceEvent?.Invoke(localSIPEndPoint, remoteEndPoint, sipResponse);
            }
            catch (Exception excp)
            {
                logger.Error("Exception FireSIPResponseInTraceEvent. " + excp.Message);
            }
        }

        private void FireSIPResponseOutTraceEvent(SIPEndPoint localSIPEndPoint, SIPEndPoint remoteEndPoint, SIPResponse sipResponse)
        {
            try
            {
                SIPResponseOutTraceEvent?.Invoke(localSIPEndPoint, remoteEndPoint, sipResponse);
            }
            catch (Exception excp)
            {
                logger.Error("Exception FireSIPResponseOutTraceEvent. " + excp.Message);
            }
        }

        private void FireSIPBadRequestInTraceEvent(SIPEndPoint localSIPEndPoint, SIPEndPoint remoteEndPoint, string message, SIPValidationFieldsEnum sipErrorField, string rawMessage)
        {
            try
            {
                //logger.Warn("SIPTransport SIPValidationException SIPRequest. Field=" + sipErrorField + ", Message=" + message + ", Remote=" + remoteEndPoint.ToString() + ".");

                SIPBadRequestInTraceEvent?.Invoke(localSIPEndPoint, remoteEndPoint, message, sipErrorField, rawMessage);
            }
            catch (Exception excp)
            {
                logger.Error("Exception FireSIPBadRequestInTraceEvent. " + excp.Message);
            }
        }

        private void FireSIPBadResponseInTraceEvent(SIPEndPoint localSIPEndPoint, SIPEndPoint remoteEndPoint, string message, SIPValidationFieldsEnum sipErrorField, string rawMessage)
        {
            try
            {
                SIPBadResponseInTraceEvent?.Invoke(localSIPEndPoint, remoteEndPoint, message, sipErrorField, rawMessage);
            }
            catch (Exception excp)
            {
                logger.Error("Exception FireSIPBadResponseInTraceEvent. " + excp.Message);
            }
        }

        #endregion

        #region Request, Response and Transaction retrieval and creation methods.

        public static SIPResponse GetResponse(SIPRequest sipRequest, SIPResponseStatusCodesEnum responseCode, string reasonPhrase)
        {
            try
            {
               // SIPResponse response = new SIPResponse(responseCode, reasonPhrase, sipRequest.LocalSIPEndPoint);
                var response = new SIPResponse(responseCode, reasonPhrase);
                if (reasonPhrase != null)
                {
                    response.ReasonPhrase = reasonPhrase;
                }

                SIPHeader requestHeader = sipRequest.Header;
                SIPFromHeader from = (requestHeader == null || requestHeader.From != null) ? requestHeader.From : new SIPFromHeader(null, new SIPURI(sipRequest.URI.Scheme, sipRequest.LocalSIPEndPoint), null);
                SIPToHeader to = (requestHeader == null || requestHeader.To != null) ? requestHeader.To : new SIPToHeader(null, new SIPURI(sipRequest.URI.Scheme, sipRequest.LocalSIPEndPoint), null);
                int cSeq = (requestHeader == null || requestHeader.CSeq != -1) ? requestHeader.CSeq : 1;
                string callId = (requestHeader == null || requestHeader.CallId != null) ? requestHeader.CallId : CallProperties.CreateNewCallId();

                response.Header = new SIPHeader(from, to, cSeq, callId)
                {
                    CSeqMethod = (requestHeader != null) ? requestHeader.CSeqMethod : SIPMethodsEnum.NONE
                };

                if (requestHeader == null || requestHeader.Vias == null || requestHeader.Vias.Length == 0)
                {
                    response.Header.Vias.PushViaHeader(new SIPViaHeader(sipRequest.RemoteSIPEndPoint, CallProperties.CreateBranchId()));
                }
                else
                {
                    response.Header.Vias = requestHeader.Vias;
                }

                response.Header.MaxForwards = Int32.MinValue;
                response.Header.Allow = ALLOWED_SIP_METHODS;

                return response;
            }
            catch (Exception excp)
            {
                logger.Error("Exception SIPTransport GetResponse. " + excp.Message);
                throw excp;
            }
        }

        /// <summary>
        /// Used to create a SIP response when it was not possible to parse the incoming SIP request.
        /// </summary>
        public SIPResponse GetResponse(SIPEndPoint localSIPEndPoint, SIPEndPoint remoteEndPoint, SIPResponseStatusCodesEnum responseCode, string reasonPhrase)
        {
            try
            {
                if (localSIPEndPoint == null)
                {
                    localSIPEndPoint = GetDefaultSIPEndPoint();
                }

                SIPResponse response = new SIPResponse(responseCode, reasonPhrase);
                SIPSchemesEnum sipScheme = (localSIPEndPoint.Protocol == SIPProtocolsEnum.tls) ? SIPSchemesEnum.sips : SIPSchemesEnum.sip;
                SIPFromHeader from = new SIPFromHeader(null, new SIPURI(sipScheme, localSIPEndPoint), null);
                SIPToHeader to = new SIPToHeader(null, new SIPURI(sipScheme, localSIPEndPoint), null);
                int cSeq = 1;
                string callId = CallProperties.CreateNewCallId();
                response.Header = new SIPHeader(from, to, cSeq, callId)
                {
                    CSeqMethod = SIPMethodsEnum.NONE
                };
                response.Header.Vias.PushViaHeader(new SIPViaHeader(new SIPEndPoint(localSIPEndPoint.Protocol, remoteEndPoint.GetIPEndPoint()), CallProperties.CreateBranchId()));
                response.Header.MaxForwards = Int32.MinValue;
                response.Header.Allow = ALLOWED_SIP_METHODS;

                return response;
            }
            catch (Exception excp)
            {
                logger.Error("Exception SIPTransport GetResponse. " + excp.Message);
                throw;
            }
        }

        public SIPRequest GetRequest(SIPMethodsEnum method, SIPURI uri)
        {
            return GetRequest(method, uri, new SIPToHeader(null, uri, null), null);
        }

        public SIPRequest GetRequest(SIPMethodsEnum method, SIPURI uri, SIPToHeader to, SIPEndPoint localSIPEndPoint)
        {
            try
            {
                if (localSIPEndPoint == null)
                {
                    localSIPEndPoint = GetDefaultSIPEndPoint();
                }

                SIPRequest request = new SIPRequest(method, uri)
                {
                    LocalSIPEndPoint = localSIPEndPoint
                };

                SIPContactHeader contactHeader = new SIPContactHeader(null, new SIPURI(SIPSchemesEnum.sip, localSIPEndPoint));
                SIPFromHeader fromHeader = new SIPFromHeader(null, contactHeader.ContactURI, CallProperties.CreateNewTag());
                SIPHeader header = new SIPHeader(contactHeader, fromHeader, to, 1, CallProperties.CreateNewCallId());
                request.Header = header;
                header.CSeqMethod = method;
                header.Allow = ALLOWED_SIP_METHODS;

                SIPViaHeader viaHeader = new SIPViaHeader(localSIPEndPoint, CallProperties.CreateBranchId());
                header.Vias.PushViaHeader(viaHeader);

                return request;
            }
            catch (Exception excp)
            {
                logger.Error("Exception SIPTransport GetRequest. " + excp.Message);
                throw;
            }
        }

        public SIPTransaction GetTransaction(string transactionId)
        {
            CheckTransactionEngineExists();
            return _transactionEngine.GetTransaction(transactionId);
        }

        public SIPTransaction GetTransaction(SIPRequest sipRequest)
        {
            CheckTransactionEngineExists();
            return _transactionEngine.GetTransaction(sipRequest);
        }

        public SIPNonInviteTransaction CreateNonInviteTransaction(SIPRequest sipRequest, SIPEndPoint dstEndPoint, SIPEndPoint localSIPEndPoint, SIPEndPoint outboundProxy)
        {
            try
            {
                if (localSIPEndPoint == null)
                {
                    localSIPEndPoint = GetDefaultSIPEndPoint();
                }

                CheckTransactionEngineExists();
                SIPNonInviteTransaction nonInviteTransaction = new SIPNonInviteTransaction(this, sipRequest, dstEndPoint, localSIPEndPoint, outboundProxy);
                _transactionEngine.AddTransaction(nonInviteTransaction);
                return nonInviteTransaction;
            }
            catch (Exception excp)
            {
                logger.Error("Exception CreateNonInviteTransaction. " + excp.Message);
                throw;
            }
        }

        public UACInviteTransaction CreateUACTransaction(SIPRequest sipRequest, SIPEndPoint dstEndPoint, SIPEndPoint localSIPEndPoint, SIPEndPoint outboundProxy, bool sendOkAckManually = false)
        {
            try
            {
                if (localSIPEndPoint == null)
                {
                    localSIPEndPoint = GetDefaultSIPEndPoint();
                }

                CheckTransactionEngineExists();
                UACInviteTransaction uacInviteTransaction = new UACInviteTransaction(this, sipRequest, dstEndPoint, localSIPEndPoint, outboundProxy, sendOkAckManually);
                _transactionEngine.AddTransaction(uacInviteTransaction);
                return uacInviteTransaction;
            }
            catch (Exception excp)
            {
                logger.Error("Exception CreateUACTransaction. " + excp.Message);
                throw;
            }
        }

        public UASInviteTransaction CreateUASTransaction(SIPRequest sipRequest, SIPEndPoint dstEndPoint, SIPEndPoint localSIPEndPoint, SIPEndPoint outboundProxy, bool noCDR = false)
        {
            try
            {
                if (localSIPEndPoint == null)
                {
                    localSIPEndPoint = GetDefaultSIPEndPoint();
                }

                CheckTransactionEngineExists();
                UASInviteTransaction uasInviteTransaction = new UASInviteTransaction(this, sipRequest, dstEndPoint, localSIPEndPoint, outboundProxy, ContactIPAddress, noCDR);
                if (_transactionEngine.GetTransaction(sipRequest) != null
                    && _transactionEngine.GetTransaction(sipRequest).TransactionId.Equals(uasInviteTransaction.TransactionId))
                {
                    return uasInviteTransaction;
                }
                _transactionEngine.AddTransaction(uasInviteTransaction);
                return uasInviteTransaction;
            }
            catch (Exception excp)
            {
                logger.Error("Exception CreateUASTransaction. " + excp);
                throw;
            }
        }

        public SIPCancelTransaction CreateCancelTransaction(SIPRequest sipRequest, SIPEndPoint dstEndPoint, SIPEndPoint localSIPEndPoint, UASInviteTransaction inviteTransaction)
        {
            try
            {
                if (localSIPEndPoint == null)
                {
                    localSIPEndPoint = GetDefaultSIPEndPoint();
                }

                CheckTransactionEngineExists();
                SIPCancelTransaction cancelTransaction = new SIPCancelTransaction(this, sipRequest, dstEndPoint, localSIPEndPoint, inviteTransaction);
                _transactionEngine.AddTransaction(cancelTransaction);
                return cancelTransaction;
            }
            catch (Exception excp)
            {
                logger.Error("Exception CreateCancelTransaction. " + excp);
                throw;
            }
        }

        private void CheckTransactionEngineExists()
        {
            if (_transactionEngine == null)
            {
                throw new ApplicationException("A transaction engine is required for this operation but one has not been provided.");
            }
        }

        #endregion

    }
}
