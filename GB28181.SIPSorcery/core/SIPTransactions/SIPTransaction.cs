//-----------------------------------------------------------------------------
// Filename: SIPTransaction.cs
//
// Description: SIP Transaction.
// 
// History:
// 14 Feb 2006	Aaron Clauson	Created.
// 30 May 2020	Edward Chen     Updated.
// License: 
// BSD 3-Clause "New" or "Revised" License, see included LICENSE.md file.
//

using System;
using GB28181.Logger4Net;
using SIPSorcery.SIP;


namespace GB28181
{

    public class SIPTransaction:SIPSorcery.SIP.SIPTransaction
    {
        protected static ILog logger = AssemblyState.logger;

        protected static readonly int m_t1 = SIPTimings.T1;                     // SIP Timer T1 in milliseconds.
        protected static readonly int m_t6 = SIPTimings.T6;                     // SIP Timer T1 in milliseconds.
        private static string m_crLF = SIPConstants.CRLF;

        private string m_transactionId;

        private string m_sentBy;                        // The contact address from the top Via header that created the transaction. This is used for matching requests to server transactions.

        protected SIPRequest m_ackRequest;                  // ACK request for INVITE transactions.
        protected SIPEndPoint m_ackRequestIPEndPoint;       // Socket the ACK request was sent to.


        public SIPUserField TransactionRequestFrom => m_transactionRequest?.Header.From.FromUserField;

        public SIPEndPoint RemoteEndPoint;                  // The remote socket that caused the transaction to be created or the socket a newly created transaction request was sent to.             
        public SIPEndPoint LocalSIPEndPoint;                // The local SIP endpoint the remote request was received on the if created by this stack the local SIP end point used to send the transaction.
        public SIPCDR CDR;

        private SIPTransactionStatesEnum m_transactionState = SIPTransactionStatesEnum.Calling;
        public SIPTransactionStatesEnum TransactionState => m_transactionState;

        protected SIPRequest m_transactionRequest;          // This is the request which started the transaction and on which it is based.
        public SIPRequest TransactionRequest
        {
            get { return m_transactionRequest; }
        }

        protected SIPResponse m_transactionFinalResponse;   // This is the final response being sent by a UAS transaction or the one received by a UAC one.
        public SIPResponse TransactionFinalResponse
        {
            get { return m_transactionFinalResponse; }
        }

        // These are the events that will normally be required by upper level transaction users such as registration or call agents.
        protected event SIPTransactionRequestReceivedDelegate TransactionRequestReceived;
        //protected event SIPTransactionAuthenticationRequiredDelegate TransactionAuthenticationRequired;
        protected event SIPTransactionResponseReceivedDelegate TransactionInformationResponseReceived;
        protected event SIPTransactionResponseReceivedDelegate TransactionFinalResponseReceived;
        protected event SIPTransactionTimedOutDelegate TransactionTimedOut;

        // These events are normally only used for housekeeping such as retransmits on ACK's.
        protected event SIPTransactionResponseReceivedDelegate TransactionDuplicateResponse;
        protected event SIPTransactionRequestRetransmitDelegate TransactionRequestRetransmit;
        protected event SIPTransactionResponseRetransmitDelegate TransactionResponseRetransmit;

        // Events that don't affect the transaction processing, i.e. used for logging/tracing.
        public event SIPTransactionStateChangeDelegate TransactionStateChanged;
        public event SIPTransactionTraceMessageDelegate TransactionTraceMessage;

        public event SIPTransactionRemovedDelegate TransactionRemoved;       // This is called just before the SIPTransaction is expired and is to let consumer classes know to remove their event handlers to prevent memory leaks.

        public long TransactionsCreated = 0;
        public long TransactionsDestroyed = 0;

        private SIPTransport m_sipTransport;

