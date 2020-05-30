//-----------------------------------------------------------------------------
// Filename: RTSPResponse.cs
//
// Description: RTSP response.
//
// History:
// 09 Nov 2007	Aaron Clauson	Created.
//
// License: 
// BSD 3-Clause "New" or "Revised" License, see included LICENSE.md file.
//

using System;
using System.Net;
using System.Collections.Generic;
using System.Text;
using GB28181.Sys;
using GB28181.Logger4Net;
using SIPSorcery.Sys;

namespace GB28181.Net
{
    public enum RTSPResponseParserError
    {
        None = 0,
    }
    
    /// <summary>
    /// RFC2326 7.1:
    /// Status-Line =   RTSP-Version SP Status-Code SP Reason-Phrase CRLF

    /// </summary>
    public class RTSPResponse
    {
        private static ILog logger = AssemblyStreamState.logger;
		
		private static string m_CRLF = RTSPConstants.CRLF;
		//private static string m_rtspFullVersion = RTSPConstants.RTSP_FULLVERSION_STRING;
		private static string m_rtspVersion = RTSPConstants.RTSP_VERSION_STRING;
		private static int m_rtspMajorVersion = RTSPConstants.RTSP_MAJOR_VERSION;
		private static int m_rtspMinorVersion = RTSPConstants.RTSP_MINOR_VERSION;

		public bool Valid = true;
		public RTSPHeaderError ValidationError = RTSPHeaderError.None;

		public string RTSPVersion = m_rtspVersion;
		public int RTSPMajorVersion = m_rtspMajorVersion;
		public int RTSPMinorVersion = m_rtspMinorVersion;
        public RTSPResponseStatusCodesEnum Status;
        public int StatusCode;
        public string ReasonPhrase;
        public string Body;
		public RTSPHeader Header;

		public DateTime ReceivedAt = DateTime.MinValue;
		public IPEndPoint ReceivedFrom;

        private RTSPResponse()
        { }

        public RTSPResponse(RTSPResponseStatusCodesEnum responseType, string reasonPhrase)
		{
            StatusCode = (int)responseType;
            Status = responseType;
            ReasonPhrase = reasonPhrase;
            ReasonPhrase = responseType.ToString();
		}

        public static RTSPResponse ParseRTSPResponse(RTSPMessage rtspMessage)
        {
            RTSPResponseParserError dontCare = RTSPResponseParserError.None;
            return ParseRTSPResponse(rtspMessage, out dontCare);
        }

		public static RTSPResponse ParseRTSPResponse(RTSPMessage rtspMessage, out RTSPResponseParserError responseParserError)
		{
            responseParserError = RTSPResponseParserError.None;
			
			try
			{
                RTSPResponse rtspResponse = new RTSPResponse();

                string statusLine = rtspMessage.FirstLine;

                int firstSpacePosn = statusLine.IndexOf(" ");

                rtspResponse.RTSPVersion = statusLine.Substring(0, firstSpacePosn).Trim();
                statusLine = statusLine.Substring(firstSpacePosn).Trim();
                rtspResponse.StatusCode = Convert.ToInt32(statusLine.Substring(0, 3));
                rtspResponse.Status = RTSPResponseStatusCodes.GetStatusTypeForCode(rtspResponse.StatusCode);
                rtspResponse.ReasonPhrase = statusLine.Substring(3).Trim();

                rtspResponse.Header = RTSPHeader.ParseRTSPHeaders(rtspMessage.RTSPHeaders);
                rtspResponse.Body = rtspMessage.Body;

                //rtspResponse.Valid = rtspResponse.Validate(out sipResponse.ValidationError);

                return rtspResponse;
    		}
			catch(Exception excp)
			{
				logger.Error("Exception parsing RTSP reqsponse. "  + excp.Message);
				throw new ApplicationException("There was an exception parsing an RTSP response. " + excp.Message);
			}
		}

		public new string ToString()
		{
			try
			{
                string reasonPhrase = (!ReasonPhrase.IsNullOrBlank()) ? " " + ReasonPhrase : null;

                string message =
                    RTSPVersion + "/" + RTSPMajorVersion + "." + RTSPMinorVersion + " " + StatusCode + reasonPhrase + m_CRLF +
                    this.Header.ToString();

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
			catch(Exception excp)
			{
				logger.Error("Exception RTSPResponse ToString. " + excp.Message);
				throw excp;
			}
		}
    }
}
