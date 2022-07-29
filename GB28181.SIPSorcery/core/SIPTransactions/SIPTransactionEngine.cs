using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using GB28181.Logger4Net;
using SIPSorcery.SIP;

namespace GB28181
{
    public class SIPTransactionEngine : ISIPTransactionEngine
    {
        protected static ILog logger = AssemblyState.logger;

        private static readonly int m_t6 = SIPTimings.T6;
        protected static readonly int m_maxRingTime = SIPTimings.MAX_RING_TIME; // Max time an INVITE will be left ringing for (typically 10 mins).    
        /// <summary>
        /// The maximum number of pending transactions that can be outstanding.
        /// </summary>
        internal static int MaxReliableTranismissionsCount = 5000;

        /// <summary>
        /// Contains a list of the transactions that are being monitored or responses and retransmitted 
        /// on when none is received to attempt a more reliable delivery rather then just relying on the initial 
        /// request to get through.
        /// </summary>
        private ConcurrentDictionary<string, SIPTransaction> m_pendingTransactions = new ConcurrentDictionary<string, SIPTransaction>();
        private AutoResetEvent m_newPendingTransactionEvent = new AutoResetEvent(false);
        private bool m_disposed;

        public int TransactionsCount
        {
            get { return m_pendingTransactions.Count; }
        }

        public void AddTransaction(SIPTransaction sipTransaction)
        {
            if (m_pendingTransactions.Count > MaxReliableTranismissionsCount)
            {
                throw new ApplicationException("Pending transactions list is full.");
            }
            else if (!m_pendingTransactions.ContainsKey(sipTransaction.TransactionId))
            {
                if (!m_pendingTransactions.TryAdd(sipTransaction.TransactionId, sipTransaction))
                {
                    throw new ApplicationException("Failed to add transaction to pending list.");
                }

                m_newPendingTransactionEvent.Set();
            }
        }

        public bool Exists(SIPRequest sipRequest)
        {
            return GetTransaction(sipRequest) != null;
        }

        public bool Exists(SIPResponse sipResponse)
        {
            return GetTransaction(sipResponse) != null;
        }

