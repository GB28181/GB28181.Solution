//-----------------------------------------------------------------------------
// Filename: SIPClientUserAgent.cs
//
// Description: Implementation of a SIP Client User Agent that can be used to initiate SIP calls.
// 
// History:
// 22 Feb 2008	Aaron Clauson	    Created.
// 30 May 2020	Edward Chen         Updated.


using System;
using System.Collections.Generic;
using System.Net;
using GB28181.Net;
using GB28181.Sys;
using GB28181.Logger4Net;
using SIPSorcery.Sys;
using SIPSorcery.SIP;

namespace GB28181.App
{
    public class SIPClientUserAgent : ISIPClientUserAgent
    {
        private const int DNS_LOOKUP_TIMEOUT = 5000;
        private const char OUTBOUNDPROXY_AS_ROUTESET_CHAR = '<';    // If this character exists in the call descriptor OutboundProxy setting it gets treated as a Route set.

        private static ILog logger = AppState.logger;
        private static ILog rtccLogger = AppState.GetLogger("rtcc");

        private static string m_userAgent = SIPConstants.SIP_USERAGENT_STRING;
        private static readonly int m_defaultSIPPort = SIPConstants.DEFAULT_SIP_PORT;
        private static readonly string m_sdpContentType = SDP.SDP_MIME_CONTENTTYPE;
        //private static readonly int m_rtccInitialReservationSeconds = SIPSorcery.Entities.CustomerAccountDataLayer.INITIAL_RESERVATION_SECONDS;

        private SIPTransport m_sipTransport;
        private SIPEndPoint m_localSIPEndPoint;
        private SIPMonitorLogDelegate Log_External;

        public string Owner { get; private set; }                   // If the UAC is authenticated holds the username of the client.
        public string AdminMemberId { get; private set; }           // If the UAC is authenticated holds the username of the client.

        // Real-time call control properties.
        public string AccountCode { get; set; }
        public decimal ReservedCredit { get; set; }
        public int ReservedSeconds { get; set; }
        public decimal Rate { get; set; }

        private SIPCallDescriptor m_sipCallDescriptor;              // Describes the server leg of the call from the sipswitch.
        private SIPEndPoint m_serverEndPoint;
        private UACInviteTransaction m_serverTransaction;
        private bool m_callCancelled;                               // It's possible for the call to be cancelled before the INVITE has been sent. This could occur if a DNS lookup on the server takes a while.
        private bool m_hungupOnCancel;                              // Set to true if a call has been cancelled AND and then an Ok response was received AND a BYE has been sent to hang it up. This variable is used to stop another BYE transaction being generated.
        private int m_serverAuthAttempts;                           // Used to determine if credentials for a server leg call fail.
        private SIPNonInviteTransaction m_cancelTransaction;        // If the server call is cancelled this transaction contains the CANCEL in case it needs to be resent.
        private SIPEndPoint m_outboundProxy;                        // If the system needs to use an outbound proxy for every request this will be set and overrides any user supplied values.
        private SIPDialogue m_sipDialogue;

        //private SIPSorcery.Entities.CustomerAccountDataLayer m_customerAccountDataLayer = new SIPSorcery.Entities.CustomerAccountDataLayer();
        private RtccGetCustomerDelegate RtccGetCustomer_External;
        private RtccGetRateDelegate RtccGetRate_External;
        private RtccGetBalanceDelegate RtccGetBalance_External;
        private RtccReserveInitialCreditDelegate RtccReserveInitialCredit_External;
        private RtccUpdateCdrDelegate RtccUpdateCdr_External;

        public event SIPCallResponseDelegate CallTrying;
        public event SIPCallResponseDelegate CallRinging;
        public event SIPCallResponseDelegate CallAnswered;
        public event SIPCallFailedDelegate CallFailed;

        public UACInviteTransaction ServerTransaction
        {
            get { return m_serverTransaction; }
        }

        public bool IsUACAnswered
        {
            get { return m_serverTransaction.TransactionFinalResponse != null; }
        }

        public SIPDialogue SIPDialogue
        {
            get { return m_sipDialogue; }
        }

        public SIPCallDescriptor CallDescriptor
        {
            get { return m_sipCallDescriptor; }
        }

        public SIPClientUserAgent(
            SIPTransport sipTransport,
            SIPEndPoint outboundProxy,
            string owner,
            string adminMemberId,
            SIPMonitorLogDelegate logDelegate
            )
        {
            m_sipTransport = sipTransport;
            m_outboundProxy = (outboundProxy != null) ? SIPEndPoint.ParseSIPEndPoint(outboundProxy.ToString()) : null;
            Owner = owner;
            AdminMemberId = adminMemberId;
            Log_External = logDelegate;

            // If external logging is not required assign an empty handler to stop null reference exceptions.
            if (Log_External == null)
            {
                Log_External = (e) => { };
            }
        }

