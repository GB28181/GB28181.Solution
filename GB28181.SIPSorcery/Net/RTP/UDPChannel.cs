//-----------------------------------------------------------------------------
// Filename: RTPChannel.cs
//
// Description: Communications channel to send and receive RTP packets.
//
// History:
// 27 Feb 2012	Aaron Clauson	Created.
//
// License: 
// BSD 3-Clause "New" or "Revised" License, see included LICENSE.md file.
//


using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using GB28181.Sys;
using GB28181.Logger4Net;
using System.IO;
using GB28181.Net.RTP;
using SIPSorcery.Sys;

namespace GB28181.Net
{
    public class UDPChannel : Channel
    {
        private const int H264_RTP_HEADER_LENGTH = 2;
        private const int JPEG_RTP_HEADER_LENGTH = 8;
        private const int VP8_RTP_HEADER_LENGTH = 1;
        private const int MAX_FRAMES_QUEUE_LENGTH = 1000;
        private const int RTP_KEEP_ALIVE_INTERVAL = 30;         // The interval at which to send RTP keep-alive packets to keep the RTSP server from closing the connection.
        private const int RTP_TIMEOUT_SECONDS = 60;             // If no RTP pakcets are received during this interval then assume the connection has failed.
        private const int RFC_2435_FREQUENCY_BASELINE = 90000;
        private const int RTP_MAX_PAYLOAD = 1500; //1452;
        private const int RECEIVE_BUFFER_SIZE = 2048;
        private const int MEDIA_PORT_START = 10000;             // Arbitrary port number to start allocating RTP and control ports from.
        private const int MEDIA_PORT_END = 40000;               // Arbitrary port number that RTP and control ports won't be allocated above.


        private const int SRTP_SIGNATURE_LENGTH = 10;           // If SRTP is being used this many extra bytes need to be added to the RTP payload to hold the authentication signature.

        private static DateTime UtcEpoch2036 = new DateTime(2036, 2, 7, 6, 28, 16, DateTimeKind.Utc);
        private static DateTime UtcEpoch1900 = new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private static DateTime UtcEpoch1970 = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private static Mutex _allocatePortsMutex = new Mutex();


        //private TcpListener _tcpListener;
        //private List<TcpClientState> _clients;

        private Socket _rtpSocket;
        private SocketError _rtpSocketError = SocketError.Success;
        private Socket _controlSocket;

        private int _rtpPort;
        private int _controlPort;

        // Fields that track the RTP stream being managed in this channel.
        private ushort _sequenceNumber = 1;
        private uint _timestamp = 0;


        // Frame variables.

        private uint _rtcpTimestamp = 0;
        private Thread _thRTPRecv;
        private Thread _thrtcpRecv;
        public event Action OnRTPSocketDisconnected;
        private Socket _sendSocket;

        public override Socket SendSocket
        {
            get
            {
                return _sendSocket;
            }
            set
            {
                _sendSocket = value;
            }
        }

        public UDPChannel(TcpConnectMode mode, IPAddress ip, int[] port, ProtocolType protocolType, bool packetOutOrder, int receivePort)
            : base(mode, ip, port, protocolType, packetOutOrder, receivePort)
        {
            //_receivePort = receivePort;
            //_remoteEP = remoteEP;
            //_packetOutOrder = packetOutOrder;
            //_syncSource = Convert.ToUInt32(Crypto.GetRandomInt(0, 9999999));
            //_frameType = FrameTypesEnum.H264;
            //_protocolType = protocolType;
            //Initialize(port[0], port[1]);
        }