        /// <summary>
        /// Transaction matching see RFC3261 17.1.3 & 17.2.3 for matching client and server transactions respectively. 
        /// IMPORTANT NOTE this transaction matching applies to all requests and responses EXCEPT ACK requests to 2xx responses see 13.2.2.4. 
        /// For ACK's to 2xx responses the ACK represents a separate transaction. However for a UAS sending an INVITE response the ACK still has to be 
        /// matched to an existing server transaction in order to transition it to a Confirmed state.
        /// 
        /// ACK's:
        ///  - The ACK for a 2xx response will have the same CallId, From Tag and To Tag.
        ///  - An ACK for a non-2xx response will have the same branch ID as the INVITE whose response it acknowledges.
        /// </summary>
        /// <param name="sipRequest"></param>
        /// <returns></returns>
        public SIPTransaction GetTransaction(SIPRequest sipRequest)
        {
            // The branch is mandatory but it doesn't stop some UA's not setting it.
            if (sipRequest.Header.Vias.TopViaHeader.Branch == null || sipRequest.Header.Vias.TopViaHeader.Branch.Trim().Length == 0)
            {
                return null;
            }

            SIPMethodsEnum transactionMethod = (sipRequest.Method != SIPMethodsEnum.ACK) ? sipRequest.Method : SIPMethodsEnum.INVITE;
            string transactionId = SIPTransaction.GetRequestTransactionId(sipRequest.Header.Vias.TopViaHeader.Branch, transactionMethod);

            lock (m_pendingTransactions)
            {
                if (transactionId != null && m_pendingTransactions.ContainsKey(transactionId))
                {
                    return m_pendingTransactions[transactionId];
                }
                else
                {
                    // No normal match found so look of a 2xx INVITE response waiting for an ACK.
                    if (sipRequest.Method == SIPMethodsEnum.ACK)
                    {
                        //logger.LogDebug("Looking for ACK transaction, branchid=" + sipRequest.Header.Via.TopViaHeader.Branch + ".");

                        foreach (var (_, transaction) in m_pendingTransactions)
                        {
                            // According to the standard an ACK should only not get matched by the branchid on the original INVITE for a non-2xx response. However
                            // my Cisco phone created a new branchid on ACKs to 487 responses and since the Cisco also used the same Call-ID and From tag on the initial
                            // unauthenticated request and the subsequent authenticated request the condition below was found to be the best way to match the ACK.
                            /*if (transaction.TransactionType == SIPTransactionTypesEnum.Invite && transaction.TransactionFinalResponse != null && transaction.TransactionState == SIPTransactionStatesEnum.Completed)
                            {
                                if (transaction.TransactionFinalResponse.Header.CallId == sipRequest.Header.CallId &&
                                    transaction.TransactionFinalResponse.Header.To.ToTag == sipRequest.Header.To.ToTag &&
                                    transaction.TransactionFinalResponse.Header.From.FromTag == sipRequest.Header.From.FromTag)
                                {
                                    return transaction;
                                }
                            }*/

                            // As an experiment going to try matching on the Call-ID. This field seems to be unique and therefore the chance
                            // of collisions seemingly very slim. As a safeguard if there happen to be two transactions with the same Call-ID in the list the match will not be made.
                            // One case where the Call-Id match breaks down is for in-Dialogue requests in that case there will be multiple transactions with the same Call-ID and tags.
                            //if (transaction.TransactionType == SIPTransactionTypesEnum.Invite && transaction.TransactionFinalResponse != null && transaction.TransactionState == SIPTransactionStatesEnum.Completed)
                            if ((transaction.TransactionType == SIPTransactionTypesEnum.InviteClient || transaction.TransactionType == SIPTransactionTypesEnum.InviteServer)
                                && transaction.TransactionFinalResponse != null)
                            {
                                if (transaction.TransactionRequest.Header.CallId == sipRequest.Header.CallId &&
                                    transaction.TransactionFinalResponse.Header.To.ToTag == sipRequest.Header.To.ToTag &&
                                    transaction.TransactionFinalResponse.Header.From.FromTag == sipRequest.Header.From.FromTag &&
                                    transaction.TransactionFinalResponse.Header.CSeq == sipRequest.Header.CSeq)
                                {
                                    //logger.LogInformation("ACK for contact=" + contactAddress + ", cseq=" + sipRequest.Header.CSeq + " was matched by callid, tags and cseq.");

                                    return transaction;
                                }
                                else if (transaction.TransactionRequest.Header.CallId == sipRequest.Header.CallId &&
                                    transaction.TransactionFinalResponse.Header.CSeq == sipRequest.Header.CSeq &&
                                    IsCallIdUniqueForPending(sipRequest.Header.CallId))
                                {
                                    //string requestEndPoint = (sipRequest.RemoteSIPEndPoint != null) ? sipRequest.RemoteSIPEndPoint.ToString() : " ? ";
                                    //logger.LogInformation("ACK for contact=" + contactAddress + ", cseq=" + sipRequest.Header.CSeq + " was matched using Call-ID mechanism (to tags: " + transaction.TransactionFinalResponse.Header.To.ToTag + "=" + sipRequest.Header.To.ToTag + ", from tags:" + transaction.TransactionFinalResponse.Header.From.FromTag + "=" + sipRequest.Header.From.FromTag + ").");
                                    return transaction;
                                }
                            }
                        }
                    }
                    else if (sipRequest.Method == SIPMethodsEnum.PRACK)
                    {
                        foreach (var (_, transaction) in m_pendingTransactions)
                        {
                            if (transaction.TransactionType == SIPTransactionTypesEnum.InviteServer && transaction.ReliableProvisionalResponse != null)
                            {
                                if (transaction.TransactionRequest.Header.CallId == sipRequest.Header.CallId &&
                                    transaction.ReliableProvisionalResponse.Header.From.FromTag == sipRequest.Header.From.FromTag &&
                                    transaction.ReliableProvisionalResponse.Header.CSeq == sipRequest.Header.RAckCSeq &&
                                    transaction.ReliableProvisionalResponse.Header.RSeq == sipRequest.Header.RAckRSeq &&
                                    transaction.ReliableProvisionalResponse.Header.CSeqMethod == sipRequest.Header.RAckCSeqMethod)
                                {
                                    //logger.LogDebug("PRACK for contact=" + contactAddress + ", cseq=" + sipRequest.Header.CSeq + " was matched by callid, tags and cseq.");

                                    return transaction;
                                }
                            }
                        }
                    }

                    return null;
                }
            }
        }

