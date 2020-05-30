//-----------------------------------------------------------------------------
// Filename: NetTestDescriptor.cs
//
// Description: SIP request body that describes a network test request. The network
// test is typically used to measure the realtime characteristics of a network path 
// 
// History:
// 09 Jan 2007	Aaron Clauson	Created.
//
// License: 
// BSD 3-Clause "New" or "Revised" License, see included LICENSE.md file.
//


using System;
using System.Net;
using System.Text.RegularExpressions;
using GB28181.Logger4Net;
using GB28181.Sys;
using SIPSorcery.Sys;

namespace GB28181.Net
{
    public class NetTestDescriptor
    {   
        public const int MAXIMUM_PAYLOAD_SIZE = 1460;			// Ethernet MTU = 1500. (12B RTP header, 8B UDP Header, 20B IP Header, TOTAL = 40B).
        public const int MAXIMUM_RATEPER_CHANNEL = 500000;      // This is in bits. Needs to be less than 1460B * 8bits * 66.667 Packets/s, so 500Kbps is a nice number.
        public const int RTP_HEADER_OVERHEAD = 12;              // 12B RTP header.
        public const int DEFAULT_RTP_PAYLOADSIZE = 160;         // g711 @ 20ms.

        private string m_CRLF = AppState.CRLF;

        protected static ILog logger = AppState.logger;

        public int NumberChannels = 1;
        public int FrameSize = 20;        // In milliseconds, determines how often packets are transmitted, e.g. framezie=20ms results in 50 packets per second.
        public int PayloadSize = 172;     // The size of each packet in bytes.
        public IPEndPoint RemoteSocket;   // Socket the data stream will be sent to.

        public NetTestDescriptor()
        { }

        public NetTestDescriptor(int numberChannels, int frameSize, int payloadSize, IPEndPoint remoteEndPoint)
        {
            NumberChannels = numberChannels;
            FrameSize = frameSize;
            PayloadSize = payloadSize;
            RemoteSocket = remoteEndPoint;
        }

        public static NetTestDescriptor ParseNetTestDescriptor(string description)
        {
            try
            {
                if (description == null || description.Trim().Length == 0)
                {
                    logger.Error("Cannot parse NetTestDescriptor from an empty string.");
                    return null;
                }
                else
                {
                    int numberChannels = Convert.ToInt32(Regex.Match(description, @"channels=(?<channels>\d+)", RegexOptions.IgnoreCase | RegexOptions.Singleline).Result("${channels}"));
                    int frameSize = Convert.ToInt32(Regex.Match(description, @"frame=(?<frame>\d+)", RegexOptions.IgnoreCase | RegexOptions.Singleline).Result("${frame}"));
                    int payloadSize = Convert.ToInt32(Regex.Match(description, @"payload=(?<payload>\d+)", RegexOptions.IgnoreCase | RegexOptions.Singleline).Result("${payload}"));
                    string socketStr = Regex.Match(description, @"socket=(?<socket>.+?)(\s|$)", RegexOptions.IgnoreCase | RegexOptions.Singleline).Result("${socket}");

                    NetTestDescriptor descriptor = new NetTestDescriptor(numberChannels, frameSize, payloadSize, IPSocket.ParseSocketString(socketStr));
                    return descriptor;
                }
            }
            catch (Exception excp)
            {
                logger.Error("Exception ParseNetTestDescriptor. " + excp.Message);
                throw excp;
            }
        }

        public new string ToString()
        {
            string description =
                "channels=" + NumberChannels + m_CRLF +
                "frame=" + FrameSize + m_CRLF +
                "payload=" + PayloadSize + m_CRLF +
                "socket=" + RemoteSocket.ToString() + m_CRLF;

            return description;
        }
    }
}
