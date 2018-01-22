using log4net;
using SIPSorcery.GB28181.Net;
using SIPSorcery.GB28181.Servers.SIPMonitor;
using SIPSorcery.GB28181.SIP;
using SIPSorcery.GB28181.SIP.App;
using SIPSorcery.GB28181.Sys;
using SIPSorcery.GB28181.Sys.XML;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace SIPSorcery.GB28181.Servers.SIPMessage
{
    /// <summary>
    /// SIP服务状态
    /// </summary>
    public enum SipServiceStatus
    {
        /// <summary>
        /// 等待
        /// </summary>
        Wait = 0,
        /// <summary>
        /// 初始化完成
        /// </summary>
        Complete = 1
    }

    public enum PTZCommand:int
    {
        None = 0,
        [Description("上")]
        Up = 1,
        [Description("左上")]
        UpLeft = 2,
        [Description("右上")]
        UpRight = 3,
        [Description("下")]
        Down = 4,
        [Description("左下")]
        DownLeft = 5,
        [Description("右下")]
        DownRight = 6,
        [Description("左")]
        Left = 7,
        [Description("右")]
        Right = 8,
        [Description("聚焦+")]
        Focus1 = 9,
        [Description("聚焦-")]
        Focus2 = 10,
        [Description("变倍+")]
        Zoom1 = 11,
        [Description("变倍-")]
        Zoom2 = 12,
        [Description("光圈Open")]
        Iris1 = 13,
        [Description("光圈Close")]
        Iris2 = 14
    }

    /// <summary>
    /// sip消息核心处理
    /// </summary>
    public class SIPMessageCore
    {
        #region 私有字段

        private static ILog logger = AppState.logger;

        private bool _initSIP = false;
        private int MEDIA_PORT_START = 10000;
        private int MEDIA_PORT_END = 20000;
        private RegistrarCore m_registrarCore;
        private TaskTiming _catalogTask;

        /// <summary>
        /// 用户代理
        /// </summary>
        internal string UserAgent;
        /// <summary>
        /// 本地sip终结点
        /// </summary>
        internal SIPEndPoint LocalEndPoint;
        /// <summary>
        /// 远程sip终结点
        /// </summary>
        internal SIPEndPoint RemoteEndPoint;
        /// <summary>
        /// sip传输请求
        /// </summary>
        internal SIPTransport Transport;
        /// <summary>
        /// 本地sip编码
        /// </summary>
        internal string LocalSIPId;
        /// <summary>
        /// 远程sip编码
        /// </summary>
        internal string RemoteSIPId;
        /// <summary>
        /// 监控服务
        /// </summary>
        public Dictionary<string, ISIPMonitorService> MonitorService;
        /// <summary>
        /// sip服务状态
        /// </summary>
        public event Action<string, SipServiceStatus> OnSIPServiceChanged;
        /// <summary>
        /// 设备目录接收
        /// </summary>
        public event Action<Catalog> OnCatalogReceived;
        /// <summary>
        /// 消息发送超时
        /// </summary>
        public event Action<SIPResponse> SendRequestTimeout;
        #endregion

        public SIPMessageCore(SIPTransport transport, string userAgent)
        {
            Transport = transport;
            UserAgent = userAgent;
        }

        public void Initialize(string switchboarduserAgentPrefix,
            SIPAuthenticateRequestDelegate sipRequestAuthenticator,
            GetCanonicalDomainDelegate getCanonicalDomain,
            SIPAssetGetDelegate<SIPAccount> getSIPAccount,
            SIPUserAgentConfigurationManager userAgentConfigs,
            SIPRegistrarBindingsManager registrarBindingsManager,
            Dictionary<string, string> devList)
        {
            m_registrarCore = new RegistrarCore(Transport, registrarBindingsManager, getSIPAccount, getCanonicalDomain, true, true, userAgentConfigs, sipRequestAuthenticator, switchboarduserAgentPrefix);
            m_registrarCore.Start(1);
            MonitorService = new Dictionary<string, ISIPMonitorService>();

            foreach (var item in devList)
            {
                ISIPMonitorService monitor = new SIPMonitorCore(this, item.Key, item.Value);
                monitor.OnSIPServiceChanged += monitor_OnSIPServiceChanged;
                MonitorService.Add(item.Key, monitor);
            }
        }

        /// <summary>
        /// sip请求消息
        /// </summary>
        /// <param name="localSIPEndPoint">本地终结点</param>
        /// <param name="remoteEndPoint">远程终结点</param>
        /// <param name="request">sip请求</param>
        public void AddMessageRequest(SIPEndPoint localSIPEndPoint, SIPEndPoint remoteEndPoint, SIPRequest request)
        {
            //注册请求
            if (request.Method == SIPMethodsEnum.REGISTER)
            {
                m_registrarCore.AddRegisterRequest(localSIPEndPoint, remoteEndPoint, request);
            }
            //消息请求
            else if (request.Method == SIPMethodsEnum.MESSAGE)
            {
                KeepAlive keepAlive = KeepAlive.Instance.Read(request.Body);
                if (keepAlive != null)  //心跳
                {
                    if (!_initSIP)
                    {
                        LocalEndPoint = request.Header.To.ToURI.ToSIPEndPoint();
                        RemoteEndPoint = request.Header.From.FromURI.ToSIPEndPoint();
                        LocalSIPId = request.Header.To.ToURI.User;
                        RemoteSIPId = request.Header.From.FromURI.User;
                    }

                    _initSIP = true;

                    OnSIPServiceChange(RemoteSIPId, SipServiceStatus.Complete);
                }
                else   //目录检索
                {
                    Catalog catalog = Catalog.Instance.Read(request.Body);
                    if (catalog != null)
                    {
                        foreach (var cata in catalog.DeviceList.Items)
                        {
                            lock (MonitorService)
                            {
                                if (!MonitorService.ContainsKey(cata.DeviceID))
                                {
                                    ISIPMonitorService monitor = new SIPMonitorCore(this, cata.DeviceID, cata.Name);
                                    monitor.OnSIPServiceChanged += monitor_OnSIPServiceChanged;
                                    MonitorService.Add(cata.DeviceID, monitor);
                                }
                            }
                        }
                        OnCatalogReceive(catalog);
                    }
                }
                SIPResponse msgRes = GetResponse(localSIPEndPoint, remoteEndPoint, SIPResponseStatusCodesEnum.Ok, "", request);
                Transport.SendResponse(msgRes);
            }
            //停止播放请求
            else if (request.Method == SIPMethodsEnum.BYE)
            {
                SIPResponse byeRes = GetResponse(localSIPEndPoint, remoteEndPoint, SIPResponseStatusCodesEnum.Ok, "", request);
                Transport.SendResponse(byeRes);
            }
        }

        /// <summary>
        /// sip响应消息
        /// </summary>
        /// <param name="localSIPEndPoint">本地终结点</param>
        /// <param name="remoteEndPoint">远程终结点</param>
        /// <param name="response">sip响应</param>
        public void AddMessageResponse(SIPEndPoint localSIPEndPoint, SIPEndPoint remoteEndPoint, SIPResponse response)
        {
            if (SendRequestTimeout != null)
            {
                SendRequestTimeout(response);
            }
            if (response.Status == SIPResponseStatusCodesEnum.Trying)
            {
                logger.Debug("up platform return waiting process msg...");
            }
            else if (response.Status == SIPResponseStatusCodesEnum.Ok)
            {
                if (response.Header.ContentType.ToLower() == "application/sdp")
                {
                    SIPRequest ackReq = MonitorService[response.Header.To.ToURI.User].AckRequest(response);
                    Transport.SendRequest(RemoteEndPoint, ackReq);
                }
            }
            else if (response.Status == SIPResponseStatusCodesEnum.BadRequest)  //请求失败
            {
                string msg = "realVideo bad request";
                if (response.Header.Warning != null)
                {
                    msg += response.Header.Warning;
                }
                MonitorService[response.Header.To.ToURI.User].BadRequest(msg);
            }
            else if (response.Status == SIPResponseStatusCodesEnum.InternalServerError) //服务器内部错误
            {
                string msg = "realVideo InternalServerError request";
                if (response.Header.Warning != null)
                {
                    msg += response.Header.Warning;
                }
                MonitorService[response.Header.To.ToURI.User].BadRequest(msg);
            }
        }

        private void monitor_OnSIPServiceChanged(string msg, SipServiceStatus state)
        {
            OnSIPServiceChange(msg, state);
        }

        public void OnSIPServiceChange(string msg, SipServiceStatus state)
        {
            Action<string, SipServiceStatus> action = OnSIPServiceChanged;

            if (action == null) return;

            foreach (Action<string, SipServiceStatus> handler in action.GetInvocationList())
            {
                try { handler(msg, state); }
                catch { continue; }
            }
        }

        public void OnCatalogReceive(Catalog cata)
        {
            Action<Catalog> action = OnCatalogReceived;
            if (action == null) return;

            foreach (Action<Catalog> handler in action.GetInvocationList())
            {
                try { handler(cata); }
                catch { continue; }
            }
        }

        private SIPResponse GetResponse(SIPEndPoint localSIPEndPoint, SIPEndPoint remoteEndPoint, SIPResponseStatusCodesEnum responseCode, string reasonPhrase, SIPRequest request)
        {
            try
            {
                SIPResponse response = new SIPResponse(responseCode, reasonPhrase, localSIPEndPoint);
                SIPSchemesEnum sipScheme = (localSIPEndPoint.Protocol == SIPProtocolsEnum.tls) ? SIPSchemesEnum.sips : SIPSchemesEnum.sip;
                SIPFromHeader from = request.Header.From;
                from.FromTag = request.Header.From.FromTag;
                SIPToHeader to = request.Header.To;
                response.Header = new SIPHeader(from, to, request.Header.CSeq, request.Header.CallId);
                response.Header.CSeqMethod = request.Header.CSeqMethod;
                response.Header.Vias = request.Header.Vias;
                //response.Header.Server = _userAgent;
                response.Header.UserAgent = UserAgent;
                response.Header.CSeq = request.Header.CSeq;

                if (response.Header.To.ToTag == null || request.Header.To.ToTag.Trim().Length == 0)
                {
                    response.Header.To.ToTag = CallProperties.CreateNewTag();
                }

                return response;
            }
            catch (Exception excp)
            {
                logger.Error("Exception SIPTransport GetResponse. " + excp.Message);
                throw;
            }
        }

        /// <summary>
        /// 设备目录查询
        /// </summary>
        /// <param name="deviceId">目的设备编码</param>
        public void DeviceCatalogQuery(string deviceId)
        {
            if (LocalEndPoint == null)
            {
                OnSIPServiceChange(deviceId, SipServiceStatus.Wait);
                return;
            }
            string fromTag = CallProperties.CreateNewTag();
            string toTag = CallProperties.CreateNewTag();
            int cSeq = CallProperties.CreateNewCSeq();
            string callId = CallProperties.CreateNewCallId();
            SIPRequest catalogReq = QueryItems(fromTag, toTag, cSeq, callId);
            CatalogQuery catalog = new CatalogQuery()
            {
                CommandType = CommandType.Catalog,
                DeviceID = deviceId,
                SN = new Random().Next(9999)
            };
            string xmlBody = CatalogQuery.Instance.Save<CatalogQuery>(catalog);
            catalogReq.Body = xmlBody;
            Transport.SendRequest(RemoteEndPoint, catalogReq);
            _catalogTask = new TaskTiming(catalogReq, Transport);
            this.SendRequestTimeout += _catalogTask.MessageSendRequestTimeout;
            _catalogTask.Start();
        }

        /// <summary>
        /// PTZ云台控制
        /// </summary>
        /// <param name="deviceId">设备ID</param>
        /// <param name="ucommand">控制命令</param>
        /// <param name="dwStop">开始或结束</param>
        /// <param name="dwSpeed">速度</param>
        public void PtzContrl(string deviceId, int ucommand, int dwStop, int dwSpeed)
        {
            if (LocalEndPoint == null)
            {
                OnSIPServiceChange(deviceId, SipServiceStatus.Wait);
                return;
            }
            string fromTag = CallProperties.CreateNewTag();
            string toTag = CallProperties.CreateNewTag();
            int cSeq = CallProperties.CreateNewCSeq();
            string callId = CallProperties.CreateNewCallId();
            SIPRequest catalogReq = QueryItems(fromTag, toTag, cSeq, callId);
            string cmdStr = GetPtzCmd(ucommand, dwStop, dwSpeed);

            PTZControl ptz = new PTZControl()
            {
                CommandType = CommandType.DeviceControl,
                DeviceID = deviceId,
                SN = new Random().Next(9999),
                PTZCmd = cmdStr
            };
            string xmlBody = PTZControl.Instance.Save<PTZControl>(ptz);
            catalogReq.Body = xmlBody;
            Transport.SendRequest(RemoteEndPoint, catalogReq);
            _catalogTask = new TaskTiming(catalogReq, Transport);
            this.SendRequestTimeout += _catalogTask.MessageSendRequestTimeout;
            _catalogTask.Start();
        }
        #region 拼接ptz控制指令
        /// <summary>
        /// 拼接ptz控制指令
        /// </summary>
        /// <param name="ucommand"></param>
        /// <param name="dwStop"></param>
        /// <param name="dwSpeed"></param>
        /// <returns></returns>
        private string GetPtzCmd(int ucommand, int dwStop, int dwSpeed)
        {
            List<int> cmdList = new List<int>(8);
            cmdList.Add(0xA5);
            cmdList.Add(0x0F);
            cmdList.Add(0x01);
            if (dwStop == 1)//停止云台控制
            {
                cmdList.Add(00);
                cmdList.Add(00);
                cmdList.Add(00);
                cmdList.Add(00);
                cmdList.Add(0xB5);
            }
            else//开始云台控制
            {
                switch ((PTZCommand)ucommand)
                {
                    case PTZCommand.Up:
                        cmdList.Add(0x08);
                        cmdList.Add(dwSpeed);
                        cmdList.Add(00);
                        cmdList.Add(00);
                        break;
                    case PTZCommand.Down:
                        cmdList.Add(0x04);
                        cmdList.Add(dwSpeed);
                        cmdList.Add(00);
                        cmdList.Add(00);
                        break;
                    case PTZCommand.Left:
                        cmdList.Add(0x02);
                        cmdList.Add(00);
                        cmdList.Add(dwSpeed);
                        cmdList.Add(00);
                        break;
                    case PTZCommand.Right:
                        cmdList.Add(0x01);
                        cmdList.Add(00);
                        cmdList.Add(dwSpeed);
                        cmdList.Add(00);
                        break;
                    case PTZCommand.UpRight:
                        cmdList.Add(0x9);
                        cmdList.Add(dwSpeed);
                        cmdList.Add(dwSpeed);
                        cmdList.Add(00);
                        break;
                    case PTZCommand.DownRight:
                        cmdList.Add(0x09);
                        cmdList.Add(dwSpeed);
                        cmdList.Add(dwSpeed);
                        cmdList.Add(00);
                        break;
                    case PTZCommand.UpLeft:
                        cmdList.Add(0x0A);
                        cmdList.Add(dwSpeed);
                        cmdList.Add(dwSpeed);
                        cmdList.Add(00);
                        break;
                    case PTZCommand.DownLeft:
                        cmdList.Add(0x06);
                        cmdList.Add(dwSpeed);
                        cmdList.Add(dwSpeed);
                        cmdList.Add(00);
                        break;
                    case PTZCommand.Zoom1://镜头放大
                        cmdList.Add(0x10);
                        cmdList.Add(00);
                        cmdList.Add(00);
                        cmdList.Add(dwSpeed << 4);
                        break;
                    case PTZCommand.Zoom2://镜头缩小
                        cmdList.Add(0x20);
                        cmdList.Add(00);
                        cmdList.Add(00);
                        cmdList.Add(dwSpeed << 4);
                        break;
                    case PTZCommand.Focus1://聚焦+
                        cmdList.Add(0x42);
                        cmdList.Add(dwSpeed);
                        cmdList.Add(00);
                        cmdList.Add(00);
                        break;
                    case PTZCommand.Focus2://聚焦—
                        cmdList.Add(0x41);
                        cmdList.Add(dwSpeed);
                        cmdList.Add(00);
                        cmdList.Add(00);
                        break;
                    case PTZCommand.Iris1: //光圈open
                        cmdList.Add(0x44);
                        cmdList.Add(00);
                        cmdList.Add(dwSpeed);
                        cmdList.Add(00);
                        break;
                    case PTZCommand.Iris2: //光圈close
                        cmdList.Add(0x48);
                        cmdList.Add(00);
                        cmdList.Add(dwSpeed);
                        cmdList.Add(00);
                        break;
                    default:
                        break;
                }
            }

            int checkBit = 0;
            foreach (int cmdItem in cmdList)
            {
                checkBit = checkBit + cmdItem;
            }
            checkBit = checkBit % 256;
            cmdList.Add(checkBit);

            string cmdStr = string.Empty;
            foreach (var cmdItemStr in cmdList)
            {
                cmdStr = cmdStr + cmdItemStr.ToString("X").PadLeft(2, '0');
            }
            return cmdStr;
        }
        #endregion

        /// <summary>
        /// 查询设备目录请求
        /// </summary>
        /// <returns></returns>
        private SIPRequest QueryItems(string fromTag, string toTag, int cSeq, string callId)
        {
            SIPURI remoteUri = new SIPURI(RemoteSIPId, RemoteEndPoint.ToHost(), "");
            SIPURI localUri = new SIPURI(LocalSIPId, LocalEndPoint.ToHost(), "");
            SIPFromHeader from = new SIPFromHeader(null, localUri, fromTag);
            SIPToHeader to = new SIPToHeader(null, remoteUri, toTag);
            SIPRequest catalogReq = Transport.GetRequest(SIPMethodsEnum.MESSAGE, remoteUri);
            catalogReq.Header.From = from;
            catalogReq.Header.Contact = null;
            catalogReq.Header.Allow = null;
            catalogReq.Header.To = to;
            catalogReq.Header.UserAgent = UserAgent;
            catalogReq.Header.CSeq = cSeq;
            catalogReq.Header.CallId = callId;
            catalogReq.Header.ContentType = "application/MANSCDP+xml";
            return catalogReq;
        }

        public void Stop()
        {
            if (_catalogTask != null)
            {
                _catalogTask.Stop();
            }
            foreach (var item in MonitorService)
            {
                item.Value.Stop();
            }
            LocalEndPoint = null;
            LocalSIPId = null;
            RemoteEndPoint = null;
            RemoteSIPId = null;
            Transport = null;
            MonitorService.Clear();
            MonitorService = null;
        }

        /// <summary>
        /// 设置媒体(rtp/rtcp)端口号
        /// </summary>
        /// <returns></returns>
        public int[] SetMediaPort()
        {
            var inUseUDPPorts = (from p in IPGlobalProperties.GetIPGlobalProperties().GetActiveUdpListeners() where p.Port >= MEDIA_PORT_START select p.Port).OrderBy(x => x).ToList();

            int rtpPort = 0;
            int rtcpPort = 0;

            if (inUseUDPPorts.Count > 0)
            {
                // Find the first two available for the RTP socket.
                for (int index = MEDIA_PORT_START; index <= MEDIA_PORT_END; index++)
                {
                    if (!inUseUDPPorts.Contains(index))
                    {
                        rtpPort = index;
                        break;
                    }
                }

                // Find the next available for the control socket.
                for (int index = rtpPort + 1; index <= MEDIA_PORT_END; index++)
                {
                    if (!inUseUDPPorts.Contains(index))
                    {
                        rtcpPort = index;
                        break;
                    }
                }
            }
            else
            {
                rtpPort = MEDIA_PORT_START;
                rtcpPort = MEDIA_PORT_START + 1;
            }

            if (MEDIA_PORT_START >= MEDIA_PORT_END)
            {
                MEDIA_PORT_START = 10000;
            }
            MEDIA_PORT_START += 2;
            int[] mediaPort = new int[2];
            mediaPort[0] = rtpPort;
            mediaPort[1] = rtcpPort;
            return mediaPort;
        }
    }

    //    #region 构造函数
    //    /// <summary>
    //    /// sip监控初始化
    //    /// </summary>
    //    /// <param name="messageCore">sip消息</param>
    //    /// <param name="sipTransport">sip传输</param>
    //    /// <param name="cameraId">摄像机编码</param>
    //    public SIPMessageCore(SIPMessageCore messageCore, string cameraId)
    //    {
    //        _messageCore = messageCore;
    //        _m_sipTransport = messageCore.m_sipTransport;
    //        _cameraId = cameraId;
    //        _userAgent = messageCore.m_userAgent;
    //        _rtcpSyncSource = Convert.ToUInt32(Crypto.GetRandomInt(0, 9999999));

    //        _messageCore.SipRequestInited += messageCore_SipRequestInited;
    //        _messageCore.SipInviteVideoOK += messageCore_SipInviteVideoOK;
    //    } 
    //    #endregion

    //    #region 确认视频请求
    //    /// <summary>
    //    /// 实时视频请求成功事件处理
    //    /// </summary>
    //    /// <param name="res"></param>
    //    private void messageCore_SipInviteVideoOK(SIPResponse res)
    //    {
    //        if (_realReqSession == null)
    //        {
    //            return;
    //        }
    //        //同一会话消息
    //        if (_realReqSession.Header.CallId == res.Header.CallId)
    //        {
    //            RealVideoRes realRes = RealVideoRes.Instance.Read(res.Body);
    //            GetRemoteRtcp(realRes.Socket);

    //            SIPRequest ackReq = AckRequest(res);
    //            _m_sipTransport.SendRequest(_remoteEndPoint, ackReq);
    //        }
    //    } 
    //    #endregion

    //    #region rtp/rtcp事件处理
    //    /// <summary>
    //    /// sip初始化完成事件
    //    /// </summary>
    //    /// <param name="sipRequest">sip请求</param>
    //    /// <param name="localEndPoint">本地终结点</param>
    //    /// <param name="remoteEndPoint">远程终结点</param>
    //    /// <param name="sipAccount">sip账户</param>
    //    private void messageCore_SipRequestInited(SIPRequest sipRequest, SIPEndPoint localEndPoint, SIPEndPoint remoteEndPoint, SIPAccount sipAccount)
    //    {
    //        _sipInited = true;
    //        _sipRequest = sipRequest;
    //        _localEndPoint = localEndPoint;
    //        _remoteEndPoint = remoteEndPoint;
    //        _sipAccount = sipAccount;

    //        _rtpRemoteEndPoint = new IPEndPoint(remoteEndPoint.Address, remoteEndPoint.Port);
    //        _rtpChannel = new RTPChannel(_rtpRemoteEndPoint);
    //        _rtpChannel.OnFrameReady += _rtpChannel_OnFrameReady;
    //        _rtpChannel.OnControlDataReceived += _rtpChannel_OnControlDataReceived;

    //        if (SipStatusHandler != null)
    //        {
    //            SipStatusHandler(SipServiceStatus.Inited);
    //        }
    //        _messageCore.SipRequestInited -= messageCore_SipRequestInited;
    //    }

    //    /// <summary>
    //    /// rtp包回调事件处理
    //    /// </summary>
    //    /// <param name="frame"></param>
    //    private void _rtpChannel_OnFrameReady(RTPFrame frame)
    //    {
    //        //byte[] buffer = frame.GetFramePayload();
    //        //Write(buffer);
    //    }

    //    /// <summary>
    //    /// rtcp包回调事件处理
    //    /// </summary>
    //    /// <param name="buffer"></param>
    //    /// <param name="rtcpSocket"></param>
    //    private void _rtpChannel_OnControlDataReceived(byte[] buffer, Socket rtcpSocket)
    //    {
    //        _rtcpSocket = rtcpSocket;
    //        DateTime packetTimestamp = DateTime.Now;
    //        _rtcpTimestamp = RTPChannel.DateTimeToNptTimestamp90K(DateTime.Now);
    //        if (_rtcpRemoteEndPoint != null)
    //        {
    //            SendRtcpSenderReport(RTPChannel.DateTimeToNptTimestamp(packetTimestamp), _rtcpTimestamp);
    //        }
    //    }

    //    /// <summary>
    //    /// 发送rtcp包
    //    /// </summary>
    //    /// <param name="ntpTimestamp"></param>
    //    /// <param name="rtpTimestamp"></param>
    //    private void SendRtcpSenderReport(ulong ntpTimestamp, uint rtpTimestamp)
    //    {
    //        try
    //        {
    //            RTCPPacket senderReport = new RTCPPacket(_rtcpSyncSource, ntpTimestamp, rtpTimestamp, _senderPacketCount, _senderOctetCount);
    //            var bytes = senderReport.GetBytes();
    //            _rtcpSocket.BeginSendTo(bytes, 0, bytes.Length, SocketFlags.None, _rtcpRemoteEndPoint, SendRtcpCallback, _rtcpSocket);
    //            _senderLastSentAt = DateTime.Now;
    //        }
    //        catch (Exception excp)
    //        {
    //            logger.Error("Exception SendRtcpSenderReport. " + excp);
    //        }
    //    }

    //    /// <summary>
    //    /// 发送rtcp回调
    //    /// </summary>
    //    /// <param name="ar"></param>
    //    private void SendRtcpCallback(IAsyncResult ar)
    //    {
    //        try
    //        {
    //            _rtcpSocket.EndSend(ar);
    //        }
    //        catch (Exception ex)
    //        {
    //            logger.Warn("Exception Rtcp", ex);
    //        }
    //    } 
    //    #endregion

    //    #region sip视频请求
    //    /// <summary>
    //    /// 实时视频请求
    //    /// </summary>
    //    public void RealVideoRequest()
    //    {
    //        if (!_sipInited)
    //        {
    //            if (SipStatusHandler != null)
    //            {
    //                SipStatusHandler(SipServiceStatus.Wait);
    //            }
    //            return;
    //        }
    //        _mediaPort = _messageCore.SetMediaPort();

    //        SIPRequest request = InviteRequest();
    //        RealVideo real = new RealVideo()
    //        {
    //            Address = _cameraId,
    //            Variable = VariableType.RealMedia,
    //            Privilege = 90,
    //            Format = "4CIF CIF QCIF 720p 1080p",
    //            Video = "H.264",
    //            Audio = "G.711",
    //            MaxBitrate = 800,
    //            Socket = this.ToString()
    //        };

    //        string xmlBody = RealVideo.Instance.Save<RealVideo>(real);
    //        request.Body = xmlBody;
    //        _m_sipTransport.SendRequest(_remoteEndPoint, request);

    //        //启动RTP通道
    //        _rtpChannel.IsClosed = false;
    //        _rtpChannel.ReservePorts(_mediaPort[0], _mediaPort[1]);
    //        _rtpChannel.Start();
    //    }

    //    /// <summary>
    //    /// 实时视频取消
    //    /// </summary>
    //    public void RealVideoBye()
    //    {
    //        if (!_sipInited)
    //        {
    //            if (SipStatusHandler != null)
    //            {
    //                SipStatusHandler(SipServiceStatus.Wait);
    //            }
    //            return;
    //        }
    //        _rtpChannel.Close();
    //        if (_realReqSession == null)
    //        {
    //            return;
    //        }
    //        SIPRequest req = ByeRequest();
    //        _m_sipTransport.SendRequest(_remoteEndPoint, req);
    //    }

    //    /// <summary>
    //    /// 查询前端设备信息
    //    /// </summary>
    //    /// <param name="cameraId"></param>
    //    public void DeviceQuery(string cameraId)
    //    {
    //        if (!_sipInited)
    //        {
    //            if (SipStatusHandler != null)
    //            {
    //                SipStatusHandler(SipServiceStatus.Wait);
    //            }
    //            return;
    //        }
    //        Device dev = new Device()
    //        {
    //            Privilege = 90,
    //            Variable = VariableType.DeviceInfo
    //        };
    //        SIPRequest req = DeviceReq(cameraId);
    //        string xmlBody = Device.Instance.Save<Device>(dev);
    //        req.Body = xmlBody;
    //        _m_sipTransport.SendRequest(_remoteEndPoint, req);
    //    }



    //    private SIPRequest ByeRequest()
    //    {
    //        SIPURI uri = new SIPURI(_cameraId, _remoteEndPoint.ToHost(), "");
    //        SIPRequest byeRequest = _m_sipTransport.GetRequest(SIPMethodsEnum.BYE, uri);
    //        SIPFromHeader from = new SIPFromHeader(null, _sipRequest.URI, _realReqSession.Header.From.FromTag);
    //        SIPHeader header = new SIPHeader(from, byeRequest.Header.To, _realReqSession.Header.CSeq, _realReqSession.Header.CallId);
    //        header.ContentType = "application/DDCP";
    //        header.Expires = byeRequest.Header.Expires;
    //        header.CSeqMethod = byeRequest.Header.CSeqMethod;
    //        header.Vias = byeRequest.Header.Vias;
    //        header.MaxForwards = byeRequest.Header.MaxForwards;
    //        header.UserAgent = _userAgent;
    //        byeRequest.Header.From = from;
    //        byeRequest.Header = header;
    //        return byeRequest;
    //    }

    //    /// <summary>
    //    /// 前端设备信息请求
    //    /// </summary>
    //    /// <param name="cameraId"></param>
    //    /// <returns></returns>
    //    private SIPRequest DeviceReq(string cameraId)
    //    {
    //        SIPURI remoteUri = new SIPURI(cameraId, _remoteEndPoint.ToHost(), "");
    //        SIPURI localUri = new SIPURI(_sipAccount.LocalSipId, _localEndPoint.ToHost(), "");
    //        SIPFromHeader from = new SIPFromHeader(null, localUri, CallProperties.CreateNewTag());
    //        SIPToHeader to = new SIPToHeader(null, remoteUri, null);
    //        SIPRequest queryReq = _m_sipTransport.GetRequest(SIPMethodsEnum.DO, remoteUri);
    //        queryReq.Header.Contact = null;
    //        queryReq.Header.From = from;
    //        queryReq.Header.Allow = null;
    //        queryReq.Header.To = to;
    //        queryReq.Header.CSeq = CallProperties.CreateNewCSeq();
    //        queryReq.Header.CallId = CallProperties.CreateNewCallId();
    //        queryReq.Header.ContentType = "Application/DDCP";
    //        return queryReq;
    //    }

    //    /// <summary>
    //    /// 查询设备目录请求
    //    /// </summary>
    //    /// <returns></returns>
    //    private SIPRequest QueryItems()
    //    {
    //        SIPURI remoteUri = new SIPURI(_sipAccount.RemoteSipId, _remoteEndPoint.ToHost(), "");
    //        SIPURI localUri = new SIPURI(_sipAccount.LocalSipId, _localEndPoint.ToHost(), "");
    //        SIPFromHeader from = new SIPFromHeader(null, localUri, CallProperties.CreateNewTag());
    //        SIPToHeader to = new SIPToHeader(null, remoteUri, null);
    //        SIPRequest queryReq = _m_sipTransport.GetRequest(SIPMethodsEnum.DO, remoteUri);
    //        queryReq.Header.From = from;
    //        queryReq.Header.Contact = null;
    //        queryReq.Header.Allow = null;
    //        queryReq.Header.To = to;
    //        queryReq.Header.CSeq = CallProperties.CreateNewCSeq();
    //        queryReq.Header.CallId = CallProperties.CreateNewCallId();
    //        queryReq.Header.ContentType = "Application/DDCP";
    //        return queryReq;
    //    }

    //    /// <summary>
    //    /// 监控视频请求
    //    /// </summary>
    //    /// <returns></returns>
    //    private SIPRequest InviteRequest()
    //    {
    //        SIPURI uri = new SIPURI(_cameraId, _remoteEndPoint.ToHost(), "");
    //        SIPRequest inviteRequest = _m_sipTransport.GetRequest(SIPMethodsEnum.INVITE, uri);
    //        SIPFromHeader from = new SIPFromHeader(null, _sipRequest.URI, CallProperties.CreateNewTag());
    //        SIPHeader header = new SIPHeader(from, inviteRequest.Header.To, CallProperties.CreateNewCSeq(), CallProperties.CreateNewCallId());
    //        header.ContentType = "application/DDCP";
    //        header.Expires = inviteRequest.Header.Expires;
    //        header.CSeqMethod = inviteRequest.Header.CSeqMethod;
    //        header.Vias = inviteRequest.Header.Vias;
    //        header.MaxForwards = inviteRequest.Header.MaxForwards;
    //        header.UserAgent = _userAgent;
    //        inviteRequest.Header.From = from;
    //        inviteRequest.Header = header;
    //        _realReqSession = inviteRequest;
    //        return inviteRequest;
    //    }

    //    /// <summary>
    //    /// 确认接收视频请求
    //    /// </summary>
    //    /// <param name="response">响应消息</param>
    //    /// <returns></returns>
    //    private SIPRequest AckRequest(SIPResponse response)
    //    {
    //        SIPURI uri = new SIPURI(response.Header.To.ToURI.User, _remoteEndPoint.ToHost(), "");
    //        SIPRequest ackRequest = _m_sipTransport.GetRequest(SIPMethodsEnum.ACK, uri);
    //        SIPFromHeader from = new SIPFromHeader(null, _sipRequest.URI, response.Header.CallId);
    //        from.FromTag = response.Header.From.FromTag;
    //        SIPHeader header = new SIPHeader(from, response.Header.To, response.Header.CSeq, response.Header.CallId);
    //        header.To.ToTag = null;
    //        header.CSeqMethod = SIPMethodsEnum.ACK;
    //        header.Vias = response.Header.Vias;
    //        header.MaxForwards = response.Header.MaxForwards;
    //        header.ContentLength = response.Header.ContentLength;
    //        header.UserAgent = _userAgent;
    //        header.Allow = null;
    //        ackRequest.Header = header;
    //        return ackRequest;
    //    } 
    //    #endregion

    //    #region 私有方法

    //    /// <summary>
    //    /// 获取远程rtcp终结点(192.168.10.250 UDP 5000)
    //    /// </summary>
    //    /// <param name="socket"></param>
    //    private void GetRemoteRtcp(string socket)
    //    {
    //        string[] split = socket.Split(' ');
    //        if (split.Length < 3)
    //        {
    //            return;
    //        }

    //        try
    //        {
    //            IPAddress remoteIP = _remoteEndPoint.Address;
    //            IPAddress.TryParse(split[0], out remoteIP);
    //            int rtcpPort = _mediaPort[1];
    //            int.TryParse(split[2], out rtcpPort);
    //            _rtcpRemoteEndPoint = new IPEndPoint(remoteIP, rtcpPort + 1);
    //        }
    //        catch (Exception ex)
    //        {
    //            logger.Warn("remote rtp ip/port error", ex);
    //        }
    //    }

    //    public override string ToString()
    //    {
    //        return _localEndPoint.Address.ToString() + " UDP " + _mediaPort[0];
    //    } 
    //    #endregion
    //}
}