        /// <summary>
        /// Creates a new SIP transaction and adds it to the list of in progress transactions.
        /// </summary>
        /// <param name="sipTransport">The SIP Transport layer that is to be used with the transaction.</param>
        /// <param name="transactionRequest">The SIP Request on which the transaction is based.</param>
        /// <param name="dstEndPoint">The socket the at the remote end of the transaction and which transaction messages will be sent to.</param>
        /// <param name="localSIPEndPoint">The socket that should be used as the send from socket for communications on this transaction. Typically this will
        /// be the socket the initial request was received on.</param>
        protected SIPTransaction(
            SIPTransport sipTransport,
            SIPRequest transactionRequest,
            SIPEndPoint dstEndPoint,
            SIPEndPoint localSIPEndPoint,
            SIPEndPoint outboundProxy):base(sipTransport, transactionRequest, outboundProxy)
        {
          
                TransactionsCreated++;

                m_sipTransport = sipTransport;
                m_transactionId = GetRequestTransactionId(transactionRequest.Header.Vias.TopViaHeader.Branch, transactionRequest.Header.CSeqMethod);
                HasTimedOut = false;

                m_transactionRequest = transactionRequest;
                m_branchId = transactionRequest.Header.Vias.TopViaHeader.Branch;
                m_callId = transactionRequest.Header.CallId;
                m_sentBy = transactionRequest.Header.Vias.TopViaHeader.ContactAddress;
                RemoteEndPoint = dstEndPoint;
                LocalSIPEndPoint = localSIPEndPoint;
                OutboundProxy = outboundProxy;
         
        }

        //public static string GetRequestTransactionId(string branchId, SIPMethodsEnum method)
        //{
        //    return Crypto.GetSHAHashAsString(branchId + method.ToString());
        //}

        public void GotRequest(SIPEndPoint localSIPEndPoint, SIPEndPoint remoteEndPoint, SIPRequest sipRequest)
        {
            FireTransactionTraceMessage("Received Request " + localSIPEndPoint.ToString() + "<-" + remoteEndPoint.ToString() + m_crLF + sipRequest.ToString());

            TransactionRequestReceived?.Invoke(localSIPEndPoint, remoteEndPoint, this, sipRequest);
        }

        public void GotResponse(SIPEndPoint localSIPEndPoint, SIPEndPoint remoteEndPoint, SIPResponse sipResponse)
        {
            if (m_transactionState == SIPTransactionStatesEnum.Completed || m_transactionState == SIPTransactionStatesEnum.Confirmed)
            {
                FireTransactionTraceMessage("Received Duplicate Response " + localSIPEndPoint.ToString() + "<-" + remoteEndPoint + m_crLF + sipResponse.ToString());

                if (sipResponse.Header.CSeqMethod == SIPMethodsEnum.INVITE)
                {
                    if (sipResponse.StatusCode >= 100 && sipResponse.StatusCode <= 199)
                    {
                        // Ignore info response on completed transaction.
                    }
                    else
                    {
                        ResendAckRequest();
                    }
                }

                TransactionDuplicateResponse?.Invoke(localSIPEndPoint, remoteEndPoint, this, sipResponse);
            }
            else
            {
                FireTransactionTraceMessage("Received Response " + localSIPEndPoint.ToString() + "<-" + remoteEndPoint + m_crLF + sipResponse.ToString());

                if (sipResponse.StatusCode >= 100 && sipResponse.StatusCode <= 199)
                {
                    UpdateTransactionState(SIPTransactionStatesEnum.Proceeding);

                    if (sipResponse.Header.CSeqMethod == SIPMethodsEnum.INVITE)
                    {
                        //ignore the respose of 100 trying 
                    }
                    else
                    {
                        TransactionInformationResponseReceived?.Invoke(localSIPEndPoint, remoteEndPoint, this, sipResponse);
                    }


                }
                else
                {
                    m_transactionFinalResponse = sipResponse;
                    UpdateTransactionState(SIPTransactionStatesEnum.Completed);
                    TransactionFinalResponseReceived?.Invoke(localSIPEndPoint, remoteEndPoint, this, sipResponse);
                }
            }
        }

