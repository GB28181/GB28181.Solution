using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using GB28181.Logger4Net;
using GB28181.Sys;
using SIPSorcery.Sys;

namespace GB28181.Net.RTP
{
    /// <summary>
    /// 帧类型
    /// </summary>
    public enum FrameTypesEnum
    {
        Audio = 0,
        JPEG = 1,
        H264 = 2,
        VP8 = 3,
        H265 = 4
    }

    /// <summary>
    /// Internal class to join the TCP client and buffer together
    /// for easy management in the server
    /// </summary>
    public class TcpClientState
    {
        private NetworkStream _stream;
        /// <summary>
        /// Constructor for a new Client
        /// </summary>
        /// <param name="tcpClient">The TCP client</param>
        /// <param name="buffer">The byte array buffer</param>
        public TcpClientState(TcpClient tcpClient, byte[] buffer)
        {
            if (tcpClient == null)
                throw new ArgumentNullException("tcpClient");
            if (buffer == null)
                throw new ArgumentNullException("buffer");

            this.TcpClient = tcpClient;
            this.Buffer = buffer;
            this._stream = tcpClient.GetStream();
        }

        public TcpClientState(NetworkStream stream, byte[] buffer)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer");
            this.Buffer = buffer;
            this._stream = stream;
        }

        /// <summary>
        /// Gets the TCP Client
        /// </summary>
        public TcpClient TcpClient { get; private set; }

        /// <summary>
        /// Gets the Buffer.
        /// </summary>
        public byte[] Buffer { get; private set; }

