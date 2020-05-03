using GB28181.Logger4Net;
using GB28181.Sys;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GB28181.Net.RTP
{
    /// <summary>
    /// TCP连接模式
    /// </summary>
    public enum TcpConnectMode
    {
        /// <summary>
        /// 主动连接
        /// </summary>
        active = 1,
        /// <summary>
        /// 被动连接
        /// </summary>
        passive = 2
    }
    public partial class TCPChannel : Channel
    {
        private Socket _rtpSocket;
        private TcpListener _tcpListener;
        private NetworkStream _stream { get; set; }

        public TCPChannel(TcpConnectMode mode, IPAddress ip, int[] port, ProtocolType protocolType, bool packetOutOrder, int receivePort)
            : base(mode, ip, port, protocolType, packetOutOrder, receivePort)
        {
            //IsRunning = true;
            //buffer = new byte[RTP_RECEIVE_BUFFER_SIZE];
            //_receivePort = receivePort;
            //_remoteEP = new IPEndPoint(remoteEP.Address, receivePort);
            //Initialize(port[0], port[1]);
        }

        public override void Initialize(int rtpPort, int rtcpPort)
        {
            if (TcpMode == TcpConnectMode.active)
            {
                IsRunning = true;
                _rtpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _rtpSocket.Bind(new IPEndPoint(IPAddress.Any, rtpPort));
                _rtpSocket.ReceiveBufferSize = RTP_RECEIVE_BUFFER_SIZE;
                _rtpSocket.BeginConnect(_remoteEP, new AsyncCallback(EndConnect), null);
            }
            else
            {
                _tcpListener = new TcpListener(IPAddress.Any, rtpPort);
                _tcpListener.AllowNatTraversal(true);
                IsRunning = true;
                _tcpListener.Start();
                _tcpListener.BeginAcceptTcpClient(new AsyncCallback(HandleTcpClientAccepted), _tcpListener);
            }
        }

        public override void Start()
        {
            ThreadPool.QueueUserWorkItem(delegate { ProcessRTPPackets(); });
        }

        #region 被动连接
        private void HandleTcpClientAccepted(IAsyncResult ar)
        {
            if (IsRunning)
            {
                TcpListener tcpListener = (TcpListener)ar.AsyncState;

                TcpClient tcpClient = tcpListener.EndAcceptTcpClient(ar);
                tcpClient.ReceiveBufferSize = RTP_RECEIVE_BUFFER_SIZE;
                byte[] buffer = new byte[tcpClient.ReceiveBufferSize];

                TcpClientState internalClient = new TcpClientState(tcpClient, buffer);
                _stream = internalClient.NetworkStream;
                _stream.BeginRead(
                  internalClient.Buffer,
                  0,
                  internalClient.Buffer.Length,
                  HandleTcpDatagramReceived,
                  internalClient);

                tcpListener.BeginAcceptTcpClient(new AsyncCallback(HandleTcpClientAccepted), ar.AsyncState);
            }
        }
        #endregion

        #region 主动链接
        private void EndConnect(IAsyncResult result)
        {
            try
            {
                _rtpSocket.EndConnect(result);
            }
            catch (Exception)
            {
                return;
            }
            if (IsRunning)
            {
                //连接成功。
                //创建Socket网络流
                _stream = new NetworkStream(_rtpSocket);

                //开始接收数据
                byte[] buffer = new byte[RTP_RECEIVE_BUFFER_SIZE];
                TcpClientState internalClient = new TcpClientState(_stream, buffer);
                _stream.BeginRead(
                  internalClient.Buffer,
                  0,
                  internalClient.Buffer.Length,
                  HandleTcpDatagramReceived,
                  internalClient);
            }
        }
        #endregion

        private void HandleTcpDatagramReceived(IAsyncResult ar)
        {
            if (IsRunning)
            {
                TcpClientState internalClient = (TcpClientState)ar.AsyncState;
                _stream = internalClient.NetworkStream;

                int numberOfReadBytes = 0;
                try
                {
                    numberOfReadBytes = _stream.EndRead(ar);
                }
                catch
                {
                    numberOfReadBytes = 0;
                }

                // received byte and trigger event notification
                byte[] buffer = new byte[numberOfReadBytes];
                Buffer.BlockCopy(internalClient.Buffer, 0, buffer, 0, numberOfReadBytes);

                try
                {
                    int len = 2;
                    int packetLen = GetPacketLen(buffer, 0, len);
                    int bufLen = buffer.Length;
                    int srcOffset = len;
                    while (bufLen > packetLen)
                    {
                        byte[] newBuffer = new byte[packetLen];
                        Buffer.BlockCopy(buffer, srcOffset, newBuffer, 0, packetLen);

                        TcpDatagramToEnqueue(newBuffer);

                        srcOffset += packetLen;
                        if (srcOffset + len > buffer.Length)
                        {
                            break;
                        }
                        bufLen -= packetLen + len;
                        packetLen = GetPacketLen(buffer, srcOffset, len);

                        srcOffset += len;
                    }
                    // continue listening for tcp datagram packets
                    _stream.BeginRead(
                      internalClient.Buffer,
                      0,
                      internalClient.Buffer.Length,
                      HandleTcpDatagramReceived,
                      internalClient);
                }
                catch (Exception)
                {

                }
            }
        }
        #region 处理数据
        private byte[] tempBuffer = Array.Empty<byte>();
        //private void HandleTcpDatagramReceived(IAsyncResult ar)
        //{
        //    if (IsRunning)
        //    {
        //        TcpClientState internalClient = (TcpClientState)ar.AsyncState;
        //        _stream = internalClient.NetworkStream;

        //        int numberOfReadBytes = 0;
        //        try
        //        {
        //            numberOfReadBytes = _stream.EndRead(ar);
        //        }
        //        catch
        //        {
        //            numberOfReadBytes = 0;
        //        }

        //        // received byte and trigger event notification
        //        //byte[] buffer = new byte[numberOfReadBytes];
        //        //Buffer.BlockCopy(internalClient.Buffer, 0, buffer, 0, numberOfReadBytes);
        //        byte[] buffer;
        //        if(tempBuffer.Length>0)
        //        {
        //            buffer = new byte[tempBuffer.Length + internalClient.Buffer.Length];
        //            Buffer.BlockCopy(tempBuffer, 0, buffer, 0, tempBuffer.Length);
        //            Buffer.BlockCopy(internalClient.Buffer, 0, buffer, tempBuffer.Length, internalClient.Buffer.Length);

        //            tempBuffer = new byte[0];

        //        }
        //        else
        //        {
        //            buffer = new byte[internalClient.Buffer.Length];
        //            Buffer.BlockCopy(internalClient.Buffer, 0, buffer, 0, internalClient.Buffer.Length);
        //        }



        //        try
        //        {
        //            int len = 2;
        //            int packetLen = GetPacketLen(buffer, 0, len);
        //            int bufLen = buffer.Length;
        //            int srcOffset = 0;
        //            int lastPacketLen = 0;
        //            while(bufLen>=(len+packetLen))
        //            {
        //                srcOffset += len;
        //                lastPacketLen = packetLen;

        //                byte[] newBuffer = new byte[packetLen];
        //                Buffer.BlockCopy(buffer, srcOffset, newBuffer, 0, packetLen);
        //                logger.Debug("--------------------" + newBuffer.Length);
        //                //TcpDatagramToEnqueue(newBuffer);

        //                srcOffset += packetLen;
        //                bufLen -= len + packetLen;

        //                packetLen = GetPacketLen(buffer, srcOffset, len);
        //            }

        //            if(bufLen < (len + lastPacketLen))
        //            {
        //                tempBuffer = new byte[buffer.Length - srcOffset];
        //                Buffer.BlockCopy(buffer, srcOffset, tempBuffer, 0, tempBuffer.Length);

        //                srcOffset = 0;
        //                lastPacketLen = 0;
        //                packetLen = 0;
        //                bufLen = 0;
        //            }




        //            //while (bufLen >= packetLen)
        //            //{
        //            //    byte[] newBuffer = new byte[packetLen];
        //            //    Buffer.BlockCopy(buffer, srcOffset, newBuffer, 0, packetLen);

        //            //    TcpDatagramToEnqueue(newBuffer);
        //            //    srcOffset += packetLen;
        //            //    bufLen -= len;
        //            //    if (srcOffset + len > buffer.Length)
        //            //    {
        //            //        flag = true;
        //            //        logger.Debug("========================");
        //            //        break;
        //            //    }
        //            //    bufLen -= packetLen;
        //            //    packetLen = GetPacketLen(buffer, srcOffset, len);

        //            //    srcOffset += len;
        //            //}
        //            //if(!flag)
        //            //{
        //            //    if (bufLen  < packetLen)
        //            //    {
        //            //        tempBuffer = new byte[buffer.Length - srcOffset + 2];
        //            //        Buffer.BlockCopy(buffer, srcOffset - 2, tempBuffer, 0, tempBuffer.Length);
        //            //        logger.Debug(tempBuffer.Length + "--------------------");
        //            //    }
        //            //    else
        //            //    {
        //            //        tempBuffer = new byte[0];
        //            //    }
        //            //}
        //            //else
        //            //{
        //            //    tempBuffer = new byte[buffer.Length - srcOffset];
        //            //    Buffer.BlockCopy(buffer, srcOffset, tempBuffer, 0, tempBuffer.Length);
        //            //    flag = false;
        //            //    tempBuffer = new byte[0];
        //            //}

                   
        //            // continue listening for tcp datagram packets
        //            _stream.BeginRead(
        //              internalClient.Buffer,
        //              0,
        //              internalClient.Buffer.Length,
        //              HandleTcpDatagramReceived,
        //              internalClient);
        //        }
        //        catch (Exception ex)
        //        {

        //        }
        //    }
        //}
     //   FileStream m_fs;
        protected void TcpDatagramToEnqueue(byte[] buffer)
        {
            if (buffer.Length > RTPHeader.MIN_HEADER_LEN)
            {
                RTPPacket item = new RTPPacket(buffer, buffer.Length);
                if (item.Payload == null)
                {
                    return;
                }
                logger.Debug("Seq:" + item.Header.SequenceNumber + "----Timestamp:" + item.Header.Timestamp + "-----Length:" + item.Payload.Length);
                //if (this.m_fs == null)
                //{
                //    this.m_fs = new FileStream("D:\\test.h264", FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite, 8 * 1024);
                //}
                //m_fs.Write(item.Payload, 0, item.Payload.Length);
                //m_fs.Flush();
                if (item.Payload != null)
                {
                    lock (_packets)
                    {
                        if (_packets.Count < RTP_PACKETS_MAX_QUEUE_LENGTH)
                        {
                            _packets.Enqueue(item);
                        }
                        else
                        {
                            logger.Warn("RTPChannel.RTPReceive packets queue full, clearing.");
                            _packets.Clear();
                        }
                    }
                }
            }
        }
        #endregion

        public override void Stop()
        {
            base.Stop();
            if (_rtpSocket != null)
            {
                _rtpSocket.Close();
            }
            if (_tcpListener != null)
            {
                _tcpListener.Stop();
            }
        }
    }

}
