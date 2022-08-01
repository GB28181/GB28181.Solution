//-----------------------------------------------------------------------------
// Filename: SIPHeader.cs
//
// Description: SIP Header.
// 
// History:
// 17 Sep 2005	Aaron Clauson	Created.
// 30 May 2020	Edward Chen     Updated.
// 01 Aug 2022	Edward Chen     Updated.
// License: 
// BSD 3-Clause "New" or "Revised" License, see included LICENSE.md file.
//

using System;
using System.Collections.Generic;
using System.Text;
using GB28181.Logger4Net;
using SIPSorcery.SIP;
using SIPSorcery.Sys;


namespace GB28181
{
    /// <bnf>
    /// header  =  "header-name" HCOLON header-value *(COMMA header-value)
    /// field-name: field-value CRLF
    /// </bnf>
    public class SIPHeader : SIPSorcery.SIP.SIPHeader
    {
        public struct ContentTypes
        {
            public const string Application_SDP = "application/sdp";
            public const string Application_MANSRTSP = "Application/MANSRTSP";

        }


        private static ILog logger = AssemblyState.logger;
        private static string m_CRLF = SIPConstants.CRLF;

        // RFC SIP headers.
        public SIPAuthenticationHeader AuthenticationHeader;
        public List<SIPContactHeader> Contact = new List<SIPContactHeader>();
        public SIPFromHeader From;
        public SIPRouteSet RecordRoutes = new SIPRouteSet();
        public SIPRouteSet Routes = new SIPRouteSet();
        public SIPToHeader To;
        public SIPViaSet Vias = new SIPViaSet();

        // Non-core custom SIP headers for use with the SIP Sorcery switchboard.
        public string SwitchboardOriginalCallID { get; set; }    // The original Call-ID header on the call that was forwarded to the switchboard.
        //public string SwitchboardOriginalFrom;      // The original From header on the call that was forwarded to the switchboard.
        //public string SwitchboardOriginalTo;        // The original To header on the call that was forwarded to the switchboard.
        public string SwitchboardLineName { get; set; }          // An optional name for the line the call was received on.
        public string SwitchboardOwner { get; set; }            // If a switchboard operator "grabs" a call then they will take exclusive ownership of it. This field records the owner.
        public string SwitchboardTerminate { get; set; }        // Can be set on a BYE request to indicate whether the switchboard is requesting both, current or opposite dialogues to be hungup.
        //public string SwitchboardFromContactURL;
        //public int SwitchboardTokenRequest;         // A user agent can request a token from a sipsorcery server and this value indicates the period the token is being requested for.
        //public string SwitchboardToken;             // If a token is issued this header will be used to hold it in the response.
        public string CRMPersonName { get; set; }
        public string CRMCompanyName { get; set; }
        public string CRMPictureURL { get; set; }

        public SIPHeader()
        { }

        public SIPHeader(string fromHeader, string toHeader, int cseq, string callId)
        {
            SIPFromHeader from = SIPFromHeader.ParseFromHeader(fromHeader);
            SIPToHeader to = SIPToHeader.ParseToHeader(toHeader);
            Initialise(null, from, to, cseq, callId);
        }

        public SIPHeader(string fromHeader, string toHeader, string contactHeader, int cseq, string callId)
        {
            SIPFromHeader from = SIPFromHeader.ParseFromHeader(fromHeader);
            SIPToHeader to = SIPToHeader.ParseToHeader(toHeader);
            List<SIPContactHeader> contact = SIPContactHeader.ParseContactHeader(contactHeader);
            Initialise(contact, from, to, cseq, callId);
        }

        public SIPHeader(SIPFromHeader from, SIPToHeader to, int cseq, string callId)
        {
            Initialise(null, from, to, cseq, callId);
        }

        public SIPHeader(SIPContactHeader contact, SIPFromHeader from, SIPToHeader to, int cseq, string callId)
        {
            List<SIPContactHeader> contactList = new List<SIPContactHeader>();
            if (contact != null)
            {
                contactList.Add(contact);
            }

            Initialise(contactList, from, to, cseq, callId);
        }

        public SIPHeader(List<SIPContactHeader> contactList, SIPFromHeader from, SIPToHeader to, int cseq, string callId)
        {
            Initialise(contactList, from, to, cseq, callId);
        }

        private void Initialise(List<SIPContactHeader> contact, SIPFromHeader from, SIPToHeader to, int cseq, string callId)
        {
            if (callId == null || callId.Trim().Length == 0)
            {
                throw new ApplicationException("The CallId header cannot be empty when creating a new SIP header.");
            }

            From = from ?? throw new ApplicationException("The From header cannot be empty when creating a new SIP header.");
            To = to ?? throw new ApplicationException("The To header cannot be empty when creating a new SIP header.");
            Contact = contact;
            CallId = callId;

            if (cseq > 0 && cseq < int.MaxValue)
            {
                CSeq = cseq;
            }
            else
            {
                CSeq = DEFAULT_CSEQ;
            }
        }