        public SIPClientUserAgent(
            SIPTransport sipTransport,
            SIPEndPoint outboundProxy,
            string owner,
            string adminMemberId,
            SIPMonitorLogDelegate logDelegate,
            RtccGetCustomerDelegate rtccGetCustomer,
            RtccGetRateDelegate rtccGetRate,
            RtccGetBalanceDelegate rtccGetBalance,
            RtccReserveInitialCreditDelegate rtccReserveInitialCredit,
            RtccUpdateCdrDelegate rtccUpdateCdr
            ) : this(sipTransport, outboundProxy, owner, adminMemberId, logDelegate)
        {
            RtccGetCustomer_External = rtccGetCustomer;
            RtccGetRate_External = rtccGetRate;
            RtccGetBalance_External = rtccGetBalance;
            RtccReserveInitialCredit_External = rtccReserveInitialCredit;
            RtccUpdateCdr_External = rtccUpdateCdr;
        }


        public void Cancel()
        {
            try
            {
                m_callCancelled = true;

                // Cancel server call.
                if (m_serverTransaction == null)
                {
                    Log_External(new SIPMonitorConsoleEvent(SIPMonitorServerTypesEnum.UserAgentClient, SIPMonitorEventTypesEnum.DialPlan, "Cancelling forwarded call leg " + m_sipCallDescriptor.Uri + ", server transaction has not been created yet no CANCEL request required.", Owner));
                }
                else if (m_cancelTransaction != null)
                {
                    if (m_cancelTransaction.TransactionState != SIPTransactionStatesEnum.Completed)
                    {
                        Log_External(new SIPMonitorConsoleEvent(SIPMonitorServerTypesEnum.UserAgentClient, SIPMonitorEventTypesEnum.DialPlan, "Call " + m_serverTransaction.TransactionRequest.URI.ToString() + " has already been cancelled once, trying again.", Owner));
                        m_cancelTransaction.SendRequest(m_cancelTransaction.TransactionRequest);
                    }
                    else
                    {
                        Log_External(new SIPMonitorConsoleEvent(SIPMonitorServerTypesEnum.UserAgentClient, SIPMonitorEventTypesEnum.DialPlan, "Call " + m_serverTransaction.TransactionRequest.URI.ToString() + " has already responded to CANCEL, probably overlap in messages not re-sending.", Owner));
                    }
                }
                else //if (m_serverTransaction.TransactionState == SIPTransactionStatesEnum.Proceeding || m_serverTransaction.TransactionState == SIPTransactionStatesEnum.Trying)
                {
                    //logger.Debug("Cancelling forwarded call leg, sending CANCEL to " + ForwardedTransaction.TransactionRequest.URI.ToString() + " (transid: " + ForwardedTransaction.TransactionId + ").");
                    Log_External(new SIPMonitorConsoleEvent(SIPMonitorServerTypesEnum.UserAgentClient, SIPMonitorEventTypesEnum.DialPlan, "Cancelling forwarded call leg, sending CANCEL to " + m_serverTransaction.TransactionRequest.URI.ToString() + ".", Owner));

                    // No reponse has been received from the server so no CANCEL request neccessary, stop any retransmits of the INVITE.
                    m_serverTransaction.CancelCall();

                    SIPRequest cancelRequest = GetCancelRequest(m_serverTransaction.TransactionRequest);
                    m_cancelTransaction = m_sipTransport.CreateNonInviteTransaction(cancelRequest, m_serverEndPoint, m_serverTransaction.LocalSIPEndPoint, m_outboundProxy);
                    m_cancelTransaction.TransactionTraceMessage += TransactionTraceMessage;
                    //m_cancelTransaction.SendRequest(m_serverEndPoint, cancelRequest);
                    m_cancelTransaction.SendReliableRequest();
                }
                //else
                //{
                // No reponse has been received from the server so no CANCEL request neccessary, stop any retransmits of the INVITE.
                //    m_serverTransaction.CancelCall();
                //    Log_External(new SIPMonitorConsoleEvent(SIPMonitorServerTypesEnum.UserAgentClient, SIPMonitorEventTypesEnum.DialPlan, "Cancelling forwarded call leg " + m_sipCallDescriptor.Uri.ToString() + ", no response from server has been received so no CANCEL request required.", Owner));
                //}

                FireCallFailed(this, "Call cancelled by user.");
            }
            catch (Exception excp)
            {
                Log_External(new SIPMonitorConsoleEvent(SIPMonitorServerTypesEnum.UserAgentClient, SIPMonitorEventTypesEnum.DialPlan, "Exception CancelServerCall. " + excp.Message, Owner));
            }
        }

