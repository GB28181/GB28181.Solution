//-----------------------------------------------------------------------------
// Filename: SIPMessage.cs
//
// Desciption: Functionality to determine whether a SIP message is a request or
// a response and break a message up into its constituent parts.
//
// History:
// 04 May 2006	Aaron Clauson	Created.
// 30 May 2020	Edward Chen     Updated.
// 06 Sep 2020  Edward Chen     Refactoring
// License: 
// BSD 3-Clause "New" or "Revised" License, see included LICENSE.md file.
//

using System;
using GB28181.Logger4Net;
using SIPSorcery.SIP;


namespace GB28181
{
    /// <bnf>
    /// generic-message  =  start-line
    ///                     *message-header
    ///                     CRLF
    ///                     [ message-body ]
    /// start-line       =  Request-Line / Status-Line
    /// </bnf>
    public class SIPMessage:SIPMessageBuffer
	{		
		private const string SIP_RESPONSE_PREFIX = "SIP";
		private const string SIP_MESSAGE_IDENTIFIER = "SIP";	// String that must be in a message buffer to be recognised as a SIP message and processed.

		private static int m_sipFullVersionStrLen = SIPConstants.SIP_FULLVERSION_STRING.Length;
		private static int m_minFirstLineLength = 7;
		private static string m_CRLF = SIPConstants.CRLF;

        private static ILog logger = AssemblyState.logger;


        public new static SIPMessage ParseSIPMessage(string message, SIPEndPoint localSIPEndPoint, SIPEndPoint remoteSIPEndPoint)
		{
			try
			{
				SIPMessage sipMessage = new SIPMessage();
                sipMessage.LocalSIPEndPoint = localSIPEndPoint;
                sipMessage.RemoteSIPEndPoint = remoteSIPEndPoint;

				sipMessage.RawMessage = message;
				int endFistLinePosn = message.IndexOf(m_CRLF);

                if (endFistLinePosn != -1)
                {
                    sipMessage.FirstLine = message.Substring(0, endFistLinePosn);

                    if (sipMessage.FirstLine.Substring(0, 3) == SIP_RESPONSE_PREFIX)
                    {
                        sipMessage.SIPMessageType = SIPMessageTypesEnum.Response;
                    }
                    else
                    {
                        sipMessage.SIPMessageType = SIPMessageTypesEnum.Request;
                    }

                    int endHeaderPosn = message.IndexOf(m_CRLF + m_CRLF);
                    if (endHeaderPosn == -1)
                    {
                        // Assume flakey implementation if message does not contain the required CRLFCRLF sequence and treat the message as having no body.
                        string headerString = message.Substring(endFistLinePosn + 2, message.Length - endFistLinePosn - 2);
                        sipMessage.SIPHeaders = SIPHeader.SplitHeaders(headerString); //Regex.Split(headerString, m_CRLF);
                    }
                    else
                    {
                        string headerString = message.Substring(endFistLinePosn + 2, endHeaderPosn - endFistLinePosn - 2);
                        sipMessage.SIPHeaders = SIPHeader.SplitHeaders(headerString); //Regex.Split(headerString, m_CRLF);

                        if (message.Length > endHeaderPosn + 4)
                        {
                            sipMessage.Body = message.Substring(endHeaderPosn + 4);
                        }
                    }

                    return sipMessage;
                }
                else
                {
                    logger.Warn("Error ParseSIPMessage, there were no end of line characters in the string being parsed.");
                    return null;
                }
			}
			catch(Exception excp)
			{
				logger.Error("Exception ParseSIPMessage. " + excp.Message + "\nSIP Message=" + message + ".");
				return null;
			}
		}
					
	
	}
}