        public static SIPHeader ParseSIPHeaders(string[] headersCollection)
        {
            try
            {
                SIPHeader sipHeader = new SIPHeader
                {
                    MaxForwards = -1        // This allows detection of whether this header is present or not.
                };
                string lastHeader = null;

                for (int lineIndex = 0; lineIndex < headersCollection.Length; lineIndex++)
                {
                    string headerLine = headersCollection[lineIndex];

                    if (headerLine.IsNullOrBlank())
                    {
                        // No point processing blank headers.
                        continue;
                    }

                    string headerName = null;
                    string headerValue = null;

                    // If the first character of a line is whitespace it's a contiuation of the previous line.
                    if (headerLine.StartsWith(" ", StringComparison.CurrentCulture))
                    {
                        headerName = lastHeader;
                        headerValue = headerLine.Trim();
                    }
                    else
                    {
                        headerLine = headerLine.Trim();
                        int delimiterIndex = headerLine.IndexOf(SIPConstants.HEADER_DELIMITER_CHAR);

                        if (delimiterIndex == -1)
                        {
                            logger.Warn("Invalid SIP header, ignoring, " + headerLine + ".");
                            continue;
                        }

                        headerName = headerLine.Substring(0, delimiterIndex).Trim();
                        headerValue = headerLine.Substring(delimiterIndex + 1).Trim();
                    }

                    try
                    {
                        string headerNameLower = headerName.ToLower(System.Globalization.CultureInfo.CurrentCulture);

                        #region Via
                        if (headerNameLower == SIPHeaders.SIP_COMPACTHEADER_VIA ||
                            headerNameLower == SIPHeaders.SIP_HEADER_VIA.ToLower(System.Globalization.CultureInfo.CurrentCulture))
                        {
                            //sipHeader.RawVia += headerValue;

                            SIPViaHeader[] viaHeaders = SIPViaHeader.ParseSIPViaHeader(headerValue);

                            if (viaHeaders != null && viaHeaders.Length > 0)
                            {
                                foreach (SIPViaHeader viaHeader in viaHeaders)
                                {
                                    sipHeader.Vias.AddBottomViaHeader(viaHeader);
                                }
                            }
                        }
                        #endregion
                        #region CallId
                        else if (headerNameLower == SIPHeaders.SIP_COMPACTHEADER_CALLID ||
                                headerNameLower == SIPHeaders.SIP_HEADER_CALLID.ToLower(System.Globalization.CultureInfo.CurrentCulture))
                        {
                            sipHeader.CallId = headerValue;
                        }
                        #endregion
                        #region CSeq
                        else if (headerNameLower == SIPHeaders.SIP_HEADER_CSEQ.ToLower(System.Globalization.CultureInfo.CurrentCulture))
                        {
                            //sipHeader.RawCSeq += headerValue;

                            string[] cseqFields = headerValue.Split(' ');
                            if (cseqFields == null || cseqFields.Length == 0)
                            {
                                logger.Warn("The " + SIPHeaders.SIP_HEADER_CSEQ + " was empty.");
                            }
                            else
                            {
                                if (!Int32.TryParse(cseqFields[0], out sipHeader.CSeq))
                                {
                                    logger.Warn(SIPHeaders.SIP_HEADER_CSEQ + " did not contain a valid integer, " + headerLine + ".");
                                }

                                if (cseqFields != null && cseqFields.Length > 1)
                                {
                                    sipHeader.CSeqMethod = SIPMethods.GetMethod(cseqFields[1]);
                                }
                                else
                                {
                                    logger.Warn("There was no " + SIPHeaders.SIP_HEADER_CSEQ + " method, " + headerLine + ".");
                                }
                            }
                        }
                        #endregion
                        #region Expires
                        else if (headerNameLower == SIPHeaders.SIP_HEADER_EXPIRES.ToLower(System.Globalization.CultureInfo.CurrentCulture))
                        {
                            //sipHeader.RawExpires += headerValue;

                            if (!Int64.TryParse(headerValue, out sipHeader.Expires))
                            {
                                logger.Warn("The Expires value was not a valid integer, " + headerLine + ".");
                            }
                        }
                        #endregion
                        #region Min-Expires
                        else if (headerNameLower == SIPHeaders.SIP_HEADER_MINEXPIRES.ToLower(System.Globalization.CultureInfo.CurrentCulture))
                        {
                            if (!Int64.TryParse(headerValue, out sipHeader.MinExpires))
                            {
                                logger.Warn("The Min-Expires value was not a valid integer, " + headerLine + ".");
                            }
                        }
                        #endregion
                        #region Contact
                        else if (headerNameLower == SIPHeaders.SIP_COMPACTHEADER_CONTACT ||
                            headerNameLower == SIPHeaders.SIP_HEADER_CONTACT.ToLower(System.Globalization.CultureInfo.CurrentCulture))
                        {
                            List<SIPContactHeader> contacts = SIPContactHeader.ParseContactHeader(headerValue);
                            if (contacts != null && contacts.Count > 0)
                            {
                                sipHeader.Contact.AddRange(contacts);
                            }
                        }
                        #endregion
                        #region From
                        else if (headerNameLower == SIPHeaders.SIP_COMPACTHEADER_FROM ||
                             headerNameLower == SIPHeaders.SIP_HEADER_FROM.ToLower(System.Globalization.CultureInfo.CurrentCulture))
                        {
                            //sipHeader.RawFrom = headerValue;
                            sipHeader.From = SIPFromHeader.ParseFromHeader(headerValue);
                        }
                        #endregion
                        #region To
                        else if (headerNameLower == SIPHeaders.SIP_COMPACTHEADER_TO ||
                            headerNameLower == SIPHeaders.SIP_HEADER_TO.ToLower(System.Globalization.CultureInfo.CurrentCulture))
                        {
                            //sipHeader.RawTo = headerValue;
                            sipHeader.To = SIPToHeader.ParseToHeader(headerValue);
                        }
                        #endregion
                        #region WWWAuthenticate
                        else if (headerNameLower == SIPHeaders.SIP_HEADER_WWWAUTHENTICATE.ToLower(System.Globalization.CultureInfo.CurrentCulture))
                        {
                            //sipHeader.RawAuthentication = headerValue;
                            sipHeader.AuthenticationHeader = SIPAuthenticationHeader.ParseSIPAuthenticationHeader(SIPAuthorisationHeadersEnum.WWWAuthenticate, headerValue);
                        }
                        #endregion
                        #region Authorization
                        else if (headerNameLower == SIPHeaders.SIP_HEADER_AUTHORIZATION.ToLower(System.Globalization.CultureInfo.CurrentCulture))
                        {
                            //sipHeader.RawAuthentication = headerValue;
                            sipHeader.AuthenticationHeader = SIPAuthenticationHeader.ParseSIPAuthenticationHeader(SIPAuthorisationHeadersEnum.Authorize, headerValue);
                        }
                        #endregion
                        #region ProxyAuthentication
                        else if (headerNameLower == SIPHeaders.SIP_HEADER_PROXYAUTHENTICATION.ToLower(System.Globalization.CultureInfo.CurrentCulture))
                        {
                            //sipHeader.RawAuthentication = headerValue;
                            sipHeader.AuthenticationHeader = SIPAuthenticationHeader.ParseSIPAuthenticationHeader(SIPAuthorisationHeadersEnum.ProxyAuthenticate, headerValue);
                        }
                        #endregion
                        #region ProxyAuthorization
                        else if (headerNameLower == SIPHeaders.SIP_HEADER_PROXYAUTHORIZATION.ToLower(System.Globalization.CultureInfo.CurrentCulture))
                        {
                            sipHeader.AuthenticationHeader = SIPAuthenticationHeader.ParseSIPAuthenticationHeader(SIPAuthorisationHeadersEnum.ProxyAuthorization, headerValue);
                        }
                        #endregion
                        #region UserAgent
                        else if (headerNameLower == SIPHeaders.SIP_HEADER_USERAGENT.ToLower(System.Globalization.CultureInfo.CurrentCulture))
                        {
                            sipHeader.UserAgent = headerValue;
                        }
                        #endregion
                        #region MaxForwards
                        else if (headerNameLower == SIPHeaders.SIP_HEADER_MAXFORWARDS.ToLower(System.Globalization.CultureInfo.CurrentCulture))
                        {
                            if (!Int32.TryParse(headerValue, out sipHeader.MaxForwards))
                            {
                                logger.Warn("The " + SIPHeaders.SIP_HEADER_MAXFORWARDS + " could not be parsed as a valid integer, " + headerLine + ".");
                            }
                        }
                        #endregion
                        #region ContentLength
                        else if (headerNameLower == SIPHeaders.SIP_COMPACTHEADER_CONTENTLENGTH ||
                            headerNameLower == SIPHeaders.SIP_HEADER_CONTENTLENGTH.ToLower(System.Globalization.CultureInfo.CurrentCulture))
                        {
                            if (!Int32.TryParse(headerValue, out sipHeader.ContentLength))
                            {
                                logger.Warn("The " + SIPHeaders.SIP_HEADER_CONTENTLENGTH + " could not be parsed as a valid integer.");
                            }
                        }
                        #endregion
                        #region ContentType
                        else if (headerNameLower == SIPHeaders.SIP_COMPACTHEADER_CONTENTTYPE ||
                            headerNameLower == SIPHeaders.SIP_HEADER_CONTENTTYPE.ToLower(System.Globalization.CultureInfo.CurrentCulture))
                        {
                            sipHeader.ContentType = headerValue;
                        }
                        #endregion
                        #region Accept
                        else if (headerNameLower == SIPHeaders.SIP_HEADER_ACCEPT.ToLower(System.Globalization.CultureInfo.CurrentCulture))
                        {
                            sipHeader.Accept = headerValue;
                        }
                        #endregion
                        #region Route
                        else if (headerNameLower == SIPHeaders.SIP_HEADER_ROUTE.ToLower(System.Globalization.CultureInfo.CurrentCulture))
                        {
                            SIPRouteSet routeSet = SIPRouteSet.ParseSIPRouteSet(headerValue);
                            if (routeSet != null)
                            {
                                while (routeSet.Length > 0)
                                {
                                    sipHeader.Routes.AddBottomRoute(routeSet.PopRoute());
                                }
                            }
                        }
                        #endregion
                        #region RecordRoute
                        else if (headerNameLower == SIPHeaders.SIP_HEADER_RECORDROUTE.ToLower(System.Globalization.CultureInfo.CurrentCulture))
                        {
                            SIPRouteSet recordRouteSet = SIPRouteSet.ParseSIPRouteSet(headerValue);
                            if (recordRouteSet != null)
                            {
                                while (recordRouteSet.Length > 0)
                                {
                                    sipHeader.RecordRoutes.AddBottomRoute(recordRouteSet.PopRoute());
                                }
                            }
                        }
                        #endregion
                        #region Allow-Events
                        else if (headerNameLower == SIPHeaders.SIP_HEADER_ALLOW_EVENTS || headerNameLower == SIPHeaders.SIP_COMPACTHEADER_ALLOWEVENTS)
                        {
                            sipHeader.AllowEvents = headerValue;
                        }
                        #endregion
                        #region Event
                        else if (headerNameLower == SIPHeaders.SIP_HEADER_EVENT.ToLower(System.Globalization.CultureInfo.CurrentCulture) || headerNameLower == SIPHeaders.SIP_COMPACTHEADER_EVENT)
                        {
                            sipHeader.Event = headerValue;
                        }
                        #endregion
                        #region SubscriptionState.
                        else if (headerNameLower == SIPHeaders.SIP_HEADER_SUBSCRIPTION_STATE.ToLower(System.Globalization.CultureInfo.CurrentCulture))
                        {
                            sipHeader.SubscriptionState = headerValue;
                        }
                        #endregion
                        #region Timestamp.
                        else if (headerNameLower == SIPHeaders.SIP_HEADER_TIMESTAMP.ToLower(System.Globalization.CultureInfo.CurrentCulture))
                        {
                            sipHeader.Timestamp = headerValue;
                        }
                        #endregion
                        #region Date.
                        else if (headerNameLower == SIPHeaders.SIP_HEADER_DATE.ToLower(System.Globalization.CultureInfo.CurrentCulture))
                        {
                            sipHeader.Date = headerValue;
                        }
                        #endregion
                        #region Refer-Sub.
                        else if (headerNameLower == SIPHeaders.SIP_HEADER_REFERSUB.ToLower(System.Globalization.CultureInfo.CurrentCulture))
                        {
                            if (sipHeader.ReferSub == null)
                            {
                                sipHeader.ReferSub = headerValue;
                            }
                            else
                            {
                                throw new SIPValidationException(SIPValidationFieldsEnum.ReferToHeader, "Only a single Refer-Sub header is permitted.");
                            }
                        }
                        #endregion
                        #region Refer-To.
                        else if (headerNameLower == SIPHeaders.SIP_HEADER_REFERTO.ToLower(System.Globalization.CultureInfo.CurrentCulture) ||
                            headerNameLower == SIPHeaders.SIP_COMPACTHEADER_REFERTO)
                        {
                            if (sipHeader.ReferTo == null)
                            {
                                sipHeader.ReferTo = headerValue;
                            }
                            else
                            {
                                throw new SIPValidationException(SIPValidationFieldsEnum.ReferToHeader, "Only a single Refer-To header is permitted.");
                            }
                        }
                        #endregion
                        #region Referred-By.
                        else if (headerNameLower == SIPHeaders.SIP_HEADER_REFERREDBY.ToLower(System.Globalization.CultureInfo.CurrentCulture))
                        {
                            sipHeader.ReferredBy = headerValue;
                        }
                        #endregion
                        #region Require.
                        else if (headerNameLower == SIPHeaders.SIP_HEADER_REQUIRE.ToLower(System.Globalization.CultureInfo.CurrentCulture))
                        {
                            sipHeader.Require = headerValue;
                        }
                        #endregion
                        #region Reason.
                        else if (headerNameLower == SIPHeaders.SIP_HEADER_REASON.ToLower(System.Globalization.CultureInfo.CurrentCulture))
                        {
                            sipHeader.Reason = headerValue;
                        }
                        #endregion
                        #region Proxy-ReceivedFrom.
                        else if (headerNameLower == SIPHeaders.SIP_HEADER_PROXY_RECEIVEDFROM.ToLower(System.Globalization.CultureInfo.CurrentCulture))
                        {
                            sipHeader.ProxyReceivedFrom = headerValue;
                        }
                        #endregion
                        #region Proxy-ReceivedOn.
                        else if (headerNameLower == SIPHeaders.SIP_HEADER_PROXY_RECEIVEDON.ToLower(System.Globalization.CultureInfo.CurrentCulture))
                        {
                            sipHeader.ProxyReceivedOn = headerValue;
                        }
                        #endregion
                        #region Proxy-SendFrom.
                        else if (headerNameLower == SIPHeaders.SIP_HEADER_PROXY_SENDFROM.ToLower(System.Globalization.CultureInfo.CurrentCulture))
                        {
                            sipHeader.ProxySendFrom = headerValue;
                        }
                        #endregion
                        #region Supported
                        else if (headerNameLower == SIPHeaders.SIP_COMPACTHEADER_SUPPORTED ||
                            headerNameLower == SIPHeaders.SIP_HEADER_SUPPORTED.ToLower(System.Globalization.CultureInfo.CurrentCulture))
                        {
                            sipHeader.Supported = headerValue;
                        }
                        #endregion
                        #region Authentication-Info
                        else if (headerNameLower == SIPHeaders.SIP_HEADER_AUTHENTICATIONINFO.ToLower(System.Globalization.CultureInfo.CurrentCulture))
                        {
                            sipHeader.AuthenticationInfo = headerValue;
                        }
                        #endregion
                        #region Accept-Encoding
                        else if (headerNameLower == SIPHeaders.SIP_HEADER_ACCEPTENCODING.ToLower(System.Globalization.CultureInfo.CurrentCulture))
                        {
                            sipHeader.AcceptEncoding = headerValue;
                        }
                        #endregion
                        #region Accept-Language
                        else if (headerNameLower == SIPHeaders.SIP_HEADER_ACCEPTLANGUAGE.ToLower(System.Globalization.CultureInfo.CurrentCulture))
                        {
                            sipHeader.AcceptLanguage = headerValue;
                        }
                        #endregion
                        #region Alert-Info
                        else if (headerNameLower == SIPHeaders.SIP_HEADER_ALERTINFO.ToLower(System.Globalization.CultureInfo.CurrentCulture))
                        {
                            sipHeader.AlertInfo = headerValue;
                        }
                        #endregion
                        #region Allow
                        else if (headerNameLower == SIPHeaders.SIP_HEADER_ALLOW.ToLower(System.Globalization.CultureInfo.CurrentCulture))
                        {
                            sipHeader.Allow = headerValue;
                        }
                        #endregion
                        #region Call-Info
                        else if (headerNameLower == SIPHeaders.SIP_HEADER_CALLINFO.ToLower(System.Globalization.CultureInfo.CurrentCulture))
                        {
                            sipHeader.CallInfo = headerValue;
                        }
                        #endregion
                        #region Content-Disposition
                        else if (headerNameLower == SIPHeaders.SIP_HEADER_CONTENT_DISPOSITION.ToLower(System.Globalization.CultureInfo.CurrentCulture))
                        {
                            sipHeader.ContentDisposition = headerValue;
                        }
                        #endregion
                        #region Content-Encoding
                        else if (headerNameLower == SIPHeaders.SIP_HEADER_CONTENT_ENCODING.ToLower(System.Globalization.CultureInfo.CurrentCulture))
                        {
                            sipHeader.ContentEncoding = headerValue;
                        }
                        #endregion
                        #region Content-Language
                        else if (headerNameLower == SIPHeaders.SIP_HEADER_CONTENT_LANGUAGE.ToLower(System.Globalization.CultureInfo.CurrentCulture))
                        {
                            sipHeader.ContentLanguage = headerValue;
                        }
                        #endregion
                        #region Error-Info
                        else if (headerNameLower == SIPHeaders.SIP_HEADER_ERROR_INFO.ToLower(System.Globalization.CultureInfo.CurrentCulture))
                        {
                            sipHeader.ErrorInfo = headerValue;
                        }
                        #endregion
                        #region In-Reply-To
                        else if (headerNameLower == SIPHeaders.SIP_HEADER_IN_REPLY_TO.ToLower(System.Globalization.CultureInfo.CurrentCulture))
                        {
                            sipHeader.InReplyTo = headerValue;
                        }
                        #endregion
                        #region MIME-Version
                        else if (headerNameLower == SIPHeaders.SIP_HEADER_MIME_VERSION.ToLower(System.Globalization.CultureInfo.CurrentCulture))
                        {
                            sipHeader.MIMEVersion = headerValue;
                        }
                        #endregion
                        #region Organization
                        else if (headerNameLower == SIPHeaders.SIP_HEADER_ORGANIZATION.ToLower(System.Globalization.CultureInfo.CurrentCulture))
                        {
                            sipHeader.Organization = headerValue;
                        }
                        #endregion
                        #region Priority
                        else if (headerNameLower == SIPHeaders.SIP_HEADER_PRIORITY.ToLower(System.Globalization.CultureInfo.CurrentCulture))
                        {
                            sipHeader.Priority = headerValue;
                        }
                        #endregion
                        #region Proxy-Require
                        else if (headerNameLower == SIPHeaders.SIP_HEADER_PROXY_REQUIRE.ToLower(System.Globalization.CultureInfo.CurrentCulture))
                        {
                            sipHeader.ProxyRequire = headerValue;
                        }
                        #endregion
                        #region Reply-To
                        else if (headerNameLower == SIPHeaders.SIP_HEADER_REPLY_TO.ToLower(System.Globalization.CultureInfo.CurrentCulture))
                        {
                            sipHeader.ReplyTo = headerValue;
                        }
                        #endregion
                        #region Retry-After
                        else if (headerNameLower == SIPHeaders.SIP_HEADER_RETRY_AFTER.ToLower(System.Globalization.CultureInfo.CurrentCulture))
                        {
                            sipHeader.RetryAfter = headerValue;
                        }
                        #endregion
                        #region Subject
                        else if (headerNameLower == SIPHeaders.SIP_HEADER_SUBJECT.ToLower(System.Globalization.CultureInfo.CurrentCulture))
                        {
                            sipHeader.Subject = headerValue;
                        }
                        #endregion
                        #region Unsupported
                        else if (headerNameLower == SIPHeaders.SIP_HEADER_UNSUPPORTED.ToLower(System.Globalization.CultureInfo.CurrentCulture))
                        {
                            sipHeader.Unsupported = headerValue;
                        }
                        #endregion
                        #region Warning
                        else if (headerNameLower == SIPHeaders.SIP_HEADER_WARNING.ToLower(System.Globalization.CultureInfo.CurrentCulture))
                        {
                            sipHeader.Warning = headerValue;
                        }
                        #endregion
                        #region Switchboard-OriginalCallID.
                        //  else if (headerNameLower == SIPHeaders.SIP_HEADER_SWITCHBOARD_ORIGINAL_CALLID.ToLower(System.Globalization.CultureInfo.CurrentCulture))
                        //   {
                        //       sipHeader.SwitchboardOriginalCallID = headerValue;
                        //    }
                        #endregion
                        #region Switchboard-OriginalTo.
                        //else if (headerNameLower == SIPHeaders.SIP_HEADER_SWITCHBOARD_ORIGINAL_TO.ToLower(System.Globalization.CultureInfo.CurrentCulture))
                        //{
                        //    sipHeader.SwitchboardOriginalTo = headerValue;
                        //}
                        #endregion
                        #region Switchboard-CallerDescription.
                        //else if (headerNameLower == SIPHeaders.SIP_HEADER_SWITCHBOARD_CALLER_DESCRIPTION.ToLower(System.Globalization.CultureInfo.CurrentCulture))
                        //{
                        //    sipHeader.SwitchboardCallerDescription = headerValue;
                        //}
                        #endregion
                        #region Switchboard-LineName.
                        //else if (headerNameLower == SIPHeaders.SIP_HEADER_SWITCHBOARD_LINE_NAME.ToLower(System.Globalization.CultureInfo.CurrentCulture))
                        //{
                        //    sipHeader.SwitchboardLineName = headerValue;
                        //}
                         #endregion
                        #region Switchboard-OriginalFrom.
                        //else if (headerNameLower == SIPHeaders.SIP_HEADER_SWITCHBOARD_ORIGINAL_FROM.ToLower(System.Globalization.CultureInfo.CurrentCulture))
                        //{
                        //    sipHeader.SwitchboardOriginalFrom = headerValue;
                        //}
                        #endregion
                        //#region Switchboard-FromContactURL.
                        //else if (headerNameLower == SIPHeaders.SIP_HEADER_SWITCHBOARD_FROM_CONTACT_URL.ToLower(System.Globalization.CultureInfo.CurrentCulture))
                        //{
                        //    sipHeader.SwitchboardFromContactURL = headerValue;
                        //}
                        //#endregion
                        #region Switchboard-Owner.
                        //    else if (headerNameLower == SIPHeaders.SIP_HEADER_SWITCHBOARD_OWNER.ToLower(System.Globalization.CultureInfo.CurrentCulture))
                        //    {
                        //        sipHeader.SwitchboardOwner = headerValue;
                        //     }
                        #endregion
                        #region Switchboard-Terminate.
                        //else if (headerNameLower == SIPHeaders.SIP_HEADER_SWITCHBOARD_TERMINATE.ToLower(System.Globalization.CultureInfo.CurrentCulture))
                        //{
                        //    sipHeader.SwitchboardTerminate = headerValue;
                        //}
                        #endregion
                        //#region Switchboard-Token.
                        //else if (headerNameLower == SIPHeaders.SIP_HEADER_SWITCHBOARD_TOKEN.ToLower(System.Globalization.CultureInfo.CurrentCulture))
                        //{
                        //    sipHeader.SwitchboardToken = headerValue;
                        //}
                        //#endregion
                        //#region Switchboard-TokenRequest.
                        //else if (headerNameLower == SIPHeaders.SIP_HEADER_SWITCHBOARD_TOKENREQUEST.ToLower(System.Globalization.CultureInfo.CurrentCulture))
                        //{
                        //    Int32.TryParse(headerValue, out sipHeader.SwitchboardTokenRequest);
                        //}
                        //#endregion
                        #region CRM-PersonName.
                        //else if (headerNameLower == SIPHeaders.SIP_HEADER_CRM_PERSON_NAME.ToLower(System.Globalization.CultureInfo.CurrentCulture))
                        //{
                        //    sipHeader.CRMPersonName = headerValue;
                        //}
                        #endregion
                        #region CRM-CompanyName.
                        //else if (headerNameLower == SIPHeaders.SIP_HEADER_CRM_COMPANY_NAME.ToLower(System.Globalization.CultureInfo.CurrentCulture))
                        //{
                        //    sipHeader.CRMCompanyName = headerValue;
                        //}
                        //#endregion
                        //#region CRM-AvatarURL.
                        //else if (headerNameLower == SIPHeaders.SIP_HEADER_CRM_PICTURE_URL.ToLower(System.Globalization.CultureInfo.CurrentCulture))
                        //{
                        //    sipHeader.CRMPictureURL = headerValue;
                        //}
                        #endregion
                        #region ETag
                        else if (headerNameLower == SIPHeaders.SIP_HEADER_ETAG.ToLower(System.Globalization.CultureInfo.CurrentCulture))
                        {
                            sipHeader.ETag = headerValue;
                        }
                        #endregion
                        else
                        {
                            sipHeader.UnknownHeaders.Add(headerLine);
                        }

                        lastHeader = headerName;
                    }
                    catch (SIPValidationException)
                    {
                        throw;
                    }
                    catch (Exception parseExcp)
                    {
                        logger.Error("Error parsing SIP header " + headerLine + ". " + parseExcp.Message);
                        throw new SIPValidationException(SIPValidationFieldsEnum.Headers, "Unknown error parsing Header.");
                    }
                }

                sipHeader.Validate();

                return sipHeader;
            }
            catch (SIPValidationException)
            {
                throw;
            }
            catch (Exception excp)
            {
                logger.Error("Exception ParseSIPHeaders. " + excp.Message);
                throw new SIPValidationException(SIPValidationFieldsEnum.Headers, "Unknown error parsing Headers.");
            }
        }