        private void UpdateTransactionState(SIPTransactionStatesEnum transactionState)
        {
            m_transactionState = transactionState;

            if (transactionState == SIPTransactionStatesEnum.Confirmed || transactionState == SIPTransactionStatesEnum.Terminated || transactionState == SIPTransactionStatesEnum.Cancelled)
            {
                DeliveryPending = false;
            }
            else if (transactionState == SIPTransactionStatesEnum.Completed)
            {
                CompletedAt = DateTime.Now;
            }

            if (TransactionStateChanged != null && transactionState != SIPTransactionStatesEnum.Proceeding && transactionState != SIPTransactionStatesEnum.Calling)
            {
                FireTransactionStateChangedEvent();
            }
        }

        public virtual void SendFinalResponse(SIPResponse finalResponse)
        {
            m_transactionFinalResponse = finalResponse;
            UpdateTransactionState(SIPTransactionStatesEnum.Completed);
            string viaAddress = finalResponse.Header.Vias.TopViaHeader.ReceivedFromAddress;

            if (TransactionType == SIPTransactionTypesEnum.InviteServer)
            {
                FireTransactionTraceMessage("Send Final Response Reliable " + LocalSIPEndPoint.ToString() + "->" + viaAddress + m_crLF + finalResponse.ToString());
                m_sipTransport.SendSIPReliable(this);
            }
            else
            {
                FireTransactionTraceMessage("Send Final Response " + LocalSIPEndPoint.ToString() + "->" + viaAddress + m_crLF + finalResponse.ToString());
                m_sipTransport.SendResponse(finalResponse);
            }
        }

        public virtual void SendInformationalResponse(SIPResponse sipResponse)
        {
            FireTransactionTraceMessage("Send Info Response " + LocalSIPEndPoint.ToString() + "->" + this.RemoteEndPoint + m_crLF + sipResponse.ToString());

            if (sipResponse.StatusCode == 100)
            {
                UpdateTransactionState(SIPTransactionStatesEnum.Trying);
            }
            else if (sipResponse.StatusCode > 100 && sipResponse.StatusCode <= 199)
            {
                UpdateTransactionState(SIPTransactionStatesEnum.Proceeding);
            }

            m_sipTransport.SendResponse(sipResponse);
        }

        public void RetransmitFinalResponse()
        {
            try
            {
                if (TransactionFinalResponse != null && m_transactionState != SIPTransactionStatesEnum.Confirmed)
                {
                    m_sipTransport.SendResponse(TransactionFinalResponse);
                    Retransmits += 1;
                    LastTransmit = DateTime.Now;
                    ResponseRetransmit();
                }
            }
            catch (Exception excp)
            {
                logger.Error("Exception RetransmitFinalResponse. " + excp.Message);
            }
        }

        public void SendRequest(SIPEndPoint dstEndPoint, SIPRequest sipRequest)
        {
            FireTransactionTraceMessage("Send Request " + LocalSIPEndPoint.ToString() + "->" + dstEndPoint + m_crLF + sipRequest.ToString());

            if (sipRequest.Method == SIPMethodsEnum.ACK)
            {
                m_ackRequest = sipRequest;
                m_ackRequestIPEndPoint = dstEndPoint;
            }

            m_sipTransport.SendRequest(dstEndPoint, sipRequest);
        }

        public void SendRequest(SIPRequest sipRequest)
        {
            m_sipTransport.SendRequestAsync(sipRequest);
        }

        public void SendReliableRequest()
        {
            FireTransactionTraceMessage("Send Request reliable " + LocalSIPEndPoint.ToString() + "->" + RemoteEndPoint + m_crLF + TransactionRequest.ToString());

            if (TransactionType == SIPTransactionTypesEnum.InviteServer && TransactionRequest.Method == SIPMethodsEnum.INVITE)
            {
                UpdateTransactionState(SIPTransactionStatesEnum.Calling);
            }

            m_sipTransport.SendSIPReliable(this);
        }

