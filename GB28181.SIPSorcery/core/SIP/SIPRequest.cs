//-----------------------------------------------------------------------------
// Filename: SIPRequest.cs
//
// Description: SIP Request.
//
// History:
// 04 May 2006	Aaron Clauson	Created, Dublin, Ireland.
// 06 Sep 2020  Edward Chen     Updated
//
// License: 
// BSD 3-Clause "New" or "Revised" License, see included LICENSE.md file.
//-----------------------------------------------------------------------------



using System;
using GB28181.Logger4Net;
using SIPSorcery.SIP;



namespace GB28181
{
    /// <bnf>
	///  Method SP Request-URI SP SIP-Version CRLF
	///  *message-header
	///	 CRLF
	///	 [ message-body ]
	///	 
	///	 Methods: REGISTER, INVITE, ACK, CANCEL, BYE, OPTIONS
	///	 SIP-Version: SIP/2.0
	///	 
	///	 SIP-Version    =  "SIP" "/" 1*DIGIT "." 1*DIGIT
	/// </bnf>
	public class SIPRequest : SIPSorcery.SIP.SIPRequest
    {
        private static new readonly ILog logger = AssemblyState.logger;

        private delegate bool IsLocalSIPSocketDelegate(string socket, SIPProtocolsEnum protocol);

        public SIPHeader Header { get; set; }
        public SIPRoute ReceivedRoute { get; set; }

        //		public DateTime Created = DateTime.Now;
        public SIPEndPoint RemoteSIPEndPoint;               // The remote IP socket the request was received from or sent to.
        public SIPEndPoint LocalSIPEndPoint;                // The local SIP socket the request was received on or sent from.



        public SIPRequest(SIPMethodsEnum method, string uri) : base(method, uri)
        {
            Method = method;
            URI = SIPURI.ParseSIPURI(uri);
        }

        public SIPRequest(SIPMethodsEnum method, SIPURI uri) : base(method, uri)
        {
            Method = method;
            URI = uri;
        }

        public static SIPRequest ParseSIPRequest(SIPMessage sipMessage)
        {
            string uriStr = null;

            try
            {
                var sipRequest = ParseSIPRequest(sipMessage);
                string statusLine = sipMessage.FirstLine;
                int firstSpacePosn = statusLine.IndexOf(" ", StringComparison.CurrentCultureIgnoreCase);

                string method = statusLine.Substring(0, firstSpacePosn).Trim();
                sipRequest.Method = SIPMethods.GetMethod(method);
                if (sipRequest.Method == SIPMethodsEnum.UNKNOWN)
                {
                    sipRequest.UnknownMethod = method;
                    logger.Warn("Unknown SIP method received " + sipRequest.UnknownMethod + ".");
                }

                statusLine = statusLine.Substring(firstSpacePosn).Trim();
                int secondSpacePosn = statusLine.IndexOf(" ", StringComparison.CurrentCultureIgnoreCase);

                if (secondSpacePosn != -1)
                {
                    uriStr = statusLine.Substring(0, secondSpacePosn);

                    sipRequest.URI = SIPURI.ParseSIPURI(uriStr);
                    sipRequest.SIPVersion = statusLine.Substring(secondSpacePosn, statusLine.Length - secondSpacePosn).Trim();
                    sipRequest.Header = SIPHeader.ParseSIPHeaders(sipMessage.SIPHeaders);
                    sipRequest.Body = System.Text.Encoding.UTF8.GetString(sipMessage.Body);

                    return sipRequest;
                }
                else
                {
                    throw new SIPValidationException(SIPValidationFieldsEnum.Request, "URI was missing on Request.");
                }
            }
            catch (SIPValidationException)
            {
                throw;
            }
            catch (Exception excp)
            {
                logger.Error("Exception parsing SIP Request. " + excp.Message);
                logger.Error(sipMessage.RawMessage);
                throw new SIPValidationException(SIPValidationFieldsEnum.Request, "Unknown error parsing SIP Request");
            }
        }

        public static SIPRequest ParseSIPRequest(string sipMessageStr)
        {
            try
            {
                SIPMessage sipMessage = (SIPMessage)SIPMessage.ParseSIPMessage(sipMessageStr, null, null);
                return ParseSIPRequest(sipMessage);
            }
            catch (SIPValidationException)
            {
                throw;
            }
            catch (Exception excp)
            {
                logger.Error("Exception ParseSIPRequest. " + excp.Message);
                logger.Error(sipMessageStr);
                throw new SIPValidationException(SIPValidationFieldsEnum.Request, "Unknown error parsing SIP Request");
            }
        }

        public new string ToString()
        {
            try
            {
                string methodStr = (Method != SIPMethodsEnum.UNKNOWN) ? Method.ToString() : UnknownMethod;

                string message = methodStr + " " + URI.ToString() + " " + SIPVersion + m_CRLF + this.Header.ToString();

                if (Body != null)
                {
                    message += m_CRLF + Body;
                }
                else
                {
                    message += m_CRLF;
                }

                return message;
            }
            catch (Exception excp)
            {
                logger.Error("Exception SIPRequest ToString. " + excp.Message);
                //throw excp;
                return "";
            }
        }

        /// <summary>
        /// Creates an identical copy of the SIP Request for the caller.
        /// </summary>
        /// <returns>New copy of the SIPRequest.</returns>
        public SIPRequest Copy()
        {
            return ParseSIPRequest(this.ToString());
        }

        public string CreateBranchId()
        {
            string routeStr = (Header.Routes != null) ? Header.Routes.ToString() : null;
            string toTagStr = (Header.To != null) ? Header.To.ToTag : null;
            string fromTagStr = (Header.From != null) ? Header.From.FromTag : null;
            string topViaStr = (Header.Vias != null && Header.Vias.TopViaHeader != null) ? Header.Vias.TopViaHeader.ToString() : null;

            return CallProperties.CreateBranchId(
                SIPConstants.SIP_BRANCH_MAGICCOOKIE,
                toTagStr,
                fromTagStr,
                Header.CallId,
                URI.ToString(),
                topViaStr,
                Header.CSeq,
                routeStr,
                Header.ProxyRequire,
                null);
        }

        /// <summary>
        /// Determines if this SIP header is a looped header. The basis for the decision is the branchid in the Via header. If the branchid for a new
        /// header computes to the same branchid as a Via header already in the SIP header then it is considered a loop.
        /// </summary>
        /// <returns>True if this header is a loop otherwise false.</returns>
        public bool IsLoop(string ipAddress, int port, string currentBranchId)
        {
            foreach (SIPViaHeader viaHeader in Header.Vias.Via)
            {
                if (viaHeader.Host == ipAddress && viaHeader.Port == port)
                {
                    if (viaHeader.Branch == currentBranchId)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public bool IsValid(out SIPValidationFieldsEnum errorField, out string errorMessage)
        {
            errorField = SIPValidationFieldsEnum.Unknown;
            errorMessage = null;

            if (Header.Vias.Length == 0)
            {
                errorField = SIPValidationFieldsEnum.ViaHeader;
                errorMessage = "No Via headers";
                return false;
            }

            return true;
        }

        //~SIPRequest()
        //{
        //    Destroyed++;
        //}
    }
}
