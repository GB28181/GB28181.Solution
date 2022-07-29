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
// 28 Jul 2022  Edward Chen     Simple it.
// License: 
// BSD 3-Clause "New" or "Revised" License, see included LICENSE.md file.
//

using System.Text;
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
    public class SIPMessage : SIPMessageBuffer
    {

        public SIPMessage(Encoding sipEncoding, Encoding sipBodyEncoding) : base(sipEncoding, sipBodyEncoding) { }


        public static new SIPMessageBuffer ParseSIPMessage(
            string message,
            SIPEndPoint localSIPEndPoint,
            SIPEndPoint remoteSIPEndPoint)
        {
            return ParseSIPMessage(SIPSorcery.SIP.SIPConstants.DEFAULT_ENCODING.GetBytes(message), localSIPEndPoint, remoteSIPEndPoint);
        }
    }
}