        protected SIPResponse GetInfoResponse(SIPRequest sipRequest, SIPResponseStatusCodesEnum sipResponseCode)
        {
            try
            {
                SIPResponse informationalResponse = new SIPResponse(sipResponseCode, null, sipRequest.LocalSIPEndPoint);

                SIPHeader requestHeader = sipRequest.Header;
                informationalResponse.Header = new SIPHeader(requestHeader.From, requestHeader.To, requestHeader.CSeq, requestHeader.CallId)
                {
                    CSeqMethod = requestHeader.CSeqMethod,
                    Vias = requestHeader.Vias,
                    MaxForwards = Int32.MinValue,
                    Timestamp = requestHeader.Timestamp
                };

                return informationalResponse;
            }
            catch (Exception excp)
            {
                logger.Error("Exception GetInformationalResponse. " + excp.Message);
                throw excp;
            }
        }

        internal void RemoveEventHandlers()
        {
            // Remove all event handlers.
            TransactionRequestReceived = null;
            TransactionInformationResponseReceived = null;
            TransactionFinalResponseReceived = null;
            TransactionTimedOut = null;
            TransactionDuplicateResponse = null;
            TransactionRequestRetransmit = null;
            TransactionResponseRetransmit = null;
            TransactionStateChanged = null;
            TransactionTraceMessage = null;
            TransactionRemoved = null;
        }

        public void RequestRetransmit()
        {
            if (TransactionRequestRetransmit != null)
            {
                try
                {
                    TransactionRequestRetransmit(this, this.TransactionRequest, this.Retransmits);
                }
                catch (Exception excp)
                {
                    logger.Error("Exception TransactionRequestRetransmit. " + excp.Message);
                }
            }

            FireTransactionTraceMessage("Send Request retransmit " + Retransmits + " " + LocalSIPEndPoint.ToString() + "->" + this.RemoteEndPoint + m_crLF + this.TransactionRequest.ToString());
        }

        private void ResponseRetransmit()
        {
            if (TransactionResponseRetransmit != null)
            {
                try
                {
                    TransactionResponseRetransmit(this, this.TransactionFinalResponse, this.Retransmits);
                }
                catch (Exception excp)
                {
                    logger.Error("Exception TransactionResponseRetransmit. " + excp.Message);
                }
            }

            FireTransactionTraceMessage("Send Response retransmit " + LocalSIPEndPoint.ToString() + "->" + this.RemoteEndPoint + m_crLF + this.TransactionFinalResponse.ToString());
        }

        public void ACKReceived(SIPEndPoint localSIPEndPoint, SIPEndPoint remoteEndPoint, SIPRequest sipRequest)
        {
            UpdateTransactionState(SIPTransactionStatesEnum.Confirmed);
        }

        public void ResendAckRequest()
        {
            if (m_ackRequest != null)
            {
                SendRequest(m_ackRequest);
                AckRetransmits += 1;
                LastTransmit = DateTime.Now;
                //RequestRetransmit();
            }
            else
            {
                logger.Warn("An ACK retransmit was required but there was no stored ACK request to send.");
            }
        }




        protected void Cancel()
        {
            UpdateTransactionState(SIPTransactionStatesEnum.Cancelled);
        }

        private void FireTransactionStateChangedEvent()
        {
            FireTransactionTraceMessage("Transaction state changed to " + m_transactionState + ".");
            TransactionStateChanged?.Invoke(this);
        }

        private void FireTransactionTraceMessage(string message)
        {
            TransactionTraceMessage?.Invoke(this, message);

        }

        ~SIPTransaction()
        {
            TransactionsDestroyed++;
        }

        #region Unit testing.

#if UNITTEST

		[TestFixture]
		public class SIPTransactionUnitTest
		{
            private class MockSIPDNSManager
            {
                public static SIPDNSLookupResult Resolve(SIPURI sipURI, bool async)
                {
                    // This assumes the input SIP URI has an IP address as the host!
                    return new SIPDNSLookupResult(sipURI, new SIPEndPoint(IPSocket.ParseSocketString(sipURI.Host)));
                }
            }

            protected static readonly string m_CRLF = SIPConstants.CRLF; 
            