        public void Update(CRMHeaders crmHeaders)
        {
            Log_External(new SIPMonitorConsoleEvent(SIPMonitorServerTypesEnum.UserAgentClient, SIPMonitorEventTypesEnum.DialPlan, "Sending UPDATE to " + m_serverTransaction.TransactionRequest.URI.ToString() + ".", Owner));

            SIPRequest updateRequest = GetUpdateRequest(m_serverTransaction.TransactionRequest, crmHeaders);
            SIPNonInviteTransaction updateTransaction = m_sipTransport.CreateNonInviteTransaction(updateRequest, m_serverEndPoint, m_serverTransaction.LocalSIPEndPoint, m_outboundProxy);
            updateTransaction.TransactionTraceMessage += TransactionTraceMessage;
            updateTransaction.SendReliableRequest();
        }

        private void ServerFinalResponseReceived(SIPEndPoint localSIPEndPoint, SIPEndPoint remoteEndPoint, SIPTransaction sipTransaction, SIPResponse sipResponse)
        {
            try
            {
                //if (Thread.CurrentThread.Name.IsNullOrBlank())
                //{
                //    Thread.CurrentThread.Name = THREAD_NAME + DateTime.Now.ToString("HHmmss") + "-" + Crypto.GetRandomString(3);
                //}

                Log_External(new SIPMonitorConsoleEvent(SIPMonitorServerTypesEnum.UserAgentClient, SIPMonitorEventTypesEnum.DialPlan, "Response " + sipResponse.StatusCode + " " + sipResponse.ReasonPhrase + " for " + m_serverTransaction.TransactionRequest.URI.ToString() + ".", Owner));
                //m_sipTrace += "Received " + DateTime.Now.ToString("dd MMM yyyy HH:mm:ss") + " " + localEndPoint + "<-" + remoteEndPoint + "\r\n" + sipResponse.ToString();

                m_serverTransaction.UACInviteTransactionInformationResponseReceived -= ServerInformationResponseReceived;
                m_serverTransaction.UACInviteTransactionFinalResponseReceived -= ServerFinalResponseReceived;
                m_serverTransaction.TransactionTraceMessage -= TransactionTraceMessage;

                if (m_callCancelled && sipResponse.Status == SIPResponseStatusCodesEnum.RequestTerminated)
                {
                    // No action required. Correctly received request terminated on an INVITE we cancelled.
                }
                else if (m_callCancelled)
                {
                    #region Call has been cancelled, hangup.

                    if (m_hungupOnCancel)
                    {
                        Log_External(new SIPMonitorConsoleEvent(SIPMonitorServerTypesEnum.UserAgentClient, SIPMonitorEventTypesEnum.DialPlan, "A cancelled call to " + m_sipCallDescriptor.Uri + " has been answered AND has already been hungup, no further action being taken.", Owner));
                    }
                    else
                    {
                        m_hungupOnCancel = true;

                        Log_External(new SIPMonitorConsoleEvent(SIPMonitorServerTypesEnum.UserAgentClient, SIPMonitorEventTypesEnum.DialPlan, "A cancelled call to " + m_sipCallDescriptor.Uri + " has been answered, hanging up.", Owner));

                        if (sipResponse.Header.Contact != null && sipResponse.Header.Contact.Count > 0)
                        {
                            SIPURI byeURI = sipResponse.Header.Contact[0].ContactURI;
                            SIPRequest byeRequest = GetByeRequest(sipResponse, byeURI, localSIPEndPoint);

                            //SIPEndPoint byeEndPoint = m_sipTransport.GetRequestEndPoint(byeRequest, m_outboundProxy, true);

                            // if (byeEndPoint != null)
                            // {
                            SIPNonInviteTransaction byeTransaction = m_sipTransport.CreateNonInviteTransaction(byeRequest, null, localSIPEndPoint, m_outboundProxy);
                            byeTransaction.SendReliableRequest();
                            // }
                            // else
                            // {
                            //     Log_External(new SIPMonitorConsoleEvent(SIPMonitorServerTypesEnum.UserAgentClient, SIPMonitorEventTypesEnum.DialPlan, "Could not end BYE on cancelled call as request end point could not be determined " + byeRequest.URI.ToString(), Owner));
                            //}
                        }
                        else
                        {
                            Log_External(new SIPMonitorConsoleEvent(SIPMonitorServerTypesEnum.UserAgentClient, SIPMonitorEventTypesEnum.DialPlan, "No contact header provided on response for cancelled call to " + m_sipCallDescriptor.Uri + " no further action.", Owner));
                        }
                    }

                    #endregion
                }
                else if (sipResponse.Status == SIPResponseStatusCodesEnum.ProxyAuthenticationRequired || sipResponse.Status == SIPResponseStatusCodesEnum.Unauthorised)
                {
                    //logger.Debug("AuthReqd Final response " + sipResponse.StatusCode + " " + sipResponse.ReasonPhrase + " for " + m_serverTransaction.TransactionRequest.URI.ToString() + ".");

                    #region Authenticate client call to third party server.

                    if (!m_callCancelled)
                    {
                        if (m_sipCallDescriptor.Password.IsNullOrBlank())
                        {
                            // No point trying to authenticate if there is no password to use.
                            Log_External(new SIPMonitorConsoleEvent(SIPMonitorServerTypesEnum.UserAgentClient, SIPMonitorEventTypesEnum.DialPlan, "Forward leg failed, authentication was requested but no credentials were available.", Owner));
                            FireCallFailed(this, "Authentication requested when no credentials available");
                        }
                        else if (m_serverAuthAttempts == 0)
                        {
                            m_serverAuthAttempts = 1;

                            // Resend INVITE with credentials.
                            string username = (m_sipCallDescriptor.AuthUsername != null && m_sipCallDescriptor.AuthUsername.Trim().Length > 0) ? m_sipCallDescriptor.AuthUsername : m_sipCallDescriptor.Username;
                            SIPAuthorisationDigest authRequest = sipResponse.Header.AuthenticationHeader.SIPDigest;
                            authRequest.SetCredentials(username, m_sipCallDescriptor.Password, m_sipCallDescriptor.Uri, SIPMethodsEnum.INVITE.ToString());

                            SIPRequest authInviteRequest = m_serverTransaction.TransactionRequest;

                            //if (SIPProviderMagicJack.IsMagicJackRequest(sipResponse))
                            //{
                            //    authInviteRequest.Header.AuthenticationHeader = SIPProviderMagicJack.GetAuthenticationHeader(sipResponse);
                            //}
                            //else
                            //{
                            authInviteRequest.Header.AuthenticationHeader = new SIPAuthenticationHeader(authRequest);
                            authInviteRequest.Header.AuthenticationHeader.SIPDigest.Response = authRequest.Digest;
                            //}

                            authInviteRequest.Header.Vias.TopViaHeader.Branch = CallProperties.CreateBranchId();
                            authInviteRequest.Header.CSeq = authInviteRequest.Header.CSeq + 1;

                            // Create a new UAC transaction to establish the authenticated server call.
                            var originalCallTransaction = m_serverTransaction;
                            m_serverTransaction = m_sipTransport.CreateUACTransaction(authInviteRequest, m_serverEndPoint, localSIPEndPoint, m_outboundProxy);
                            if (m_serverTransaction.CDR != null)
                            {
                                m_serverTransaction.CDR.Owner = Owner;
                                m_serverTransaction.CDR.AdminMemberId = AdminMemberId;
                                m_serverTransaction.CDR.DialPlanContextID = m_sipCallDescriptor.DialPlanContextID;

                                m_serverTransaction.CDR.Updated();

                                if (AccountCode != null)
                                {
                                    //var rtccCDR = new SIPSorcery.Entities.CDR()
                                    //{
                                    //    ID = m_serverTransaction.CDR.CDRId.ToString(),
                                    //    Owner = m_serverTransaction.CDR.Owner,
                                    //    AdminMemberID = m_serverTransaction.CDR.AdminMemberId,
                                    //    Inserted = DateTimeOffset.UtcNow.ToString("o"),
                                    //    Created = DateTimeOffset.UtcNow.ToString("o"),
                                    //    DstHost = "",
                                    //    DstURI = m_sipCallDescriptor.Uri,
                                    //    CallID = "",
                                    //    FromHeader = m_sipCallDescriptor.From,
                                    //    LocalSocket = "udp:0.0.0.0:5060",
                                    //    RemoteSocket = "udp:0.0.0.0:5060",
                                    //    Direction = m_serverTransaction.CDR.CallDirection.ToString(),
                                    //    DialPlanContextID = m_sipCallDescriptor.DialPlanContextID.ToString()
                                    //};
#if !SILVERLIGHT
                                    //m_customerAccountDataLayer.UpdateRealTimeCallControlCDRID(originalCallTransaction.CDR.CDRId.ToString(), m_serverTransaction.CDR);
                                    RtccUpdateCdr_External(originalCallTransaction.CDR.CDRId.ToString(), m_serverTransaction.CDR);
#endif

                                    //m_serverTransaction.CDR.AccountCode = AccountCode;
                                    //m_serverTransaction.CDR.Rate = Rate;

                                    // Transfer any credit reservations from the original call to the new call.
                                    //m_serverTransaction.CDR.SecondsReserved = originalCallTransaction.CDR.SecondsReserved;
                                    //m_serverTransaction.CDR.Cost = originalCallTransaction.CDR.Cost;
                                    //m_serverTransaction.CDR.IncrementSeconds = originalCallTransaction.CDR.IncrementSeconds;
                                    //originalCallTransaction.CDR.SecondsReserved = 0;
                                    //originalCallTransaction.CDR.Cost = 0;
                                    //originalCallTransaction.CDR.ReconciliationResult = "reallocated";
                                    //originalCallTransaction.CDR.IsHangingUp = true;
                                }

                                logger.Debug("RTCC reservation was reallocated from CDR " + originalCallTransaction.CDR.CDRId + " to " + m_serverTransaction.CDR.CDRId + " for owner " + Owner + ".");
                            }
                            m_serverTransaction.UACInviteTransactionInformationResponseReceived += ServerInformationResponseReceived;
                            m_serverTransaction.UACInviteTransactionFinalResponseReceived += ServerFinalResponseReceived;
                            m_serverTransaction.UACInviteTransactionTimedOut += ServerTimedOut;
                            m_serverTransaction.TransactionTraceMessage += TransactionTraceMessage;

                            //logger.Debug("Sending authenticated switchcall INVITE to " + ForwardedCallStruct.Host + ".");
                            m_serverTransaction.SendInviteRequest(m_serverEndPoint, authInviteRequest);
                            //m_sipTrace += "Sending " + DateTime.Now.ToString("dd MMM yyyy HH:mm:ss") + " " + localEndPoint + "->" + ForwardedTransaction.TransactionRequest.GetRequestEndPoint() + "\r\n" + ForwardedTransaction.TransactionRequest.ToString();
                        }
                        else
                        {
                            //logger.Debug("Authentication of client call to switch server failed.");
                            FireCallFailed(this, "Authentication with provided credentials failed");
                        }
                    }

                    #endregion
                }
                else
                {
                    if (sipResponse.StatusCode >= 200 && sipResponse.StatusCode <= 299)
                    {
                        if (sipResponse.Body.IsNullOrBlank())
                        {
                            Log_External(new SIPMonitorConsoleEvent(SIPMonitorServerTypesEnum.AppServer, SIPMonitorEventTypesEnum.DialPlan, "Body on UAC response was empty.", Owner));
                        }
                        else if (m_sipCallDescriptor.ContentType == m_sdpContentType)
                        {
                            if (!m_sipCallDescriptor.MangleResponseSDP)
                            {
                                IPEndPoint sdpEndPoint = SDP.GetSDPRTPEndPoint(sipResponse.Body);
                                string sdpSocket = (sdpEndPoint != null) ? sdpEndPoint.ToString() : "could not determine";
                                Log_External(new SIPMonitorConsoleEvent(SIPMonitorServerTypesEnum.AppServer, SIPMonitorEventTypesEnum.DialPlan, "SDP on UAC response was set to NOT mangle, RTP socket " + sdpEndPoint.ToString() + ".", Owner));
                            }
                            else
                            {
                                //m_callInProgress = false; // the call is now established
                                //logger.Debug("Final response " + sipResponse.StatusCode + " " + sipResponse.ReasonPhrase + " for " + ForwardedTransaction.TransactionRequest.URI.ToString() + ".");
                                // Determine of response SDP should be mangled.

                                IPEndPoint sdpEndPoint = SDP.GetSDPRTPEndPoint(sipResponse.Body);
                                //Log_External(new SIPMonitorConsoleEvent(SIPMonitorServerTypesEnum.AppServer, SIPMonitorEventTypesEnum.DialPlan, "UAC response SDP was mangled from sdp=" + sdpEndPoint.Address.ToString() + ", proxyfrom=" + sipResponse.Header.ProxyReceivedFrom + ", mangle=" + m_sipCallDescriptor.MangleResponseSDP + ".", null));
                                if (sdpEndPoint != null)
                                {
                                    if (!IPSocket.IsPrivateAddress(sdpEndPoint.Address.ToString()))
                                    {
                                        Log_External(new SIPMonitorConsoleEvent(SIPMonitorServerTypesEnum.AppServer, SIPMonitorEventTypesEnum.DialPlan, "SDP on UAC response had public IP not mangled, RTP socket " + sdpEndPoint.ToString() + ".", Owner));
                                    }
                                    else
                                    {
                                        bool wasSDPMangled = false;
                                        string publicIPAddress = null;

                                        if (!sipResponse.Header.ProxyReceivedFrom.IsNullOrBlank())
                                        {
                                            IPAddress remoteUASAddress = SIPEndPoint.ParseSIPEndPoint(sipResponse.Header.ProxyReceivedFrom).Address;
                                            if (IPSocket.IsPrivateAddress(remoteUASAddress.ToString()) && m_sipCallDescriptor.MangleIPAddress != null)
                                            {
                                                // If the response has arrived here on a private IP address then it must be
                                                // for a local version install and an incoming call that needs it's response mangled.
                                                if(!IPSocket.IsPrivateAddress(m_sipCallDescriptor.MangleIPAddress.ToString()))
                                                {
                                                    publicIPAddress = m_sipCallDescriptor.MangleIPAddress.ToString();
                                                }
                                            }
                                            else
                                            {
                                                publicIPAddress = remoteUASAddress.ToString();
                                            }
                                        }
                                        else if (!IPSocket.IsPrivateAddress(remoteEndPoint.Address.ToString()) && remoteEndPoint.Address != IPAddress.Any)
                                        {
                                            publicIPAddress = remoteEndPoint.Address.ToString();
                                        }
                                        else if (m_sipCallDescriptor.MangleIPAddress != null)
                                        {
                                            publicIPAddress = m_sipCallDescriptor.MangleIPAddress.ToString();
                                        }

                                        if (publicIPAddress != null)
                                        {
                                            sipResponse.Body = SIPPacketMangler.MangleSDP(sipResponse.Body, publicIPAddress, out wasSDPMangled);
                                        }

                                        if (wasSDPMangled)
                                        {
                                            Log_External(new SIPMonitorConsoleEvent(SIPMonitorServerTypesEnum.AppServer, SIPMonitorEventTypesEnum.DialPlan, "SDP on UAC response had RTP socket mangled from " + sdpEndPoint.ToString() + " to " + publicIPAddress + ":" + sdpEndPoint.Port + ".", Owner));
                                        }
                                        else if (sdpEndPoint != null)
                                        {
                                            Log_External(new SIPMonitorConsoleEvent(SIPMonitorServerTypesEnum.AppServer, SIPMonitorEventTypesEnum.DialPlan, "SDP on UAC response could not be mangled, RTP socket " + sdpEndPoint.ToString() + ".", Owner));
                                        }
                                    }
                                }
                                else
                                {
                                    Log_External(new SIPMonitorConsoleEvent(SIPMonitorServerTypesEnum.AppServer, SIPMonitorEventTypesEnum.DialPlan, "SDP RTP socket on UAC response could not be determined.", Owner));
                                }
                            }
                        }

                        m_sipDialogue = new SIPDialogue(m_serverTransaction, Owner, AdminMemberId);
                        m_sipDialogue.CallDurationLimit = m_sipCallDescriptor.CallDurationLimit;

                        // Set switchboard dialogue values from the answered response or from dialplan set values.
                        //m_sipDialogue.SwitchboardCallerDescription = sipResponse.Header.SwitchboardCallerDescription;
                        m_sipDialogue.SwitchboardLineName = sipResponse.Header.SwitchboardLineName;
                        m_sipDialogue.CRMPersonName = sipResponse.Header.CRMPersonName;
                        m_sipDialogue.CRMCompanyName = sipResponse.Header.CRMCompanyName;
                        m_sipDialogue.CRMPictureURL = sipResponse.Header.CRMPictureURL;

                        if (m_sipCallDescriptor.SwitchboardHeaders != null)
                        {
                            //if (!m_sipCallDescriptor.SwitchboardHeaders.SwitchboardDialogueDescription.IsNullOrBlank())
                            //{
                            //    m_sipDialogue.SwitchboardDescription = m_sipCallDescriptor.SwitchboardHeaders.SwitchboardDialogueDescription;
                            //}

                            m_sipDialogue.SwitchboardLineName = m_sipCallDescriptor.SwitchboardHeaders.SwitchboardLineName;
                            m_sipDialogue.SwitchboardOwner = m_sipCallDescriptor.SwitchboardHeaders.SwitchboardOwner;
                        }
                    }

                    FireCallAnswered(this, sipResponse);
                }
            }
            catch (Exception excp)
            {
                Log_External(new SIPMonitorConsoleEvent(SIPMonitorServerTypesEnum.UserAgentClient, SIPMonitorEventTypesEnum.Error, "Exception ServerFinalResponseReceived. " + excp.Message, Owner));
            }
        }

