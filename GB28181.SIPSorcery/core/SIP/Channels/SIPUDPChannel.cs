//-----------------------------------------------------------------------------
// Filename: SIPUDPChannel.cs
//
// Description: SIP transport for UDP.
// 
// History:
// 17 Oct 2005	Aaron Clauson	Created.
// 30 May 2020	Edward Chen     Updated.
// License: 
// BSD 3-Clause "New" or "Revised" License, see included LICENSE.md file.
//

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using SIPSorcery.SIP;
using SIPSorcery.Sys;

#if UNITTEST
using NUnit.Framework;
#endif

namespace GB28181
{
    public class SIPUDPChannel : SIPChannel
    {
        private const string THREAD_NAME = "sipchanneludp-";

        //private ILog logger = AssemblyState.logger;

        // Channel sockets.
        private Guid m_socketId = Guid.NewGuid();
        private UdpClient m_sipConn = null;
        //private bool m_closed = false;

        public SIPUDPChannel(IPEndPoint endPoint)
        {
            m_localSIPEndPoint = new SIPEndPoint(SIPProtocolsEnum.udp, endPoint);
            Initialise();
        }

        private void Initialise()
        {
            try
            {
                //IPEndPoint ipedp = new IPEndPoint(IPAddress.Parse("0.0.0.0"), EnvironmentVariables.GbServiceLocalPort);
                IPEndPoint ipedp = new IPEndPoint(IPAddress.Parse("0.0.0.0"), m_localSIPEndPoint.GetIPEndPoint().Port);
                //logger.Debug("SIPUDPChannel.Initialise: IPEndPoint=" + ipedp.ToString());
                //m_sipConn = new UdpClient(m_localSIPEndPoint.GetIPEndPoint());
                m_sipConn = new UdpClient(ipedp);
                //m_sipConn.Client.ReceiveTimeout = 3000;

                var listenThread = new Thread(new ThreadStart(Listen))
                {
                    Name = THREAD_NAME + Crypto.GetRandomString(4)
                };
                listenThread.Start();

                //logger.Debug("SIPUDPChannel listener created " + m_localSIPEndPoint.GetIPEndPoint() + ".");
            }
            catch (Exception excp)
            {
                logger.Error("Exception SIPUDPChannel Initialise. " + excp.Message);
                throw excp;
            }
        }

        private void Dispose(bool disposing)
        {
            try
            {
                this.Close();
            }
            catch (Exception excp)
            {
                logger.Error("Exception Disposing SIPUDPChannel. " + excp.Message);
            }
        }

        private void Listen()
        {
            try
            {
                byte[] buffer = null;

                logger.Debug("SIPUDPChannel socket on 0.0.0.0:" + m_localSIPEndPoint.GetIPEndPoint().Port + " listening started.");

                while (!Closed)
                {
                    IPEndPoint inEndPoint = new IPEndPoint(IPAddress.Any, 0);
                    //logger.Debug("SIPUDPChannel socket start.Receive.");
                    try
                    {
                        buffer = m_sipConn.Receive(ref inEndPoint);
                    }
                    catch (SocketException sockex)
                    {
                        // ToDo. Pretty sure these exceptions get thrown when an ICMP message comes back indicating there is no listening
                        // socket on the other end. It would be nice to be able to relate that back to the socket that the data was sent to
                        // so that we know to stop sending.
                        //logger.Warn("SocketException SIPUDPChannel Receive (" + sockExcp.ErrorCode + "). " + sockExcp.Message);

                        //inEndPoint = new SIPEndPoint(new IPEndPoint(IPAddress.Any, 0));

                        logger.Error("SocketException listening on SIPUDPChannel. " + sockex.Message);
                        continue;
                    }
                    catch (Exception listenExcp)
                    {
                        // There is no point logging this as without processing the ICMP message it's not possible to know which socket the rejection came from.
                        logger.Error("Exception listening on SIPUDPChannel. " + listenExcp.Message);

                        inEndPoint = new IPEndPoint(IPAddress.Any, 0);
                        continue;
                    }
                    //logger.Debug("SIPUDPChannel socket end.Receive.");
                    if (buffer == null || buffer.Length == 0)
                    {
                        // No need to care about zero byte packets.
                        //string remoteEndPoint = (inEndPoint != null) ? inEndPoint.ToString() : "could not determine";
                        //logger.Error("Zero bytes received on SIPUDPChannel " + m_localSIPEndPoint.ToString() + ".");
                    }
                    else
                    {
                        SIPMessageReceived?.Invoke(this, new SIPEndPoint(SIPProtocolsEnum.udp, inEndPoint), buffer);
                    }
                    //logger.Debug("SIPUDPChannel socket message handled.");
                }

                logger.Debug("SIPUDPChannel socket on " + m_localSIPEndPoint + " listening halted.");
            }
            catch (Exception excp)
            {
                logger.Error("Exception SIPUDPChannel Listen. " + excp.Message);
                //throw excp;
            }
        }

        public override void Send(IPEndPoint destinationEndPoint, string message)
        {
            byte[] messageBuffer = Encoding.UTF8.GetBytes(message);
            Send(destinationEndPoint, messageBuffer);
        }

        public override void Send(IPEndPoint destinationEndPoint, byte[] buffer)
        {
            try
            {
                if (destinationEndPoint == null)
                {
                    throw new ApplicationException("An empty destination was specified to Send in SIPUDPChannel.");
                }
                else
                {
                    string str = Encoding.UTF8.GetString(buffer);
                    m_sipConn.Send(buffer, buffer.Length, destinationEndPoint);
                }
            }
            catch (Exception excp)
            {
                logger.Error("Exception (" + excp.GetType().ToString() + ") SIPUDPChannel Send (sendto=>" + IPSocket.GetSocketString(destinationEndPoint) + "). " + excp.Message);
                throw excp;
            }
        }



        public override void Send(IPEndPoint dstEndPoint, byte[] buffer, string serverCertificateName)
        {
            throw new ApplicationException("This Send method is not available in the SIP UDP channel, please use an alternative overload.");
        }

        public override bool IsConnectionEstablished(IPEndPoint remoteEndPoint)
        {
            throw new NotSupportedException("The SIP UDP channel does not support connections.");
        }

        protected override Dictionary<string, SIPConnection> GetConnectionsList()
        {
            throw new NotSupportedException("The SIP UDP channel does not support connections.");
        }

        public override void Close()
        {
            try
            {
                logger.Debug("Closing SIP UDP Channel " + SIPChannelEndPoint + ".");

                Closed = true;
                m_sipConn.Close();
            }
            catch (Exception excp)
            {
                logger.Warn("Exception SIPUDPChannel Close. " + excp.Message);
            }
        }
    }
}