            [TestFixtureSetUp]
			public void Init()
			{}
	
			[TestFixtureTearDown]
			public void Dispose()
			{}
			
			[Test]
			public void CreateTransactionUnitTest()
			{
				Console.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name);

                string sipRequestStr =
                    "INVITE sip:023434211@213.200.94.182;switchtag=902888 SIP/2.0" + m_CRLF +
                    "Record-Route: <sip:2.3.4.5;ftag=9307C640-33C;lr=on>" + m_CRLF +
                    "Via: SIP/2.0/UDP  5.6.7.2:5060" + m_CRLF +
                    "Via: SIP/2.0/UDP 1.2.3.4;branch=z9hG4bKa7ac.2bfad091.0" + m_CRLF +
                    "From: \"unknown\" <sip:00.000.00.0>;tag=9307C640-33C" + m_CRLF +
                    "To: <sip:0113001211@82.209.165.194>" + m_CRLF +
                    "Date: Thu, 21 Feb 2008 01:46:30 GMT" + m_CRLF +
                    "Call-ID: A8706191-DF5511DC-B886ED7B-395C3F7E" + m_CRLF +
                    "Supported: timer,100rel" + m_CRLF +
                    "Min-SE:  1800" + m_CRLF +
                    "Cisco-Guid: 2825897321-3746894300-3095653755-962346878" + m_CRLF +
                    "User-Agent: Cisco-SIPGateway/IOS-12.x" + m_CRLF +
                    "Allow: INVITE, OPTIONS, BYE, CANCEL, ACK, PRACK, COMET, REFER, SUBSCRIBE, NOTIFY, INFO" + m_CRLF +
                    "CSeq: 101 INVITE" + m_CRLF +
                    "Max-Forwards: 5" + m_CRLF +
                    "Timestamp: 1203558390" + m_CRLF +
                    "Contact: <sip:1.2.3.4:5060>" + m_CRLF +
                    "Expires: 180" + m_CRLF +
                    "Allow-Events: telephone-event" + m_CRLF +
                    "Content-Type: application/sdp" + m_CRLF +
                    "Content-Length: 370" + m_CRLF +
                     m_CRLF +
                    "v=0" + m_CRLF +
                    "o=CiscoSystemsSIP-GW-UserAgent 9312 7567 IN IP4 00.00.00.0" + m_CRLF +
                    "s=SIP Call" + m_CRLF +
                    "c=IN IP4 00.000.00.0" + m_CRLF +
                    "t=0 0" + m_CRLF +
                    "m=audio 16434 RTP/AVP 8 0 4 18 3 101" + m_CRLF +
                    "c=IN IP4 00.000.00.0" + m_CRLF +
                    "a=rtpmap:8 PCMA/8000" + m_CRLF +
                    "a=rtpmap:0 PCMU/8000" + m_CRLF +
                    "a=rtpmap:4 G723/8000" + m_CRLF +
                    "a=fmtp:4 annexa=no" + m_CRLF +
                    "a=rtpmap:18 G729/8000" + m_CRLF +
                    "a=fmtp:18 annexb=no" + m_CRLF +
                    "a=rtpmap:3 GSM/8000" + m_CRLF +
                    "a=rtpmap:101 telepho";

                SIPRequest request = SIPRequest.ParseSIPRequest(sipRequestStr);
                SIPTransactionEngine transactionEngine = new SIPTransactionEngine();
                SIPTransport sipTransport = new SIPTransport(MockSIPDNSManager.Resolve, transactionEngine);
                SIPEndPoint dummySIPEndPoint = new SIPEndPoint(new IPEndPoint(IPAddress.Loopback, 1234));
                SIPTransaction transaction = sipTransport.CreateUACTransaction(request, dummySIPEndPoint, dummySIPEndPoint, null);

                Assert.IsTrue(transaction.TransactionRequest.URI.ToString() == "sip:023434211@213.200.94.182;switchtag=902888", "Transaction request URI was incorrect.");
			}
        }

#endif

        #endregion
    }
}