        private void ServerInformationResponseReceived(SIPEndPoint localSIPEndPoint, SIPEndPoint remoteEndPoint, SIPTransaction sipTransaction, SIPResponse sipResponse)
        {
            Log_External(new SIPMonitorConsoleEvent(SIPMonitorServerTypesEnum.UserAgentClient, SIPMonitorEventTypesEnum.DialPlan, "Information response " + sipResponse.StatusCode + " " + sipResponse.ReasonPhrase + " for " + m_serverTransaction.TransactionRequest.URI.ToString() + ".", Owner));

            if (m_callCancelled)
            {
                // Call was cancelled in the interim.
                Cancel();
            }
            else
            {
                if (sipResponse.Status == SIPResponseStatusCodesEnum.Ringing || sipResponse.Status == SIPResponseStatusCodesEnum.SessionProgress)
                {
                    FireCallRinging(this, sipResponse);
                }
                else
                {
                    FireCallTrying(this, sipResponse);
                }
            }
        }

        private void ServerTimedOut(SIPTransaction sipTransaction)
        {
            if (!m_callCancelled)
            {
                FireCallFailed(this, "Timeout, no response from server");
            }
        }

        //private void ByeFinalResponseReceived(IPEndPoint localEndPoint, IPEndPoint remoteEndPoint, SIPTransaction sipTransaction, SIPResponse sipResponse)
        //{
        //    Log_External(new SIPMonitorConsoleEvent(SIPMonitorServerTypesEnum.UserAgentClient, SIPMonitorEventTypesEnum.DialPlan, "BYE response " + sipResponse.StatusCode + " " + sipResponse.ReasonPhrase + ".", Owner));
        //}