        public SIPTransaction GetTransaction(SIPResponse sipResponse)
        {
            if (sipResponse.Header.Vias.TopViaHeader.Branch == null || sipResponse.Header.Vias.TopViaHeader.Branch.Trim().Length == 0)
            {
                return null;
            }
            else
            {
                string transactionId = SIPTransaction.GetRequestTransactionId(sipResponse.Header.Vias.TopViaHeader.Branch, sipResponse.Header.CSeqMethod);

                m_pendingTransactions.TryGetValue(transactionId, out var transaction);
                return transaction;
            }
        }

        public SIPTransaction GetTransaction(string transactionId)
        {
            m_pendingTransactions.TryGetValue(transactionId, out var transaction);
            return transaction;
        }

        public void RemoveExpiredTransactions()
        {
            try
            {
                List<string> expiredTransactionIds = new List<string>();
                var now = DateTime.Now;

                foreach (var (_, transaction) in m_pendingTransactions)
                {
                    if (transaction.TransactionType == SIPTransactionTypesEnum.InviteClient || transaction.TransactionType == SIPTransactionTypesEnum.InviteServer)
                    {
                        if (transaction.TransactionState == SIPTransactionStatesEnum.Confirmed)
                        {
                            // Need to wait until the transaction timeout period is reached in case any ACK re-transmits are received.
                            // No proactive actions need to be undertaken in the Confirmed state. If any ACK requests are received
                            // we use this tx to ensure they get matched and not detected as orphans.
                            if (now.Subtract(transaction.CompletedAt).TotalMilliseconds >= m_t6)
                            {
                                expiredTransactionIds.Add(transaction.TransactionId);
                            }
                        }
                        else if (transaction.TransactionState == SIPTransactionStatesEnum.Completed)
                        {
                            // If a server INVITE transaction is in the following state:
                            // - Completed it means we sent a final response but did not receive an ACK 
                            //   (which is what transitions the tx to the Confirmed state).
                            if (now.Subtract(transaction.CompletedAt).TotalMilliseconds >= m_t6)
                            {
                                // It's important that an un-Confirmed server INVITE tx fires the event to
                                // inform the application that the tx timed out. This allows it to make a decision
                                // on whether to clean up resources such as RTP and media or whether to give the
                                // caller the benefit of the doubt and see if it was an ACK only problem and 
                                // give them a chance to send RTP.
                                transaction.Expire(now);
                                expiredTransactionIds.Add(transaction.TransactionId);
                            }
                        }
                        else if (transaction.DeliveryFailed && transaction.TransactionFinalResponse == null)
                        {
                            // This transaction timed out attempting to send the initial request. No
                            // final response was received so it does not need to be kept alive for ACK 
                            // re-transmits.
                            expiredTransactionIds.Add(transaction.TransactionId);
                        }
                        else if (transaction.TransactionState == SIPTransactionStatesEnum.Proceeding)
                        {
                            if (now.Subtract(transaction.Created).TotalMilliseconds >= m_maxRingTime)
                            {
                                // INVITE requests that have been ringing too long. This can apply to both
                                // client and server INVITE transactions.
                                transaction.Expire(now);
                                expiredTransactionIds.Add(transaction.TransactionId);
                            }
                        }
                        else if (now.Subtract(transaction.Created).TotalMilliseconds >= m_t6)
                        {
                            //logger.LogDebug("INVITE transaction (" + transaction.TransactionId + ") " + transaction.TransactionRequestURI.ToString() + " in " + transaction.TransactionState + " has been alive for " + DateTime.Now.Subtract(transaction.Created).TotalSeconds.ToString("0") + ".");

                            // If a client INVITE transaction is in the following states:
                            // - Calling: it means no response of any kind (provisional or final) was received from the server in time.
                            // - Trying: it means all we got was a "100 Trying" response without any follow up progress indications or final response.

                            if (transaction.TransactionState == SIPTransactionStatesEnum.Calling ||
                                transaction.TransactionState == SIPTransactionStatesEnum.Trying)
                            {
                                transaction.Expire(now);
                                expiredTransactionIds.Add(transaction.TransactionId);
                            }
                            else
                            {
                                // The INVITE transaction has ended up in some other state and should be removed.
                                expiredTransactionIds.Add(transaction.TransactionId);
                            }
                        }
                    }
                    else if (transaction.HasTimedOut)
                    {
                        expiredTransactionIds.Add(transaction.TransactionId);
                    }
                    else if (now.Subtract(transaction.Created).TotalMilliseconds >= m_t6)
                    {
                        if (transaction.TransactionState == SIPTransactionStatesEnum.Calling ||
                           transaction.TransactionState == SIPTransactionStatesEnum.Trying ||
                           transaction.TransactionState == SIPTransactionStatesEnum.Proceeding)
                        {
                            //logger.LogWarning("Timed out transaction in SIPTransactionEngine, should have been timed out in the SIP Transport layer. " + transaction.TransactionRequest.Method + ".");
                            transaction.Expire(now);
                        }

                        expiredTransactionIds.Add(transaction.TransactionId);
                    }
                }

                foreach (string transactionId in expiredTransactionIds)
                {
                    if (m_pendingTransactions.ContainsKey(transactionId))
                    {
                        SIPTransaction expiredTransaction = m_pendingTransactions[transactionId];
                        RemoveTransaction(expiredTransaction.TransactionId);
                    }
                }
            }
            catch (Exception excp)
            {
                logger.Error("Exception RemoveExpiredTransaction. " + excp.Message);
            }
        }

