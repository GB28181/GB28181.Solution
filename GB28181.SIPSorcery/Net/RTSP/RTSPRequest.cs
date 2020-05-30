//-----------------------------------------------------------------------------
// Filename: RTSPRequest.cs
//
// Description: RTSP request.
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
using GB28181.Logger4Net;

namespace GB28181.Net
{
    public enum RTSPRequestParserError
    {
        None = 0,
    }
    
    /// <summary>
    /// RFC2326 6.1:
    /// Request-Line = Method SP Request-URI SP RTSP-Version CRLF
    /// </summary>
    public class RTSPRequest
    {
        private static ILog logger = AssemblyStreamState.logger;
		
		private static string m_CRLF = RTSPConstants.CRLF;
		private static string m_rtspFullVersion = RTSPConstants.RTSP_FULLVERSION_STRING;
		private static string m_rtspVersion = RTSPConstants.RTSP_VERSION_STRING;
		private static int m_rtspMajorVersion = RTSPConstants.RTSP_MAJOR_VERSION;
		private static int m_rtspMinorVersion = RTSPConstants.RTSP_MINOR_VERSION;

		public bool Valid { get; set; } = true;
		public RTSPHeaderError ValidationError { get; set; } = RTSPHeaderError.None;

		public string RTSPVersion { get; set; } = m_rtspVersion;
		public int RTSPMajorVersion { get; set; } = m_rtspMajorVersion;
		public int RTSPMinorVersion { get; set; } = m_rtspMinorVersion;
		public RTSPMethodsEnum Method { get; set; }
		public string UnknownMethod { get; set; }

		public RTSPURL URL { get; set; }
		public RTSPHeader Header { get; set; }
		public string Body { get; set; }

		public DateTime ReceivedAt { get; set; } = DateTime.MinValue;
		public IPEndPoint ReceivedFrom { get; set; } 

		private RTSPRequest()
		{}
			
		public RTSPRequest(RTSPMethodsEnum method, string url)
		{
			RTSPHeaderParserError urlParserError = RTSPHeaderParserError.None;
			
			try
			{
				Method = method;
				
				URL = RTSPURL.ParseRTSPURL(url, out urlParserError);
                if (urlParserError != RTSPHeaderParserError.None)
                {
                    throw new ApplicationException("Error parsing RTSP URL, " + urlParserError.ToString() + ".");
                }

				RTSPVersion = m_rtspFullVersion;
			}
			catch(Exception excp)
			{
				logger.Error("Exception RTSPRequest Ctor. " + excp.Message + ".");
			}
		}

        public RTSPRequest(RTSPMethodsEnum method, RTSPURL url)
        {
             Method = method;
             URL = url;
             RTSPVersion = m_rtspFullVersion;
        }

        public static RTSPRequest ParseRTSPRequest(RTSPMessage rtspMessage)
        {
            RTSPRequestParserError dontCare = RTSPRequestParserError.None;
            return ParseRTSPRequest(rtspMessage, out dontCare);
        }

		public static RTSPRequest ParseRTSPRequest(RTSPMessage rtspMessage, out RTSPRequestParserError requestParserError)
		{
            requestParserError = RTSPRequestParserError.None;
            string urlStr = null;
			
			try
			{
				var rtspRequest = new RTSPRequest();

                string statusLine = rtspMessage.FirstLine;

                int firstSpacePosn = statusLine.IndexOf(" ",StringComparison.OrdinalIgnoreCase);

                string method = statusLine.Substring(0, firstSpacePosn).Trim();
                rtspRequest.Method = RTSPMethods.GetMethod(method);
                if (rtspRequest.Method == RTSPMethodsEnum.UNKNOWN)
                {
                    rtspRequest.UnknownMethod = method;
                    logger.Warn("Unknown RTSP method received " + rtspRequest.Method + ".");
                }

                statusLine = statusLine.Substring(firstSpacePosn).Trim();
                int secondSpacePosn = statusLine.IndexOf(" ");

                if (secondSpacePosn != -1)
                {
                    urlStr = statusLine.Substring(0, secondSpacePosn);

                    rtspRequest.URL = RTSPURL.ParseRTSPURL(urlStr);
                    rtspRequest.RTSPVersion = statusLine.Substring(secondSpacePosn, statusLine.Length - secondSpacePosn).Trim();
                    rtspRequest.Header = (rtspMessage.RTSPHeaders != null) ? RTSPHeader.ParseRTSPHeaders(rtspMessage.RTSPHeaders) : new RTSPHeader(0, null);
                    rtspRequest.Body = rtspMessage.Body;

                    return rtspRequest;
                }
                else
                {
                    throw new ApplicationException("URI was missing on RTSP request.");
                }
    		}
			catch(Exception excp)
			{
				logger.Error("Exception parsing RTSP request. URI, " + urlStr + ".");
				throw new ApplicationException("There was an exception parsing an RTSP request. " + excp.Message);
			}
		}

		public new string ToString()
		{
			try
			{
				string methodStr = (Method != RTSPMethodsEnum.UNKNOWN) ? Method.ToString() : UnknownMethod;

                string message = methodStr + " " + URL.ToString() + " " + RTSPVersion + m_CRLF;
                message += (Header != null) ? Header.ToString() : null;

				if(Body != null)
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
				logger.Error("Exception RTSPRequest ToString. " + excp.Message);
				throw;
			}
		}
    }
}