        /// <summary>
        /// Gets the network stream
        /// </summary>
        public NetworkStream NetworkStream
        {
            get
            {
                return _stream;
            }
        }
    }
    public class Channel
    {
        protected const int RTP_RECEIVE_BUFFER_SIZE = 4 * 1024 * 1024;
        protected const int RTP_SEND_BUFFER_SIZE = 10000000;
        protected const int RTP_PACKETS_MAX_QUEUE_LENGTH = 5000;   // The maximum number of RTP packets that will be queued.
        protected static ILog logger = AppState.logger;

        protected int _receivePort;
        protected IPEndPoint _remoteEP;
        protected bool _packetOutOrder;
        protected uint _syncSource = 0;
        protected FrameTypesEnum _frameType;
        protected ProtocolType _protocolType;

        protected Queue<RTPPacket> _packets = new Queue<RTPPacket>();

        protected RTPFrame _firstFrame = new RTPFrame();
        protected RTPFrame _secondFrame = new RTPFrame();
        protected RTPFrame _currentFrame = new RTPFrame();
        protected bool IsRunning;

        public event Action<RTPFrame> OnFrameReady;

        protected int _frameTag = 1;

        private TcpConnectMode _tcpMode;

        public virtual Socket SendSocket { get; set; }

        public TcpConnectMode TcpMode
        {
            get { return _tcpMode; }
            private set { _tcpMode = value; }
        }


        public Channel(TcpConnectMode tcpMode, IPAddress ip, int[] port, ProtocolType protocolType, bool packetOutOrder, int receivePort)
        {
            _tcpMode = tcpMode;
            _receivePort = receivePort;
            _remoteEP = new IPEndPoint(ip, receivePort);
            _packetOutOrder = packetOutOrder;
            _syncSource = Convert.ToUInt32(Crypto.GetRandomInt(0, 9999999));
            _frameType = FrameTypesEnum.H264;
            _protocolType = protocolType;
            Initialize(port[0], port[1]);
        }

        public virtual void Initialize(int rtpPort, int rtcpPort)
        {

        }

        public virtual void Start()
        {
            IsRunning = true;
        }

        public virtual void Stop()
        {
            IsRunning = false;
        }

        protected virtual int GetPacketLen(byte[] byData, int index, int count)
        {
            string result = string.Empty;
            count = index + count;
            for (int i = index; i < count; i++)
            {
                result += byData[i].ToString("X2");
            }
            int len = Convert.ToInt32(result, 16);

            return len;
        }

        protected void ProcessRTPPackets()
        {
            try
            {
                while (IsRunning)
                {
                    if (_packets.Count == 0)
                    {
                        Thread.Sleep(1);
                        continue;
                    }
                    Queue<RTPPacket> queue = new Queue<RTPPacket>();
                    lock (_packets)
                    {
                        do
                        {
                            queue.Enqueue(_packets.Dequeue());
                        }
                        while (_packets.Count > 0);
                    }

                    do
                    {
                        //if (_packetOutOrder)
                        //    CombineFrameOutOrder(queue.Dequeue());
                        //else
                            CombineFrame(queue.Dequeue());
                    } while (queue.Count > 0);
                }
            }
            catch (Exception excp)
            {
                logger.Error("Exception RTPChannel.ProcessRTPPackets. " + excp);
            }
        }

        /// <summary>
        /// 合并rtp包为一帧数据
        /// </summary>
        /// <param name="packet">rtp包</param>
        protected virtual void CombineFrame(RTPPacket packet)
        {
            if (_currentFrame.HasMarker || _currentFrame.Timestamp != packet.Header.Timestamp)
            {
                if (OnFrameReady != null)
                {
                    OnFrameReady(_currentFrame);
                }
                _currentFrame.FramePackets.Clear();
            }
            _currentFrame.FrameType = FrameTypesEnum.H264;
            _currentFrame.Timestamp = packet.Header.Timestamp;
            _currentFrame.HasMarker = packet.Header.MarkerBit == 1;
            _currentFrame.AddRTPPacket(packet);
        }

        /// <summary>
        /// 合并rtp包为一帧数据(包乱序处理)
        /// </summary>
        /// <param name="rtpPacket">rtp包</param>
        protected virtual void CombineFrameOutOrder(RTPPacket rtpPacket)
        {
            if (_firstFrame.Count == 0 || _firstFrame.Timestamp == rtpPacket.Header.Timestamp)
            {
                _firstFrame.AddRTPPacket(rtpPacket);
                _firstFrame.FrameType = FrameTypesEnum.H264;
                _firstFrame.Timestamp = rtpPacket.Header.Timestamp;
                _firstFrame.HasMarker = rtpPacket.Header.MarkerBit == 1;
            }
            else if (_secondFrame.Count == 0 || _secondFrame.Timestamp == rtpPacket.Header.Timestamp)
            {
                _secondFrame.AddRTPPacket(rtpPacket);
                _secondFrame.FrameType = FrameTypesEnum.H264;
                _secondFrame.Timestamp = rtpPacket.Header.Timestamp;
                _secondFrame.HasMarker = rtpPacket.Header.MarkerBit == 1;
            }
            else
            {
                //RTPFrame frame = null;
                if (_frameTag == 1)
                {
                    _frameTag = 2;
                    //_firstFrame.HasMarker = _firstFrame.FramePackets.Any(d => d.Header.MarkerBit == 1);
                    //frame = _firstFrame;
                    OnFrameComplete(_firstFrame);
                    _firstFrame.FramePackets.Clear();
                    _firstFrame.AddRTPPacket(rtpPacket);
                }
                else
                {
                    _frameTag = 1;
                    //_secondFrame.HasMarker = _secondFrame.FramePackets.Any(d => d.Header.MarkerBit == 1);
                    //frame = _secondFrame;
                    OnFrameComplete(_secondFrame);
                    _secondFrame.FramePackets.Clear();
                    _secondFrame.AddRTPPacket(rtpPacket);
                }
            }
        }

        private void OnFrameComplete(RTPFrame frame)
        {
            Action<RTPFrame> action = OnFrameReady;

            if (action == null) return;
            foreach (Action<RTPFrame> handler in action.GetInvocationList())
            {
                try { handler(frame); }
                catch { continue; }
            }
        }

        public virtual void SendRTPRaw(byte[] buffer, IPEndPoint remoteEP)
        {

        }
    }
}