        public void PrintPendingTransactions()
        {
            logger.Debug("=== Pending Transactions ===");

            foreach (SIPTransaction transaction in m_pendingTransactions.Values)
            {
                logger.Debug(" Pending tansaction " + transaction.TransactionRequest.Method + " " + transaction.TransactionState + " " + DateTime.Now.Subtract(transaction.Created).TotalSeconds.ToString("0.##") + "s " + transaction.TransactionRequestURI.ToString() + " (" + transaction.TransactionId + ").");
            }
        }

        /// <summary>
        /// Should not normally be used as transactions will time out after the retransmit window has expired. This method is 
        /// used in such cases where the original request is being dropped and no action is required on a re-transmit.
        /// </summary>
        /// <param name="transactionId"></param>
        public void RemoveTransaction(string transactionId)
        {
            m_pendingTransactions.TryRemove(transactionId, out _);
        }

        private void RemoveTransaction(SIPTransaction transaction)
        {
            // Remove all event handlers.
            transaction.RemoveEventHandlers();

            RemoveTransaction(transaction.TransactionId);

            transaction = null;
        }

        public void RemoveAll()
        {

            m_pendingTransactions.Clear();

        }

        /// <summary>
        ///  Checks whether there is only a single transaction outstanding for a Call-ID header. This is used in an experimental trial of matching
        ///  ACK's on the Call-ID if the full check fails.
        /// </summary>
        /// <param name="callId">The SIP Header Call-ID to check for.</param>
        /// <returns>True if there is only a single pending transaction with the specified  Call-ID, false otherwise.</returns>
        private bool IsCallIdUniqueForPending(string callId)
        {
            bool match = false;

            lock (m_pendingTransactions)
            {
                foreach (var (_, transaction) in m_pendingTransactions)
                {
                    if ((transaction.TransactionType == SIPTransactionTypesEnum.InviteClient || transaction.TransactionType == SIPTransactionTypesEnum.InviteServer) &&
                        transaction.TransactionFinalResponse != null &&
                        transaction.TransactionState == SIPTransactionStatesEnum.Completed &&
                        transaction.TransactionRequest.Header.CallId == callId)
                    {
                        if (!match)
                        {
                            match = true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                }
            }

            return match;
        }

        #region Unit testing.

#if UNITTEST
       
        [TestFixture]
        public class SIPTransactionEngineUnitTest
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
            {
                // Add a Console appender so logger messages will show up in the NUnit Console.Out tab.
                GB28181.Logger4Net.Appender.ConsoleAppender appender = new GB28181.Logger4Net.Appender.ConsoleAppender();
                GB28181.Logger4Net.Layout.ILayout fallbackLayout = new GB28181.Logger4Net.Layout.PatternLayout("%m%n");
                appender.Layout = fallbackLayout;
                GB28181.Logger4Net.Config.BasicConfigurator.Configure(appender);
            }

            [TestFixtureTearDown]
            public void Dispose()
            { }

            [Test]
            [ExpectedException(typeof(ApplicationException))]
            public void DuplicateTransactionUnitTest()
            {
                SIPTransactionEngine clientEngine = new SIPTransactionEngine();  
                
                SIPURI dummyURI = SIPURI.ParseSIPURI("sip:dummy@mysipswitch.com");
                SIPRequest inviteRequest = GetDummyINVITERequest(dummyURI);

                SIPEndPoint dummySIPEndPoint = new SIPEndPoint(new IPEndPoint(IPAddress.Loopback, 1234));
                UACInviteTransaction clientTransaction = new UACInviteTransaction(new SIPTransport(MockSIPDNSManager.Resolve, null), inviteRequest, dummySIPEndPoint, dummySIPEndPoint, null);
                clientEngine.AddTransaction(clientTransaction);
                clientEngine.AddTransaction(clientTransaction);
            }

            /// <summary>
            /// Tests that the transaction ID is correctly generated and matched for a request and response pair.
            /// </summary>
            [Test]
            public void MatchOnRequestAndResponseTest() {

                SIPTransactionEngine transactionEngine = new SIPTransactionEngine();
                SIPEndPoint dummySIPEndPoint = new SIPEndPoint(new IPEndPoint(IPAddress.Loopback, 1234));

                SIPRequest inviteRequest = SIPRequest.ParseSIPRequest("INVITE sip:dummy@udp:127.0.0.1:12014 SIP/2.0" + m_CRLF +
                    "Via: SIP/2.0/UDP 127.0.0.1:1234;branch=z9hG4bK5f37455955ca433a902f8fea0ce2dc27" + m_CRLF +
                    "To: <sip:dummy@udp:127.0.0.1:12014>" + m_CRLF +
                    "From: <sip:unittest@mysipswitch.com>;tag=2062917371" + m_CRLF +
                    "Call-ID: 8ae45c15425040179a4285d774ccbaf6" + m_CRLF +
                    "CSeq: 1 INVITE" + m_CRLF +
                    "Contact: <sip:127.0.0.1:1234>" + m_CRLF +
                    "Max-Forwards: 70" + m_CRLF +
                    "User-Agent: unittest" + m_CRLF +
                    "Content-Length: 5" + m_CRLF +
                    "Content-Type: application/sdp" + m_CRLF +
                    m_CRLF +
                    "dummy");

                SIPTransaction transaction =  new UACInviteTransaction(new SIPTransport(MockSIPDNSManager.Resolve, null), inviteRequest, dummySIPEndPoint, dummySIPEndPoint, null);
                transactionEngine.AddTransaction(transaction);

                SIPResponse sipResponse = SIPResponse.ParseSIPResponse("SIP/2.0 603 Nothing listening" + m_CRLF +
                    "Via: SIP/2.0/UDP 127.0.0.1:1234;branch=z9hG4bK5f37455955ca433a902f8fea0ce2dc27;rport=12013" + m_CRLF +
                    "To: <sip:dummy@udp:127.0.0.1:12014>" + m_CRLF +
                    "From: <sip:unittest@mysipswitch.com>;tag=2062917371" + m_CRLF +
                    "Call-ID: 8ae45c15425040179a4285d774ccbaf6" + m_CRLF +
                    "CSeq: 1 INVITE" + m_CRLF +
                    "Content-Length: 0" + m_CRLF +
                    m_CRLF);

                Assert.IsNotNull(transactionEngine.GetTransaction(sipResponse), "Transaction should have matched, check the hashing mechanism.");
           }

            /// <summary>
            /// Tests the production and recognition of an ACK request for this transaction engine.
            /// The test uses two different transaction engine instances with one acting as the client and one as the server.
            /// </summary>
            [Test]
            public void AckRecognitionUnitTest()
            {
                SIPTransport clientTransport = null;
                SIPTransport serverTransport = null;

                try
                {
                    SIPTransactionEngine clientEngine = new SIPTransactionEngine();     // Client side of the INVITE.
                    SIPEndPoint clientEndPoint = new SIPEndPoint(SIPProtocolsEnum.udp, new IPEndPoint(IPAddress.Loopback, 12013));
                    clientTransport = new SIPTransport(MockSIPDNSManager.Resolve, clientEngine, new SIPUDPChannel(clientEndPoint.GetIPEndPoint()), false);
                    SetTransportTraceEvents(clientTransport);

                    SIPTransactionEngine serverEngine = new SIPTransactionEngine();     // Server side of the INVITE.
                    UASInviteTransaction serverTransaction = null;
                    SIPEndPoint serverEndPoint = new SIPEndPoint(new IPEndPoint(IPAddress.Loopback, 12014));
                    serverTransport = new SIPTransport(MockSIPDNSManager.Resolve, serverEngine, new SIPUDPChannel(serverEndPoint.GetIPEndPoint()), false);
                    SetTransportTraceEvents(serverTransport);
                    serverTransport.SIPTransportRequestReceived += (localEndPoint, remoteEndPoint, sipRequest) =>
                    {
                        Console.WriteLine("Server Transport Request In: " + sipRequest.Method + ".");
                        serverTransaction = serverTransport.CreateUASTransaction(sipRequest, remoteEndPoint, localEndPoint, null);
                        SetTransactionTraceEvents(serverTransaction);
                        serverTransaction.GotRequest(localEndPoint, remoteEndPoint, sipRequest);
                    };

                    SIPURI dummyURI = SIPURI.ParseSIPURI("sip:dummy@" + serverEndPoint);
                    SIPRequest inviteRequest = GetDummyINVITERequest(dummyURI);
                    inviteRequest.LocalSIPEndPoint = clientTransport.GetDefaultTransportContact(SIPProtocolsEnum.udp);

                    // Send the invite to the server side.
                    UACInviteTransaction clientTransaction = new UACInviteTransaction(clientTransport, inviteRequest, serverEndPoint, clientEndPoint, null);
                    SetTransactionTraceEvents(clientTransaction);
                    clientEngine.AddTransaction(clientTransaction);
                    clientTransaction.SendInviteRequest(serverEndPoint, inviteRequest);

                    Thread.Sleep(500);

                    Assert.IsTrue(clientTransaction.TransactionState == SIPTransactionStatesEnum.Completed, "Client transaction in incorrect state.");
                    Assert.IsTrue(serverTransaction.TransactionState == SIPTransactionStatesEnum.Confirmed, "Server transaction in incorrect state.");
                }
                finally
                {
                    if (clientTransport != null)
                    {
                        clientTransport.Shutdown();
                    }

                    if (serverTransport != null)
                    {
                        serverTransport.Shutdown();
                    }
                }
            }

            [Test]
            public void AckRecognitionIIUnitTest()
            {
                SIPTransactionEngine engine = new SIPTransactionEngine();     // Client side of the INVITE.
                
                string inviteRequestStr = 
                    "INVITE sip:303@sip.blueface.ie SIP/2.0" + m_CRLF +
					"Via: SIP/2.0/UDP 192.168.1.2:5065;rport;branch=z9hG4bKFBB7EAC06934405182D13950BD51F001" + m_CRLF +
					"From: SER Test X <sip:aaronxten@sip.blueface.ie:5065>;tag=196468136" + m_CRLF +
					"To: <sip:303@sip.blueface.ie>" + m_CRLF +
					"Contact: <sip:aaronxten@192.168.1.2:5065>" + m_CRLF +
					"Call-ID: A3DF9A04-0EFE-47E4-98B1-E18AA186F3D6@192.168.1.2" + m_CRLF +
					"CSeq: 49429 INVITE" + m_CRLF +
					"Max-Forwards: 70" + m_CRLF +
					"Content-Type: application/sdp" + m_CRLF +
					"User-Agent: Dummy" + m_CRLF +
					m_CRLF;

                SIPRequest inviteRequest = SIPRequest.ParseSIPRequest(inviteRequestStr);

                // Server has received the invite.
                SIPEndPoint dummySIPEndPoint = new SIPEndPoint(new IPEndPoint(IPAddress.Loopback, 1234));
                UASInviteTransaction serverTransaction = new UASInviteTransaction(new SIPTransport(MockSIPDNSManager.Resolve, null), inviteRequest, dummySIPEndPoint, dummySIPEndPoint, null);
                engine.AddTransaction(serverTransaction);

                //SIPResponse errorResponse = SIPTransport.GetResponse(inviteRequest.Header, SIPResponseStatusCodesEnum.Decline, "Unit Test", null, null);

                string ackRequestStr = 
                    "ACK sip:303@sip.blueface.ie SIP/2.0" + m_CRLF +
					"Via: SIP/2.0/UDP 192.168.1.2:5065;rport;branch=z9hG4bKFBB7EAC06934405182D13950BD51F001" + m_CRLF +
					"From: SER Test X <sip:aaronxten@sip.blueface.ie:5065>;tag=196468136" + m_CRLF +
					"To: <sip:303@sip.blueface.ie>" + m_CRLF +
					"Contact: <sip:aaronxten@192.168.1.2:5065>" + m_CRLF +
					"Call-ID: A3DF9A04-0EFE-47E4-98B1-E18AA186F3D6@192.168.1.2" + m_CRLF +
					"CSeq: 49429 ACK" + m_CRLF +
					"Max-Forwards: 70" + m_CRLF +
					"User-Agent: Dummy" + m_CRLF +
					m_CRLF;

                SIPRequest ackRequest = SIPRequest.ParseSIPRequest(ackRequestStr);

                SIPTransaction matchingTransaction = engine.GetTransaction(ackRequest);

                Assert.IsTrue(matchingTransaction.TransactionId == serverTransaction.TransactionId, "ACK transaction did not match INVITE transaction.");
            }

            private SIPRequest GetDummyINVITERequest(SIPURI dummyURI)
            {
                string dummyFrom = "<sip:unittest@mysipswitch.com>";
                string dummyContact = "sip:127.0.0.1:1234";
                SIPRequest inviteRequest = new SIPRequest(SIPMethodsEnum.INVITE, dummyURI);

                SIPHeader inviteHeader = new SIPHeader(SIPFromHeader.ParseFromHeader(dummyFrom), new SIPToHeader(null, dummyURI, null), 1, CallProperties.CreateNewCallId());
                inviteHeader.From.FromTag = CallProperties.CreateNewTag();
                inviteHeader.Contact = SIPContactHeader.ParseContactHeader(dummyContact);
                inviteHeader.CSeqMethod = SIPMethodsEnum.INVITE;
                inviteHeader.UserAgent = "unittest";
                inviteRequest.Header = inviteHeader;

                SIPViaHeader viaHeader = new SIPViaHeader("127.0.0.1", 1234, CallProperties.CreateBranchId(), SIPProtocolsEnum.udp);
                inviteRequest.Header.Vias.PushViaHeader(viaHeader);

                inviteRequest.Body = "dummy";
                inviteRequest.Header.ContentLength = inviteRequest.Body.Length;
                inviteRequest.Header.ContentType = "application/sdp";

                return inviteRequest;
            }

        #region Logging

            void SetTransportTraceEvents(SIPTransport transport)
            {
                transport.SIPBadRequestInTraceEvent += transport_SIPBadRequestInTraceEvent;
                transport.SIPBadResponseInTraceEvent += transport_SIPBadResponseInTraceEvent;
                transport.SIPRequestInTraceEvent += transport_SIPRequestInTraceEvent;
                transport.SIPRequestOutTraceEvent += transport_SIPRequestOutTraceEvent;
                transport.SIPResponseInTraceEvent += transport_SIPResponseInTraceEvent;
                transport.SIPResponseOutTraceEvent += transport_SIPResponseOutTraceEvent;
                transport.STUNRequestReceived += transport_STUNRequestReceived;
            }

            void SetTransactionTraceEvents(SIPTransaction transaction)
            {
                transaction.TransactionRemoved += new SIPTransactionRemovedDelegate(transaction_TransactionRemoved);
                transaction.TransactionStateChanged += new SIPTransactionStateChangeDelegate(transaction_TransactionStateChanged);
                transaction.TransactionTraceMessage += new SIPTransactionTraceMessageDelegate(transaction_TransactionTraceMessage);
            }

            void transaction_TransactionTraceMessage(SIPTransaction sipTransaction, string message)
            {
                //Console.WriteLine(sipTransaction.GetType() + " Trace (" + sipTransaction.TransactionId + "): " + message);
            }

            void transaction_TransactionStateChanged(SIPTransaction sipTransaction)
            {
                Console.WriteLine(sipTransaction.GetType() + " State Change (" + sipTransaction.TransactionId + "): " + sipTransaction.TransactionState);
            }

            void transaction_TransactionRemoved(SIPTransaction sipTransaction)
            {
                Console.WriteLine(sipTransaction.GetType() + " Removed (" + sipTransaction.TransactionId + ")");
            }

            void transport_UnrecognisedMessageReceived(SIPEndPoint localEndPoint, SIPEndPoint fromEndPoint, byte[] buffer)
            {
                Console.WriteLine("Unrecognised: " + localEndPoint + "<-" + fromEndPoint.ToString() + " " + buffer.Length + " bytes.");
            }

            void transport_STUNRequestReceived(IPEndPoint receivedEndPoint, IPEndPoint remoteEndPoint, byte[] buffer, int bufferLength)
            {
                Console.WriteLine("STUN: " + receivedEndPoint + "<-" + remoteEndPoint.ToString() + " " + buffer.Length + " bufferLength.");
            }

            void transport_SIPResponseOutTraceEvent(SIPEndPoint localEndPoint, SIPEndPoint toEndPoint, SIPResponse sipResponse)
            {
                Console.WriteLine("Response Out: " + localEndPoint + "->" + toEndPoint.ToString() + "\n" + sipResponse.ToString());
            }

            void transport_SIPResponseInTraceEvent(SIPEndPoint localEndPoint, SIPEndPoint fromEndPoint, SIPResponse sipResponse)
            {
                Console.WriteLine("Response In: " + localEndPoint + "<-" + fromEndPoint.ToString() + "\n" + sipResponse.ToString());
            }

            void transport_SIPRequestOutTraceEvent(SIPEndPoint localEndPoint, SIPEndPoint toEndPoint, SIPRequest sipRequest)
            {
                Console.WriteLine("Request Out: " + localEndPoint + "->" + toEndPoint.ToString() + "\n" + sipRequest.ToString());
            }

            void transport_SIPRequestInTraceEvent(SIPEndPoint localEndPoint, SIPEndPoint fromEndPoint, SIPRequest sipRequest)
            {
                Console.WriteLine("Request In: " + localEndPoint + "<-" + fromEndPoint.ToString() + "\n" + sipRequest.ToString());
            }

            void transport_SIPBadResponseInTraceEvent(SIPEndPoint localEndPoint, SIPEndPoint fromEndPoint, string message, SIPValidationFieldsEnum errorField, string rawMessage)
            {
                Console.WriteLine("Bad Response: " + localEndPoint + "<-" + fromEndPoint.ToString() + " " + errorField + ". " + message + "\n" + rawMessage);
            }

            void transport_SIPBadRequestInTraceEvent(SIPEndPoint localEndPoint, SIPEndPoint fromEndPoint, string message, SIPValidationFieldsEnum errorField, string rawMessage)
            {
                Console.WriteLine("Bad Request: " + localEndPoint + "<-" + fromEndPoint.ToString() + " " + errorField + "." + message + "\n" + rawMessage);
            }

        #endregion
        }

#endif

        #endregion
    }
}