        private SIPRequest GetInviteRequest(SIPCallDescriptor sipCallDescriptor, string branchId, string callId, SIPEndPoint localSIPEndPoint, SIPRouteSet routeSet, string content, string contentType)
        {
            SIPRequest inviteRequest = new SIPRequest(SIPMethodsEnum.INVITE, sipCallDescriptor.Uri);
            inviteRequest.LocalSIPEndPoint = localSIPEndPoint;

            SIPHeader inviteHeader = new SIPHeader(sipCallDescriptor.GetFromHeader(), SIPToHeader.ParseToHeader(sipCallDescriptor.To), 1, callId);

            inviteHeader.From.FromTag = CallProperties.CreateNewTag();

            // For incoming calls forwarded via the dial plan the username needs to go into the Contact header.
            inviteHeader.Contact = new List<SIPContactHeader>() { new SIPContactHeader(null, new SIPURI(inviteRequest.URI.Scheme, localSIPEndPoint)) };
            inviteHeader.Contact[0].ContactURI.User = sipCallDescriptor.Username;
            inviteHeader.CSeqMethod = SIPMethodsEnum.INVITE;
            inviteHeader.UserAgent = m_userAgent;
            inviteHeader.Routes = routeSet;
            inviteRequest.Header = inviteHeader;

            if (!sipCallDescriptor.ProxySendFrom.IsNullOrBlank())
            {
                inviteHeader.ProxySendFrom = sipCallDescriptor.ProxySendFrom;
            }

            SIPViaHeader viaHeader = new SIPViaHeader(localSIPEndPoint, branchId);
            inviteRequest.Header.Vias.PushViaHeader(viaHeader);

            inviteRequest.Body = content;
            inviteRequest.Header.ContentLength = (inviteRequest.Body != null) ? inviteRequest.Body.Length : 0;
            inviteRequest.Header.ContentType = contentType;

            // Add custom switchboard headers.
            if (CallDescriptor.SwitchboardHeaders != null)
            {
                inviteHeader.SwitchboardOriginalCallID = CallDescriptor.SwitchboardHeaders.SwitchboardOriginalCallID;
                //inviteHeader.SwitchboardCallerDescription = CallDescriptor.SwitchboardHeaders.SwitchboardCallerDescription;
                inviteHeader.SwitchboardLineName = CallDescriptor.SwitchboardHeaders.SwitchboardLineName;
                //inviteHeader.SwitchboardOwner = CallDescriptor.SwitchboardHeaders.SwitchboardOwner;
                //inviteHeader.SwitchboardOriginalFrom = CallDescriptor.SwitchboardHeaders.SwitchboardOriginalFrom;
            }

            // Add custom CRM headers.
            if (CallDescriptor.CRMHeaders != null)
            {
                inviteHeader.CRMPersonName = CallDescriptor.CRMHeaders.PersonName;
                inviteHeader.CRMCompanyName = CallDescriptor.CRMHeaders.CompanyName;
                inviteHeader.CRMPictureURL = CallDescriptor.CRMHeaders.AvatarURL;
            }

            try
            {
                if (sipCallDescriptor.CustomHeaders != null && sipCallDescriptor.CustomHeaders.Count > 0)
                {
                    foreach (string customHeader in sipCallDescriptor.CustomHeaders)
                    {
                        if (customHeader.IsNullOrBlank())
                        {
                            continue;
                        }
                        else if (customHeader.Trim().StartsWith(SIPHeaders.SIP_HEADER_USERAGENT))
                        {
                            inviteRequest.Header.UserAgent = customHeader.Substring(customHeader.IndexOf(":") + 1).Trim();
                        }
                        else
                        {
                            inviteRequest.Header.UnknownHeaders.Add(customHeader);
                        }
                    }
                }
            }
            catch (Exception excp)
            {
                logger.Error("Exception Parsing CustomHeader for GetInviteRequest. " + excp.Message + sipCallDescriptor.CustomHeaders);
            }

            return inviteRequest;
        }

