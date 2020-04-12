using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace GB28181.Logger4Net
{
    public class LogSocket
    {
        private LogSocket() { }
        private Socket mSendLogSocket = null;

        private static LogSocket _instance = new LogSocket();
        //单例模式访问
        public static LogSocket Instance => _instance;
        private void NewSocket()
        {
            mSendLogSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            mSendLogSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout, 100);
            mSendLogSocket.Blocking = false;
        }

        public void SendPackage(string remoteIp, int remotePort, string msg)
        {
            try
            {
                if (string.IsNullOrEmpty(remoteIp))
                    return;
                if (remotePort == 0)
                    return;

                byte[] buffer = Encoding.Default.GetBytes(msg);
                int length = buffer.Length;
                IPEndPoint remoteEp = new IPEndPoint(IPAddress.Parse(remoteIp), remotePort);
                SendPacket(buffer, 0, length, remoteEp);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        private void SendPacket(byte[] packet, int offset, int count, IPEndPoint remoteEP)
        {
            try
            {
                if (mSendLogSocket == null)
                    NewSocket();
                mSendLogSocket.SendTo(packet, 0, count, SocketFlags.None, remoteEP);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