        /// <summary>
        /// Puts the SIP headers together into a string ready for transmission.
        /// </summary>
        /// <returns>String representing the SIP headers.</returns>
        public new string ToString()
        {
            try
            {
                StringBuilder headersBuilder = new StringBuilder();

                headersBuilder.Append(Vias.ToString());

                string cseqField = null;
                if (this.CSeq != 0)
                {
                    cseqField = (this.CSeqMethod != SIPMethodsEnum.NONE) ? this.CSeq + " " + this.CSeqMethod.ToString() : CSeq.ToString();
                }

                headersBuilder.Append((To != null) ? SIPHeaders.SIP_HEADER_TO + ": " + this.To.ToString() + m_CRLF : null);
                headersBuilder.Append((From != null) ? SIPHeaders.SIP_HEADER_FROM + ": " + this.From.ToString() + m_CRLF : null);
                headersBuilder.Append((CallId != null) ? SIPHeaders.SIP_HEADER_CALLID + ": " + this.CallId + m_CRLF : null);
                headersBuilder.Append((CSeq > 0) ? SIPHeaders.SIP_HEADER_CSEQ + ": " + cseqField + m_CRLF : null);

                #region Appending Contact header.

                if (Contact != null && Contact.Count == 1)
                {
                    headersBuilder.Append(SIPHeaders.SIP_HEADER_CONTACT + ": " + Contact[0].ToString() + m_CRLF);
                }
                else if (Contact != null && Contact.Count > 1)
                {
                    StringBuilder contactsBuilder = new StringBuilder();
                    contactsBuilder.Append(SIPHeaders.SIP_HEADER_CONTACT + ": ");

                    bool firstContact = true;
                    foreach (SIPContactHeader contactHeader in Contact)
                    {
                        if (firstContact)
                        {
                            contactsBuilder.Append(contactHeader.ToString());
                        }
                        else
                        {
                            contactsBuilder.Append("," + contactHeader.ToString());
                        }

                        firstContact = false;
                    }

                    headersBuilder.Append(contactsBuilder.ToString() + m_CRLF);
                }

                #endregion

                headersBuilder.Append((MaxForwards >= 0) ? SIPHeaders.SIP_HEADER_MAXFORWARDS + ": " + this.MaxForwards + m_CRLF : null);
                headersBuilder.Append((Routes != null && Routes.Length > 0) ? SIPHeaders.SIP_HEADER_ROUTE + ": " + Routes.ToString() + m_CRLF : null);
                headersBuilder.Append((RecordRoutes != null && RecordRoutes.Length > 0) ? SIPHeaders.SIP_HEADER_RECORDROUTE + ": " + RecordRoutes.ToString() + m_CRLF : null);
                headersBuilder.Append((UserAgent != null && UserAgent.Trim().Length != 0) ? SIPHeaders.SIP_HEADER_USERAGENT + ": " + this.UserAgent + m_CRLF : null);
                headersBuilder.Append((Expires != -1) ? SIPHeaders.SIP_HEADER_EXPIRES + ": " + this.Expires + m_CRLF : null);
                headersBuilder.Append((MinExpires != -1) ? SIPHeaders.SIP_HEADER_MINEXPIRES + ": " + this.MinExpires + m_CRLF : null);
                headersBuilder.Append((Accept != null) ? SIPHeaders.SIP_HEADER_ACCEPT + ": " + this.Accept + m_CRLF : null);
                headersBuilder.Append((AcceptEncoding != null) ? SIPHeaders.SIP_HEADER_ACCEPTENCODING + ": " + this.AcceptEncoding + m_CRLF : null);
                headersBuilder.Append((AcceptLanguage != null) ? SIPHeaders.SIP_HEADER_ACCEPTLANGUAGE + ": " + this.AcceptLanguage + m_CRLF : null);
                headersBuilder.Append((Allow != null) ? SIPHeaders.SIP_HEADER_ALLOW + ": " + this.Allow + m_CRLF : null);
                headersBuilder.Append((AlertInfo != null) ? SIPHeaders.SIP_HEADER_ALERTINFO + ": " + this.AlertInfo + m_CRLF : null);
                headersBuilder.Append((AuthenticationInfo != null) ? SIPHeaders.SIP_HEADER_AUTHENTICATIONINFO + ": " + this.AuthenticationInfo + m_CRLF : null);
                headersBuilder.Append((AuthenticationHeader != null) ? AuthenticationHeader.ToString() + m_CRLF : null);
                headersBuilder.Append((CallInfo != null) ? SIPHeaders.SIP_HEADER_CALLINFO + ": " + this.CallInfo + m_CRLF : null);
                headersBuilder.Append((ContentDisposition != null) ? SIPHeaders.SIP_HEADER_CONTENT_DISPOSITION + ": " + this.ContentDisposition + m_CRLF : null);
                headersBuilder.Append((ContentEncoding != null) ? SIPHeaders.SIP_HEADER_CONTENT_ENCODING + ": " + this.ContentEncoding + m_CRLF : null);
                headersBuilder.Append((ContentLanguage != null) ? SIPHeaders.SIP_HEADER_CONTENT_LANGUAGE + ": " + this.ContentLanguage + m_CRLF : null);
                headersBuilder.Append((Date != null) ? SIPHeaders.SIP_HEADER_DATE + ": " + Date + m_CRLF : null);
                headersBuilder.Append((ErrorInfo != null) ? SIPHeaders.SIP_HEADER_ERROR_INFO + ": " + this.ErrorInfo + m_CRLF : null);
                headersBuilder.Append((InReplyTo != null) ? SIPHeaders.SIP_HEADER_IN_REPLY_TO + ": " + this.InReplyTo + m_CRLF : null);
                headersBuilder.Append((Organization != null) ? SIPHeaders.SIP_HEADER_ORGANIZATION + ": " + this.Organization + m_CRLF : null);
                headersBuilder.Append((Priority != null) ? SIPHeaders.SIP_HEADER_PRIORITY + ": " + Priority + m_CRLF : null);
                headersBuilder.Append((ProxyRequire != null) ? SIPHeaders.SIP_HEADER_PROXY_REQUIRE + ": " + this.ProxyRequire + m_CRLF : null);
                headersBuilder.Append((ReplyTo != null) ? SIPHeaders.SIP_HEADER_REPLY_TO + ": " + this.ReplyTo + m_CRLF : null);
                headersBuilder.Append((Require != null) ? SIPHeaders.SIP_HEADER_REQUIRE + ": " + Require + m_CRLF : null);
                headersBuilder.Append((RetryAfter != null) ? SIPHeaders.SIP_HEADER_RETRY_AFTER + ": " + this.RetryAfter + m_CRLF : null);
                headersBuilder.Append((Server != null && Server.Trim().Length != 0) ? SIPHeaders.SIP_HEADER_SERVER + ": " + this.Server + m_CRLF : null);
                headersBuilder.Append((Subject != null) ? SIPHeaders.SIP_HEADER_SUBJECT + ": " + Subject + m_CRLF : null);
                headersBuilder.Append((Supported != null) ? SIPHeaders.SIP_HEADER_SUPPORTED + ": " + Supported + m_CRLF : null);
                headersBuilder.Append((Timestamp != null) ? SIPHeaders.SIP_HEADER_TIMESTAMP + ": " + Timestamp + m_CRLF : null);
                headersBuilder.Append((Unsupported != null) ? SIPHeaders.SIP_HEADER_UNSUPPORTED + ": " + Unsupported + m_CRLF : null);
                headersBuilder.Append((Warning != null) ? SIPHeaders.SIP_HEADER_WARNING + ": " + Warning + m_CRLF : null);
                headersBuilder.Append((ETag != null) ? SIPHeaders.SIP_HEADER_ETAG + ": " + ETag + m_CRLF : null);
                headersBuilder.Append(SIPHeaders.SIP_HEADER_CONTENTLENGTH + ": " + this.ContentLength + m_CRLF);
                if (this.ContentType != null && this.ContentType.Trim().Length > 0)
                {
                    headersBuilder.Append(SIPHeaders.SIP_HEADER_CONTENTTYPE + ": " + this.ContentType + m_CRLF);
                }

                // Non-core SIP headers.
                headersBuilder.Append((AllowEvents != null) ? SIPHeaders.SIP_HEADER_ALLOW_EVENTS + ": " + AllowEvents + m_CRLF : null);
                headersBuilder.Append((Event != null) ? SIPHeaders.SIP_HEADER_EVENT + ": " + Event + m_CRLF : null);
                headersBuilder.Append((SubscriptionState != null) ? SIPHeaders.SIP_HEADER_SUBSCRIPTION_STATE + ": " + SubscriptionState + m_CRLF : null);
                headersBuilder.Append((ReferSub != null) ? SIPHeaders.SIP_HEADER_REFERSUB + ": " + ReferSub + m_CRLF : null);
                headersBuilder.Append((ReferTo != null) ? SIPHeaders.SIP_HEADER_REFERTO + ": " + ReferTo + m_CRLF : null);
                headersBuilder.Append((ReferredBy != null) ? SIPHeaders.SIP_HEADER_REFERREDBY + ": " + ReferredBy + m_CRLF : null);
                headersBuilder.Append((Reason != null) ? SIPHeaders.SIP_HEADER_REASON + ": " + Reason + m_CRLF : null);

                // Custom SIP headers.
                headersBuilder.Append((ProxyReceivedFrom != null) ? SIPHeaders.SIP_HEADER_PROXY_RECEIVEDFROM + ": " + ProxyReceivedFrom + m_CRLF : null);
                headersBuilder.Append((ProxyReceivedOn != null) ? SIPHeaders.SIP_HEADER_PROXY_RECEIVEDON + ": " + ProxyReceivedOn + m_CRLF : null);
                headersBuilder.Append((ProxySendFrom != null) ? SIPHeaders.SIP_HEADER_PROXY_SENDFROM + ": " + ProxySendFrom + m_CRLF : null);
                // headersBuilder.Append((SwitchboardOriginalCallID != null) ? SIPHeaders.SIP_HEADER_SWITCHBOARD_ORIGINAL_CALLID + ": " + SwitchboardOriginalCallID + m_CRLF : null);
                //headersBuilder.Append((SwitchboardOriginalTo != null) ? SIPHeaders.SIP_HEADER_SWITCHBOARD_ORIGINAL_TO + ": " + SwitchboardOriginalTo + m_CRLF : null);
                //headersBuilder.Append((SwitchboardCallerDescription != null) ? SIPHeaders.SIP_HEADER_SWITCHBOARD_CALLER_DESCRIPTION + ": " + SwitchboardCallerDescription + m_CRLF : null);
                //headersBuilder.Append((SwitchboardLineName != null) ? SIPHeaders.SIP_HEADER_SWITCHBOARD_LINE_NAME + ": " + SwitchboardLineName + m_CRLF : null);
                //headersBuilder.Append((SwitchboardOriginalFrom != null) ? SIPHeaders.SIP_HEADER_SWITCHBOARD_ORIGINAL_FROM + ": " + SwitchboardOriginalFrom + m_CRLF : null);
                //headersBuilder.Append((SwitchboardFromContactURL != null) ? SIPHeaders.SIP_HEADER_SWITCHBOARD_FROM_CONTACT_URL + ": " + SwitchboardFromContactURL + m_CRLF : null);
                // headersBuilder.Append((SwitchboardOwner != null) ? SIPHeaders.SIP_HEADER_SWITCHBOARD_OWNER + ": " + SwitchboardOwner + m_CRLF : null);
                //headersBuilder.Append((SwitchboardTerminate != null) ? SIPHeaders.SIP_HEADER_SWITCHBOARD_TERMINATE + ": " + SwitchboardTerminate + m_CRLF : null);
                //headersBuilder.Append((SwitchboardToken != null) ? SIPHeaders.SIP_HEADER_SWITCHBOARD_TOKEN + ": " + SwitchboardToken + m_CRLF : null);
                //headersBuilder.Append((SwitchboardTokenRequest > 0) ? SIPHeaders.SIP_HEADER_SWITCHBOARD_TOKENREQUEST + ": " + SwitchboardTokenRequest + m_CRLF : null);

                // CRM Headers.
                // headersBuilder.Append((CRMPersonName != null) ? SIPHeaders.SIP_HEADER_CRM_PERSON_NAME + ": " + CRMPersonName + m_CRLF : null);
                // headersBuilder.Append((CRMCompanyName != null) ? SIPHeaders.SIP_HEADER_CRM_COMPANY_NAME + ": " + CRMCompanyName + m_CRLF : null);
                //  headersBuilder.Append((CRMPictureURL != null) ? SIPHeaders.SIP_HEADER_CRM_PICTURE_URL + ": " + CRMPictureURL + m_CRLF : null);

                // Unknown SIP headers
                foreach (string unknownHeader in UnknownHeaders)
                {
                    headersBuilder.Append(unknownHeader + m_CRLF);
                }

                return headersBuilder.ToString();
            }
            catch (Exception excp)
            {
                logger.Error("Exception SIPHeader ToString. " + excp.Message);
                return "";
                //throw excp;
            }
        }

        private void Validate()
        {
            if (Vias == null || Vias.Length == 0)
            {
                throw new SIPValidationException(SIPValidationFieldsEnum.ViaHeader, "Invalid header, no Via.");
            }
        }

        public void SetDateHeader()
        {
            //Date = DateTime.UtcNow.ToString("ddd, dd MMM yyyy HH:mm:ss ") + "GMT";
            Date = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fff");
        }


    }
}