        private SIPRequest GetCancelRequest(SIPRequest inviteRequest)
        {
            SIPRequest cancelRequest = new SIPRequest(SIPMethodsEnum.CANCEL, inviteRequest.URI);
            cancelRequest.LocalSIPEndPoint = inviteRequest.LocalSIPEndPoint;

            SIPHeader inviteHeader = inviteRequest.Header;
            SIPHeader cancelHeader = new SIPHeader(inviteHeader.From, inviteHeader.To, inviteHeader.CSeq, inviteHeader.CallId);
            cancelRequest.Header = cancelHeader;
            cancelHeader.CSeqMethod = SIPMethodsEnum.CANCEL;
            cancelHeader.Routes = inviteHeader.Routes;
            cancelHeader.ProxySendFrom = inviteHeader.ProxySendFrom;
            cancelHeader.Vias = inviteHeader.Vias;

            return cancelRequest;
        }

        private SIPRequest GetByeRequest(SIPResponse inviteResponse, SIPURI byeURI, SIPEndPoint localSIPEndPoint)
        {
            SIPRequest byeRequest = new SIPRequest(SIPMethodsEnum.BYE, byeURI);
            byeRequest.LocalSIPEndPoint = localSIPEndPoint;

            SIPFromHeader byeFromHeader = inviteResponse.Header.From;
            SIPToHeader byeToHeader = inviteResponse.Header.To;
            int cseq = inviteResponse.Header.CSeq + 1;

            SIPHeader byeHeader = new SIPHeader(byeFromHeader, byeToHeader, cseq, inviteResponse.Header.CallId);
            byeHeader.CSeqMethod = SIPMethodsEnum.BYE;
            byeHeader.ProxySendFrom = m_serverTransaction.TransactionRequest.Header.ProxySendFrom;
            byeRequest.Header = byeHeader;

            byeRequest.Header.Routes = (inviteResponse.Header.RecordRoutes != null) ? inviteResponse.Header.RecordRoutes.Reversed() : null;

            SIPViaHeader viaHeader = new SIPViaHeader(localSIPEndPoint, CallProperties.CreateBranchId());
            byeRequest.Header.Vias.PushViaHeader(viaHeader);

            return byeRequest;
        }