        public override void Initialize(int rtpPort, int rtcpPort)
        {
            _rtpPort = rtpPort;
            _controlPort = rtcpPort;

            // The potential ports have been found now try and use them.
            if (_protocolType == ProtocolType.Udp)
            {
                _rtpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, _protocolType);
                _rtpSocket.ReceiveBufferSize = RTP_RECEIVE_BUFFER_SIZE;
                _rtpSocket.SendBufferSize = RTP_SEND_BUFFER_SIZE;
                
                _rtpSocket.Bind(new IPEndPoint(IPAddress.Any, _rtpPort));
                _controlSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, _protocolType);
                _controlSocket.Bind(new IPEndPoint(IPAddress.Any, _controlPort));
            }
            logger.Debug("RTPChannel allocated RTP port of " + _rtpPort + " and control port of " + _controlPort + ".");
        }

        /// <summary>
        /// Starts listenting on the RTP and control ports.
        /// </summary>
        public override void Start()
        {
            if (_protocolType == ProtocolType.Udp && _rtpSocket != null && _controlSocket != null)
            {
                _sendSocket = _rtpSocket;
                IsRunning = true;
                ThreadPool.QueueUserWorkItem(delegate { ProcessRTPPackets(); });
                _thRTPRecv = new Thread(new ThreadStart(RTPReceive));
                _thRTPRecv.Start();
                _thrtcpRecv = new Thread(new ThreadStart(RTCPReceive));
                _thrtcpRecv.Start();
            }
            //else if (_protocolType == ProtocolType.Tcp)
            //{
            //    if (!IsRunning)
            //    {
            //        _clients = new List<TcpClientState>();
            //        _tcpListener = new TcpListener(IPAddress.Any, _rtpPort);
            //        _tcpListener.AllowNatTraversal(true);
            //        IsRunning = true;
            //        _tcpListener.Start();
            //        _tcpListener.BeginAcceptTcpClient(new AsyncCallback(HandleTcpClientAccepted), _tcpListener);
            //    }
            //}
            else
            {
                logger.Warn("An RTPChannel could not start as either RTP or control sockets were not available.");
            }
        }

        private void RTPReceive()
        {
            try
            {
                byte[] buffer = new byte[1500];
                while (IsRunning)
                {
                    if (_rtpSocket == null || _rtpSocket.Available == 0)
                    {
                        Thread.Sleep(10);
                        continue;
                    }
                    int bytesRead = _rtpSocket.Receive(buffer);
                    if (bytesRead > RTPHeader.MIN_HEADER_LEN)
                    {
                        RTPPacket rtpPacket = new RTPPacket(buffer, bytesRead);
                        if (rtpPacket == null)
                        {
                            Thread.Sleep(1);
                            continue;
                        }
                        lock (_packets)
                        {
                            if (_packets.Count < RTP_PACKETS_MAX_QUEUE_LENGTH)
                            {
                                _packets.Enqueue(rtpPacket);
                            }
                            else
                            {
                                logger.Warn("RTPChannel.RTPReceive packets queue full, clearing.");
                                _packets.Clear();
                            }
                        }
                    }
                    else
                    {
                        logger.Warn("Zero bytes read from RTPChannel RTP socket connected to " + _remoteEP + ".");
                    }
                }
            }
            catch (SocketException)
            {

            }
            catch (Exception excp)
            {
                logger.Error("Exception RTPChannel.RTPReceive. " + excp);
            }
        }

        private void RTCPReceive()
        {
            Thread.CurrentThread.Name = "rtpchanrecv-" + _rtpPort;

            byte[] buffer = new byte[1024];

            DateTime packetTimestamp = DateTime.Now;
            try
            {
                EndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
                while (IsRunning)
                {
                    if (_controlSocket == null || _controlSocket.Available == 0)
                    {
                        Thread.Sleep(50);
                        continue;
                    }
                    int bytesRead = _controlSocket.ReceiveFrom(buffer, ref remoteEndPoint);
                    if (bytesRead > 0)
                    {
                        _rtcpTimestamp = DateTimeToNptTimestamp90K(DateTime.Now);
                        RTCPPacket senderReport = new RTCPPacket(_syncSource, DateTimeToNptTimestamp(packetTimestamp), _rtcpTimestamp, 0, 0);
                        byte[] rtcpPacket = senderReport.GetBytes();
                        _controlSocket.SendTo(rtcpPacket, 0, rtcpPacket.Length, SocketFlags.None, remoteEndPoint);
                    }
                    Thread.Sleep(5);
                }
            }
            catch (SocketException)
            {

            }
            catch (Exception ex)
            {
                logger.Error("Exception RTCPChannel.ControlSocketReceive. " + ex);
            }
        }

        /// <summary>
        /// Closes the session's RTP and control ports.
        /// </summary>
        public override void Stop()
        {
            if (IsRunning)
            {
                try
                {
                    logger.Debug("RTPChannel closing, RTP port " + _rtpPort + ".");
                    IsRunning = false;
                    if (_rtpSocket != null)
                    {
                        _rtpSocket.Close();
                    }

                    if (_controlSocket != null)
                    {
                        _controlSocket.Close();
                    }
                }
                catch (Exception excp)
                {
                    logger.Error("Exception RTChannel.Close. " + excp);
                }
            }
        }


        #region 发送到其他
        /// <summary>
        /// Sends an audio frame where the payload size is less than the maximum RTP packet payload size.
        /// </summary>
        /// <param name="payload">The audio payload to transmit.</param>
        /// <param name="frameSpacing">The increment to add to the RTP timestamp for each new frame.</param>
        /// <param name="payloadType">The payload type to set on the RTP packet.</param>
        public void SendAudioFrame(byte[] payload, uint frameSpacing, int payloadType)
        {
            try
            {
                if (!IsRunning)
                {
                    logger.Warn("SendAudioFrame cannot be called on a closed RTP channel.");
                }
                else if (_rtpSocketError != SocketError.Success)
                {
                    logger.Warn("SendAudioFrame was called for an RTP socket in an error state of " + _rtpSocketError + ".");
                }
                else
                {
                    _timestamp = (_timestamp == 0) ? DateTimeToNptTimestamp32(DateTime.Now) : (_timestamp + frameSpacing) % UInt32.MaxValue;

                    RTPPacket rtpPacket = new RTPPacket(payload.Length);
                    rtpPacket.Header.SyncSource = _syncSource;
                    rtpPacket.Header.SequenceNumber = _sequenceNumber++;
                    rtpPacket.Header.Timestamp = _timestamp;
                    rtpPacket.Header.MarkerBit = 1;
                    rtpPacket.Header.PayloadType = payloadType;

                    Buffer.BlockCopy(payload, 0, rtpPacket.Payload, 0, payload.Length);

                    byte[] rtpBytes = rtpPacket.GetBytes();

                    //Stopwatch sw = new Stopwatch();
                    //sw.Start();

                    _rtpSocket.SendTo(rtpBytes, rtpBytes.Length, SocketFlags.None, _remoteEP);

                    //sw.Stop();

                    //if (sw.ElapsedMilliseconds > 15)
                    //{
                    //    logger.Warn(" SendAudioFrame offset " + offset + ", payload length " + payloadLength + ", sequence number " + rtpPacket.Header.SequenceNumber + ", marker " + rtpPacket.Header.MarkerBit + ", took " + sw.ElapsedMilliseconds + "ms.");
                    //}
                }
            }
            catch (Exception excp)
            {
                if (IsRunning)
                {
                    logger.Warn("Exception RTPChannel.SendAudioFrame attempting to send to the RTP socket at " + _remoteEP + ". " + excp);

                    if (OnRTPSocketDisconnected != null)
                    {
                        OnRTPSocketDisconnected();
                    }
                }
            }
        }

        /// <summary>
        /// Helper method to send a low quality JPEG image over RTP. This method supports a very abbreviated version of RFC 2435 "RTP Payload Format for JPEG-compressed Video".
        /// It's intended as a quick convenient way to send something like a test pattern image over an RTSP connection. More than likely it won't be suitable when a high
        /// quality image is required since the header used in this method does not support quantization tables.
        /// </summary>
        /// <param name="jpegBytes">The raw encoded bytes of teh JPEG image to transmit.</param>
        /// <param name="jpegQuality">The encoder quality of the JPEG image.</param>
        /// <param name="jpegWidth">The width of the JPEG image.</param>
        /// <param name="jpegHeight">The height of the JPEG image.</param>
        /// <param name="framesPerSecond">The rate at which the JPEG frames are being transmitted at. used to calculate the timestamp.</param>
        public void SendJpegFrame(byte[] jpegBytes, int jpegQuality, int jpegWidth, int jpegHeight, int framesPerSecond)
        {
            try
            {
                if (!IsRunning)
                {
                    logger.Warn("SendJpegFrame cannot be called on a closed session.");
                }
                else if (_rtpSocketError != SocketError.Success)
                {
                    logger.Warn("SendJpegFrame was called for an RTP socket in an error state of " + _rtpSocketError + ".");
                }
                else
                {
                    _timestamp = (_timestamp == 0) ? DateTimeToNptTimestamp32(DateTime.Now) : (_timestamp + (uint)(RFC_2435_FREQUENCY_BASELINE / framesPerSecond)) % UInt32.MaxValue;

                    //System.Diagnostics.Debug.WriteLine("Sending " + jpegBytes.Length + " encoded bytes to client, timestamp " + _timestamp + ", starting sequence number " + _sequenceNumber + ", image dimensions " + jpegWidth + " x " + jpegHeight + ".");

                    for (int index = 0; index * RTP_MAX_PAYLOAD < jpegBytes.Length; index++)
                    {
                        uint offset = Convert.ToUInt32(index * RTP_MAX_PAYLOAD);
                        int payloadLength = ((index + 1) * RTP_MAX_PAYLOAD < jpegBytes.Length) ? RTP_MAX_PAYLOAD : jpegBytes.Length - index * RTP_MAX_PAYLOAD;

                        byte[] jpegHeader = CreateLowQualityRtpJpegHeader(offset, jpegQuality, jpegWidth, jpegHeight);

                        List<byte> packetPayload = new List<byte>();
                        packetPayload.AddRange(jpegHeader);
                        packetPayload.AddRange(jpegBytes.Skip(index * RTP_MAX_PAYLOAD).Take(payloadLength));

                        RTPPacket rtpPacket = new RTPPacket(packetPayload.Count);
                        rtpPacket.Header.SyncSource = _syncSource;
                        rtpPacket.Header.SequenceNumber = _sequenceNumber++;
                        rtpPacket.Header.Timestamp = _timestamp;
                        rtpPacket.Header.MarkerBit = ((index + 1) * RTP_MAX_PAYLOAD < jpegBytes.Length) ? 0 : 1;
                        rtpPacket.Header.PayloadType = (int)SDPMediaFormatsEnum.JPEG;
                        rtpPacket.Payload = packetPayload.ToArray();

                        byte[] rtpBytes = rtpPacket.GetBytes();

                        //System.Diagnostics.Debug.WriteLine(" offset " + offset + ", payload length " + payloadLength + ", sequence number " + rtpPacket.Header.SequenceNumber + ", marker " + rtpPacket.Header.MarkerBit + ".");

                        //Stopwatch sw = new Stopwatch();
                        //sw.Start();

                        _rtpSocket.SendTo(rtpBytes, _remoteEP);

                        //sw.Stop();

                        //if (sw.ElapsedMilliseconds > 15)
                        //{
                        //    logger.Warn(" SendJpegFrame offset " + offset + ", payload length " + payloadLength + ", sequence number " + rtpPacket.Header.SequenceNumber + ", marker " + rtpPacket.Header.MarkerBit + ", took " + sw.ElapsedMilliseconds + "ms.");
                        //}
                    }

                    //sw.Stop();
                    //System.Diagnostics.Debug.WriteLine("SendJpegFrame took " + sw.ElapsedMilliseconds + ".");
                }
            }
            catch (Exception excp)
            {
                if (IsRunning)
                {
                    logger.Warn("Exception RTPChannel.SendJpegFrame attempting to send to the RTP socket at " + _remoteEP + ". " + excp);
                    //_rtpSocketError = SocketError.SocketError;

                    if (OnRTPSocketDisconnected != null)
                    {
                        OnRTPSocketDisconnected();
                    }
                }
            }
        }

        /// <summary>
        /// H264 frames need a two byte header when transmitted over RTP.
        /// </summary>
        /// <param name="frame">The H264 encoded frame to transmit.</param>
        /// <param name="frameSpacing">The increment to add to the RTP timestamp for each new frame.</param>
        /// <param name="payloadType">The payload type to set on the RTP packet.</param>
        public void SendH264Frame(byte[] frame, uint frameSpacing, int payloadType)
        {
            try
            {
                if (!IsRunning)
                {
                    logger.Warn("SendH264Frame cannot be called on a closed session.");
                }
                else if (_rtpSocketError != SocketError.Success)
                {
                    logger.Warn("SendH264Frame was called for an RTP socket in an error state of " + _rtpSocketError + ".");
                }
                else
                {
                    _timestamp = (_timestamp == 0) ? DateTimeToNptTimestamp32(DateTime.Now) : (_timestamp + frameSpacing) % UInt32.MaxValue;

                    //System.Diagnostics.Debug.WriteLine("Sending " + frame.Length + " H264 encoded bytes to client, timestamp " + _timestamp + ", starting sequence number " + _sequenceNumber + ".");

                    for (int index = 0; index * RTP_MAX_PAYLOAD < frame.Length; index++)
                    {
                        uint offset = Convert.ToUInt32(index * RTP_MAX_PAYLOAD);
                        int payloadLength = ((index + 1) * RTP_MAX_PAYLOAD < frame.Length) ? RTP_MAX_PAYLOAD : frame.Length - index * RTP_MAX_PAYLOAD;

                        RTPPacket rtpPacket = new RTPPacket(payloadLength + H264_RTP_HEADER_LENGTH);
                        rtpPacket.Header.SyncSource = _syncSource;
                        rtpPacket.Header.SequenceNumber = _sequenceNumber++;
                        rtpPacket.Header.Timestamp = _timestamp;
                        rtpPacket.Header.MarkerBit = 0;
                        rtpPacket.Header.PayloadType = payloadType;

                        // Start RTP packet in frame 0x1c 0x89
                        // Middle RTP packet in frame 0x1c 0x09
                        // Last RTP packet in frame 0x1c 0x49

                        byte[] h264Header = new byte[] { 0x1c, 0x09 };

                        if (index == 0 && frame.Length < RTP_MAX_PAYLOAD)
                        {
                            // First and last RTP packet in the frame.
                            h264Header = new byte[] { 0x1c, 0x49 };
                            rtpPacket.Header.MarkerBit = 1;
                        }
                        else if (index == 0)
                        {
                            h264Header = new byte[] { 0x1c, 0x89 };
                        }
                        else if ((index + 1) * RTP_MAX_PAYLOAD > frame.Length)
                        {
                            h264Header = new byte[] { 0x1c, 0x49 };
                            rtpPacket.Header.MarkerBit = 1;
                        }

                        var h264Stream = frame.Skip(index * RTP_MAX_PAYLOAD).Take(payloadLength).ToList();
                        h264Stream.InsertRange(0, h264Header);
                        rtpPacket.Payload = h264Stream.ToArray();

                        byte[] rtpBytes = rtpPacket.GetBytes();

                        //System.Diagnostics.Debug.WriteLine(" offset " + (index * RTP_MAX_PAYLOAD) + ", payload length " + payloadLength + ", sequence number " + rtpPacket.Header.SequenceNumber + ", marker " + rtpPacket.Header .MarkerBit + ".");

                        //Stopwatch sw = new Stopwatch();
                        //sw.Start();

                        _rtpSocket.SendTo(rtpBytes, rtpBytes.Length, SocketFlags.None, _remoteEP);

                        //sw.Stop();

                        //if (sw.ElapsedMilliseconds > 15)
                        //{
                        //    logger.Warn(" SendH264Frame offset " + offset + ", payload length " + payloadLength + ", sequence number " + rtpPacket.Header.SequenceNumber + ", marker " + rtpPacket.Header.MarkerBit + ", took " + sw.ElapsedMilliseconds + "ms.");
                        //}
                    }
                }
            }
            catch (Exception excp)
            {
                if (IsRunning)
                {
                    logger.Warn("Exception RTPChannel.SendH264Frame attempting to send to the RTP socket at " + _remoteEP + ". " + excp);

                    if (OnRTPSocketDisconnected != null)
                    {
                        OnRTPSocketDisconnected();
                    }
                }
            }
        }

        /// <summary>
        /// Sends a dynamically sized frame. The RTP marker bit will be set for the last transmitted packet in the frame.
        /// </summary>
        /// <param name="frame">The frame to transmit.</param>
        /// <param name="frameSpacing">The increment to add to the RTP timestamp for each new frame.</param>
        /// <param name="payloadType">The payload type to set on the RTP packet.</param>
        public void SendVP8Frame(byte[] frame, uint frameSpacing, int payloadType)
        {
            try
            {
                if (!IsRunning)
                {
                    logger.Warn("SendVP8Frame cannot be called on a closed RTP channel.");
                }
                else if (_rtpSocketError != SocketError.Success)
                {
                    logger.Warn("SendVP8Frame was called for an RTP socket in an error state of " + _rtpSocketError + ".");
                }
                else
                {
                    _timestamp = (_timestamp == 0) ? DateTimeToNptTimestamp32(DateTime.Now) : (_timestamp + frameSpacing) % UInt32.MaxValue;

                    //System.Diagnostics.Debug.WriteLine("Sending " + frame.Length + " encoded bytes to client, timestamp " + _timestamp + ", starting sequence number " + _sequenceNumber + ".");

                    for (int index = 0; index * RTP_MAX_PAYLOAD < frame.Length; index++)
                    {
                        //byte[] vp8HeaderBytes = (index == 0) ? new byte[VP8_RTP_HEADER_LENGTH] { 0x90, 0x80, (byte)(_sequenceNumber % 128) } : new byte[VP8_RTP_HEADER_LENGTH] { 0x80, 0x80, (byte)(_sequenceNumber % 128) };
                        byte[] vp8HeaderBytes = (index == 0) ? new byte[VP8_RTP_HEADER_LENGTH] { 0x10 } : new byte[VP8_RTP_HEADER_LENGTH] { 0x00 };

                        int offset = index * RTP_MAX_PAYLOAD;
                        int payloadLength = ((index + 1) * RTP_MAX_PAYLOAD < frame.Length) ? RTP_MAX_PAYLOAD : frame.Length - index * RTP_MAX_PAYLOAD;

                        // RTPPacket rtpPacket = new RTPPacket(payloadLength + VP8_RTP_HEADER_LENGTH + ((_srtp != null) ? SRTP_SIGNATURE_LENGTH : 0));
                        RTPPacket rtpPacket = new RTPPacket(payloadLength + VP8_RTP_HEADER_LENGTH);
                        rtpPacket.Header.SyncSource = _syncSource;
                        rtpPacket.Header.SequenceNumber = _sequenceNumber++;
                        rtpPacket.Header.Timestamp = _timestamp;
                        rtpPacket.Header.MarkerBit = (offset + payloadLength >= frame.Length) ? 1 : 0;
                        rtpPacket.Header.PayloadType = payloadType;

                        Buffer.BlockCopy(vp8HeaderBytes, 0, rtpPacket.Payload, 0, vp8HeaderBytes.Length);
                        Buffer.BlockCopy(frame, offset, rtpPacket.Payload, vp8HeaderBytes.Length, payloadLength);

                        byte[] rtpBytes = rtpPacket.GetBytes();

                        //if (_srtp != null)
                        //{
                        //    int rtperr = _srtp.ProtectRTP(rtpBytes, rtpBytes.Length - SRTP_SIGNATURE_LENGTH);
                        //    if (rtperr != 0)
                        //    {
                        //        logger.Warn("An error was returned attempting to sign an SRTP packet for " + _remoteEndPoint + ", error code " + rtperr + ".");
                        //    }
                        //}

                        //System.Diagnostics.Debug.WriteLine(" offset " + (index * RTP_MAX_PAYLOAD) + ", payload length " + payloadLength + ", sequence number " + rtpPacket.Header.SequenceNumber + ", marker " + rtpPacket.Header .MarkerBit + ".");

                        //Stopwatch sw = new Stopwatch();
                        //sw.Start();

                        _rtpSocket.SendTo(rtpBytes, rtpBytes.Length, SocketFlags.None, _remoteEP);

                        //sw.Stop();

                        //if (sw.ElapsedMilliseconds > 15)
                        //{
                        //    logger.Warn(" SendVP8Frame offset " + offset + ", payload length " + payloadLength + ", sequence number " + rtpPacket.Header.SequenceNumber + ", marker " + rtpPacket.Header.MarkerBit + ", took " + sw.ElapsedMilliseconds + "ms.");
                        //}
                    }
                }
            }
            catch (Exception excp)
            {
                if (IsRunning)
                {
                    logger.Warn("Exception RTPChannel.SendVP8Frame attempting to send to the RTP socket at " + _remoteEP + ". " + excp);

                    if (OnRTPSocketDisconnected != null)
                    {
                        OnRTPSocketDisconnected();
                    }
                }
            }
        }

        /// <summary>
        /// Sends a packet to the RTSP server on the RTP socket.
        /// </summary>
        public override void SendRTPRaw(byte[] payload, IPEndPoint remoteEP)
        {
            try
            {
                if (IsRunning && _rtpSocket != null && remoteEP != null && _rtpSocketError == SocketError.Success)
                {
                    _rtpSocket.SendTo(payload, 0, payload.Length, SocketFlags.None, remoteEP);
                }
            }
            catch (Exception excp)
            {
                if (IsRunning)
                {
                    logger.Error("Exception RTPChannel.SendRTPRaw attempting to send to " + _remoteEP + ". " + excp);

                    if (OnRTPSocketDisconnected != null)
                    {
                        OnRTPSocketDisconnected();
                    }
                }
            }
        }
        #endregion

        public static uint DateTimeToNptTimestamp32(DateTime value) { return (uint)((DateTimeToNptTimestamp(value) >> 16) & 0xFFFFFFFF); }

        /// <summary>
        /// Converts specified DateTime value to long NPT time.
        /// </summary>
        /// <param name="value">DateTime value to convert. This value must be in local time.</param>
        /// <returns>Returns NPT value.</returns>
        /// <notes>
        /// Wallclock time (absolute date and time) is represented using the
        /// timestamp format of the Network Time Protocol (NPT), which is in
        /// seconds relative to 0h UTC on 1 January 1900 [4].  The full
        /// resolution NPT timestamp is a 64-bit unsigned fixed-point number with
        /// the integer part in the first 32 bits and the fractional part in the
        /// last 32 bits. In some fields where a more compact representation is
        /// appropriate, only the middle 32 bits are used; that is, the low 16
        /// bits of the integer part and the high 16 bits of the fractional part.
        /// The high 16 bits of the integer part must be determined independently.
        /// </notes>
        public static ulong DateTimeToNptTimestamp(DateTime value)
        {
            DateTime baseDate = value >= UtcEpoch2036 ? UtcEpoch2036 : UtcEpoch1900;

            TimeSpan elapsedTime = value > baseDate ? value.ToUniversalTime() - baseDate.ToUniversalTime() : baseDate.ToUniversalTime() - value.ToUniversalTime();

            return ((ulong)(elapsedTime.Ticks / TimeSpan.TicksPerSecond) << 32) | (uint)(elapsedTime.Ticks / TimeSpan.TicksPerSecond * 0x100000000L);
        }

        public static uint DateTimeToNptTimestamp90K(DateTime value)
        {
            DateTime baseDate = value >= UtcEpoch2036 ? UtcEpoch2036 : UtcEpoch1900;

            TimeSpan elapsedTime = value > baseDate ? value.ToUniversalTime() - baseDate.ToUniversalTime() : baseDate.ToUniversalTime() - value.ToUniversalTime();

            var ticks90k = elapsedTime.TotalMilliseconds * 90;

            return (uint)(ticks90k % UInt32.MaxValue);
        }



        /// <summary>
        /// Utility function to create RtpJpegHeader either for initial packet or template for further packets
        /// 
        /// 0                   1                   2                   3
        /// 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
        /// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
        /// | Type-specific |              Fragment Offset                  |
        /// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
        /// |      Type     |       Q       |     Width     |     Height    |
        /// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
        /// </summary>
        /// <param name="fragmentOffset"></param>
        /// <param name="quality"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        private static byte[] CreateLowQualityRtpJpegHeader(uint fragmentOffset, int quality, int width, int height)
        {
            byte[] rtpJpegHeader = new byte[8] { 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00 };

            // Byte 0: Type specific
            //http://tools.ietf.org/search/rfc2435#section-3.1.1

            // Bytes 1 to 3: Three byte fragment offset
            //http://tools.ietf.org/search/rfc2435#section-3.1.2

            if (BitConverter.IsLittleEndian) fragmentOffset = NetConvert.DoReverseEndian(fragmentOffset);

            byte[] offsetBytes = BitConverter.GetBytes(fragmentOffset);
            rtpJpegHeader[1] = offsetBytes[2];
            rtpJpegHeader[2] = offsetBytes[1];
            rtpJpegHeader[3] = offsetBytes[0];

            // Byte 4: JPEG Type.
            //http://tools.ietf.org/search/rfc2435#section-3.1.3

            //Byte 5: http://tools.ietf.org/search/rfc2435#section-3.1.4 (Q)
            rtpJpegHeader[5] = (byte)quality;

            // Byte 6: http://tools.ietf.org/search/rfc2435#section-3.1.5 (Width)
            rtpJpegHeader[6] = (byte)(width / 8);

            // Byte 7: http://tools.ietf.org/search/rfc2435#section-3.1.6 (Height)
            rtpJpegHeader[7] = (byte)(height / 8);

            return rtpJpegHeader;
        }
    }
}