        private SIPRequest GetUpdateRequest(SIPRequest inviteRequest, CRMHeaders crmHeaders)
        {
            SIPRequest updateRequest = new SIPRequest(SIPMethodsEnum.UPDATE, inviteRequest.URI);
            updateRequest.LocalSIPEndPoint = inviteRequest.LocalSIPEndPoint;

            SIPHeader inviteHeader = inviteRequest.Header;
            SIPHeader updateHeader = new SIPHeader(inviteHeader.From, inviteHeader.To, inviteHeader.CSeq + 1, inviteHeader.CallId);
            inviteRequest.Header.CSeq++;
            updateRequest.Header = updateHeader;
            updateHeader.CSeqMethod = SIPMethodsEnum.UPDATE;
            updateHeader.Routes = inviteHeader.Routes;
            updateHeader.ProxySendFrom = inviteHeader.ProxySendFrom;

            SIPViaHeader viaHeader = new SIPViaHeader(inviteRequest.LocalSIPEndPoint, CallProperties.CreateBranchId());
            updateHeader.Vias.PushViaHeader(viaHeader);

            // Add custom CRM headers.
            if (crmHeaders != null)
            {
                updateHeader.CRMPersonName = crmHeaders.PersonName;
                updateHeader.CRMCompanyName = crmHeaders.CompanyName;
                updateHeader.CRMPictureURL = crmHeaders.AvatarURL;
            }

            return updateRequest;
        }

        private void TransactionTraceMessage(SIPTransaction sipTransaction, string message)
        {
            Log_External(new SIPMonitorConsoleEvent(SIPMonitorServerTypesEnum.UserAgentClient, SIPMonitorEventTypesEnum.SIPTransaction, message, Owner));
        }

        private void FireCallTrying(SIPClientUserAgent uac, SIPResponse tryingResponse)
        {
            if (CallTrying != null)
            {
                CallTrying(uac, tryingResponse);
            }
        }

        private void FireCallRinging(SIPClientUserAgent uac, SIPResponse ringingResponse)
        {
            if (CallRinging != null)
            {
                CallRinging(uac, ringingResponse);
            }
        }

        private void FireCallAnswered(SIPClientUserAgent uac, SIPResponse answeredResponse)
        {
            if (CallAnswered != null)
            {
                CallAnswered(uac, answeredResponse);
            }
        }

        private void FireCallFailed(SIPClientUserAgent uac, string errorMessage)
        {
            if (CallFailed != null)
            {
                CallFailed(uac, errorMessage);
            }
        }
    }
}
