using Logger4Net;
using SIPSorcery.GB28181.Net;
using SIPSorcery.GB28181.Net.RTP;
using SIPSorcery.GB28181.Servers.SIPMonitor;
using SIPSorcery.GB28181.SIP;
using SIPSorcery.GB28181.SIP.App;
using SIPSorcery.GB28181.Sys;
using SIPSorcery.GB28181.Sys.Model;
using SIPSorcery.GB28181.Sys.XML;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
namespace SIPSorcery.GB28181.Servers.SIPMessage
{

    /// <summary>
    /// sip消息核心处理
    /// </summary>
    public class SIPCoreMessageService : ISipCoreService
    {
        #region 私有字段
        private static ILog logger = AppState.logger;
        //  private bool _subscribe = false;
        private int MEDIA_PORT_START = 10000;
        private int MEDIA_PORT_END = 12000;
        private RegistrarCore _registrarCore;
        private SIPAccount _account;
        private ServiceStatus _serviceState;
        private SIPRequest _ackRequest;
        private SIPEndPoint _byeRemoteEP;
        private SIPResponse _audioResponse;
        /// <summary>
        /// SIP远端Host的Socket地址(host:Port)和用户名(GBID)
        /// </summary>
        private readonly Dictionary<string, string> _remoteTransEPs = new Dictionary<string, string>();
        /// <summary>
        /// Monitor Service For all Remote Node
        /// </summary>
        private Dictionary<string, ISIPMonitorService> _nodeMonitorService = new Dictionary<string, ISIPMonitorService>();

        /// <summary>
        /// 本地sip终结点
        /// </summary>
        internal SIPEndPoint LocalEP;
        /// <summary>
        /// sip传输请求
        /// </summary>
        internal SIPTransport Transport;
        /// <summary>
        /// 本地域的sip编码
        /// </summary>
        internal string LocalSIPId;
        public Dictionary<string, ISIPMonitorService> NodeMonitorService => _nodeMonitorService;

        private Stream _g711Stream;
        private Channel _audioChannel;
        private IPEndPoint _audioRemoteEP;
        private AudioPSAnalyze _psAnalyze = new AudioPSAnalyze();
        private Data_Info_s packer = new Data_Info_s();
        private UInt32 src = 0;//算s64CurPts
        private UInt32 timestamp_increse = (UInt32)(90000.0 / 25);

        #endregion

        #region 事件
        /// <summary>
        /// sip服务状态
        /// </summary>
        public event Action<string, ServiceStatus> OnServiceChanged;

        /// <summary>
        /// 录像文件接收
        /// </summary>
        public event Action<RecordInfo> OnRecordInfoReceived;

        /// <summary>
        /// 设备目录接收
        /// </summary>
        public event Action<Catalog> OnCatalogReceived;

        /// <summary>
        /// 设备目录通知
        /// </summary>
        public event Action<NotifyCatalog> OnNotifyCatalogReceived;

        /// <summary>
        /// 语音广播通知
        /// </summary>
        public event Action<VoiceBroadcastNotify> OnVoiceBroadcaseReceived;

        /// <summary>
        /// 报警通知
        /// </summary>
        public event Action<Alarm> OnAlarmReceived;

        /// <summary>
        /// 平台之间心跳接收
        /// </summary>
        public event Action<SIPEndPoint, KeepAlive, string> OnKeepaliveReceived;
        /// <summary>
        /// 设备状态查询接收
        /// </summary>
        public event Action<SIPEndPoint, DeviceStatus> OnDeviceStatusReceived;
        /// <summary>
        /// 设备信息查询接收
        /// </summary>
        public event Action<SIPEndPoint, DeviceInfo> OnDeviceInfoReceived;

        /// <summary>
        /// 设备配置查询接收
        /// </summary>
        public event Action<SIPEndPoint, DeviceConfigDownload> OnDeviceConfigDownloadReceived;
        /// <summary>
        /// 历史媒体发送结束接收
        /// </summary>
        public event Action<SIPEndPoint, MediaStatus> OnMediaStatusReceived;
        /// <summary>
        /// 响应状态码接收
        /// </summary>
        public event Action<SIPResponseStatusCodesEnum, string, SIPEndPoint> OnResponseCodeReceived;

        /// <summary>
        /// 预置位查询接收
        /// </summary>
        public event Action<SIPEndPoint, PresetInfo> OnPresetQueryReceived;

        #endregion

        #region 初始化


        #endregion

        #region 初始化资源，缓存注册上来的设备信息
        public void Initialize(List<CameraInfo> cameraList, SIPAccount account)
        {
            _serviceState = ServiceStatus.Wait;
            _account = account;
            LocalEP = SIPEndPoint.ParseSIPEndPoint("udp:" + account.LocalIP.ToString() + ":" + account.LocalPort);
            LocalSIPId = account.LocalID;
            // init the camera info for connetctions
            cameraList?.ForEach(deviceChannel =>
            {
                var ipaddress = IPAddress.Parse(deviceChannel.IPAddress);
                var remoteEP = new SIPEndPoint(SIPProtocolsEnum.udp, ipaddress, deviceChannel.Port);
                var monitor = new SIPMonitorCore(this, deviceChannel.ChannelID, remoteEP, account);
                _nodeMonitorService.Add(deviceChannel.ChannelID, monitor);
            });
        }

        #endregion

        #region 启动/停止消息主服务(监听注册链接)
        public void Start()
        {
            try
            {
                logger.Debug("SIP Registrar daemon starting...");

                // Configure the SIP transport layer.
                Transport = new SIPTransport(SIPDNSManager.ResolveSIPService, new SIPTransactionEngine(), false)
                {
                    PerformanceMonitorPrefix = SIPSorceryPerformanceMonitor.REGISTRAR_PREFIX,
                    MsgEncode = _account.MsgEncode
                };
                var sipChannels = SIPTransportConfig.ParseSIPChannelsNode(_account.LocalIP, _account.LocalPort, _account.MsgProtocol);
                Transport.AddSIPChannel(sipChannels);

                Transport.SIPTransportRequestReceived += AddMessageRequest;
                Transport.SIPTransportResponseReceived += AddMessageResponse;

                _registrarCore = new RegistrarCore(Transport, true, true, SIPRequestAuthenticator.AuthenticateSIPRequest)
                {
                    _needAuthentication = _account.Authentication
                };

                logger.Debug("SIPRegistrarCore thread started ");
                Task.Factory.StartNew(() => _registrarCore.ProcessRegisterRequest());

                logger.Debug("SIP Registrar successfully started at " + _account.LocalIP + ":" + _account.LocalPort);

            }
            catch (Exception excp)
            {
                logger.Error("Exception SIPRegistrarDaemon Start. " + excp.Message);
            }
        }

        public void Stop()
        {
            foreach (var item in _nodeMonitorService)
            {
                item.Value.Stop();
            }
            LocalEP = null;
            _nodeMonitorService.Clear();
            _nodeMonitorService = null;
            if (_audioChannel != null)
            {
                _audioChannel.Stop();
            }

            try
            {
                logger.Debug("SIP Registrar daemon stopping...");
                logger.Debug("Shutting down SIP Transport.");

                Transport.Shutdown();
                Transport = null;

                logger.Debug("sip message service stopped.");
                logger.Debug("SIP Registrar daemon stopped.");
            }
            catch (Exception excp)
            {
                logger.Error("Exception SIPRegistrarDaemon Stop. " + excp.Message);
            }
        }
        #endregion

        #region 消息处理
        /// <summary>
        /// sip请求消息
        /// </summary>
        /// <param name="localEndPoint">本地终结点</param>
        /// <param name="remoteEndPoint"b>远程终结点</param>
        /// <param name="request">sip请求</param>
        public void AddMessageRequest(SIPEndPoint localEP, SIPEndPoint remoteEP, SIPRequest request)
        {
            switch (request.Method)
            {
                case SIPMethodsEnum.REGISTER:
                    RegisterHandle(localEP, remoteEP, request);
                    break;
                case SIPMethodsEnum.MESSAGE:
                    MessageHandle(localEP, remoteEP, request);
                    break;
                case SIPMethodsEnum.NOTIFY:
                    NotifyHandle(localEP, remoteEP, request);
                    break;
                case SIPMethodsEnum.BYE:
                    SendResponse(localEP, remoteEP, request);
                    break;
                case SIPMethodsEnum.NONE:
                    break;
                case SIPMethodsEnum.UNKNOWN:
                    break;
                case SIPMethodsEnum.INVITE:
                    InviteHandle(localEP, remoteEP, request);
                    break;
                case SIPMethodsEnum.ACK:
                    AckHandle(localEP, remoteEP, request);
                    break;
                case SIPMethodsEnum.CANCEL:
                    break;
                case SIPMethodsEnum.OPTIONS:
                    break;
                case SIPMethodsEnum.INFO:
                    break;
                case SIPMethodsEnum.SUBSCRIBE:
                    break;
                case SIPMethodsEnum.PUBLISH:
                    break;
                case SIPMethodsEnum.PING:
                    break;
                case SIPMethodsEnum.REFER:
                    break;
                case SIPMethodsEnum.PRACK:
                    break;
                case SIPMethodsEnum.UPDATE:
                    break;
                default:
                    break;
            }
        }

        #region 音频请求处理

        /// <summary>
        ///  Invite请求消息
        /// </summary>
        /// <param name="localEP"></param>
        /// <param name="remoteEP"></param>
        /// <param name="request"></param>
        private void InviteHandle(SIPEndPoint localEP, SIPEndPoint remoteEP, SIPRequest request)
        {
            _byeRemoteEP = remoteEP;
            int[] port = SetMediaPort();
            var trying = GetResponse(localEP, remoteEP, SIPResponseStatusCodesEnum.Trying, "", request);
            Transport.SendResponse(trying);
            //Thread.Sleep(200);
            SIPResponse audioOK = GetResponse(localEP, remoteEP, SIPResponseStatusCodesEnum.Ok, "", request);
            audioOK.Header.ContentType = "application/sdp";
            SIPURI localUri = new SIPURI(LocalSIPId, LocalEP.ToHost(), "");
            SIPContactHeader contact = new SIPContactHeader(null, localUri);
            audioOK.Header.Contact = new List<SIPContactHeader>
            {
                contact
            };
            //SDP
            audioOK.Body = SetMediaAudio(localEP.Address.ToString(), port[0], request.URI.User);
            _audioResponse = audioOK;
            Transport.SendResponse(audioOK);
            int recvPort = GetReceivePort(request.Body, SDPMediaTypesEnum.audio);
            string ip = GetReceiveIP(request.Body);
            _audioRemoteEP = new IPEndPoint(IPAddress.Parse(ip), recvPort);
            if (_audioChannel == null)
            {
                _audioChannel = new UDPChannel(TcpConnectMode.active, IPAddress.Any, port, ProtocolType.Udp, false, recvPort);
            }
            _audioChannel.Start();
        }

        public void AckHandle(SIPEndPoint localEP, SIPEndPoint remoteEP, SIPRequest request)
        {
            _ackRequest = request;
            if (_g711Stream == null)
            {
                _g711Stream = new FileStream("D:\\audio.g711", FileMode.Open, FileAccess.Read, FileShare.Read);
            }
            new Thread(new ThreadStart(SendRtp)).Start();
        }

        private void SendRtp()
        {
            packer.sendSOcket = _audioChannel.SendSocket;
            packer.IFrame = 0;
            packer.u32Ssrc = 0x0857;
            packer.s64CurPts = src;
            packer.remotePoint = _audioRemoteEP;
            int i = 0;
            long offset = 240;
            while (_g711Stream.Length > i)
            {
                byte[] buffer = new byte[240];
                _g711Stream.Read(buffer, 0, buffer.Length);
                _psAnalyze.Gb28181_streampackageForH264(buffer, buffer.Length, packer, 1);
                _g711Stream.Seek(offset, SeekOrigin.Begin);
                Thread.Sleep(5);
                i += buffer.Length;
                offset += buffer.Length;
                src += timestamp_increse;
            }
            ByeVideoReq();
        }

        public void ByeVideoReq()
        {
            if (_audioChannel == null)
            {
                return;
            }

            _audioChannel.Stop();
            _audioChannel = null;
            SIPURI localUri = new SIPURI(_account.SIPPassword, LocalEP.ToHost(), "");
            SIPURI remoteUri = new SIPURI(_ackRequest.Header.From.FromURI.User, _byeRemoteEP.ToHost(), "");
            SIPFromHeader from = new SIPFromHeader(null, localUri, _ackRequest.Header.To.ToTag);
            SIPToHeader to = new SIPToHeader(null, remoteUri, _ackRequest.Header.From.FromTag);
            SIPRequest byeReq = new SIPRequest(SIPMethodsEnum.BYE, localUri);
            SIPHeader header = new SIPHeader(from, to, _ackRequest.Header.CSeq + 1, _ackRequest.Header.CallId)
            {
                CSeqMethod = SIPMethodsEnum.BYE
            };

            SIPViaHeader viaHeader = new SIPViaHeader(LocalEP, CallProperties.CreateBranchId())
            {
                Branch = CallProperties.CreateBranchId(),
                Transport = SIPProtocolsEnum.udp
            };
            SIPViaSet viaSet = new SIPViaSet
            {
                Via = new List<SIPViaHeader>()
            };
            viaSet.Via.Add(viaHeader);
            header.Vias = viaSet;

            header.UserAgent = SIPConstants.SIP_USERAGENT_STRING;
            header.Contact = _ackRequest.Header.Contact;
            byeReq.Header = header;
            SendRequest(_byeRemoteEP, byeReq);
        }



        private byte[] RtpG711Packet(byte[] buffer, ushort seq, uint timestamp)
        {
            RTPHeader header = new RTPHeader
            {
                Version = 2,
                PaddingFlag = 0,
                HeaderExtensionFlag = 0,
                CSRCCount = 0,
                MarkerBit = 1,
                PayloadType = 0x88,
                SequenceNumber = seq,
                Timestamp = timestamp,
                SyncSource = 0x0857,
            };

            RTPPacket packet = new RTPPacket
            {
                Header = header,
                Payload = buffer
            };
            byte[] newBuffer = new byte[12 + buffer.Length];
            Buffer.BlockCopy(header.GetBytes(), 0, newBuffer, 0, 12);
            Buffer.BlockCopy(buffer, 0, newBuffer, 12, buffer.Length);
            return newBuffer;
        }

        /// <summary>
        /// 设置媒体参数请求(实时)
        /// </summary>
        /// <param name="localIp">本地ip</param>
        /// <param name="mediaPort">rtp/rtcp媒体端口(10000/10001)</param>
        /// <returns></returns>
        private string SetMediaAudio(string localIp, int port, string audioId)
        {
            SDPConnectionInformation sdpConn = new SDPConnectionInformation(localIp);

            SDP sdp = new SDP
            {
                Version = 0,
                SessionId = "0",
                Username = audioId,
                SessionName = CommandType.Play.ToString(),
                Connection = sdpConn,
                Timing = "0 0",
                Address = localIp
            };

            SDPMediaFormat psFormat = new SDPMediaFormat(SDPMediaFormatsEnum.PS)
            {
                IsStandardAttribute = false
            };

            SDPMediaAnnouncement media = new SDPMediaAnnouncement
            {
                Media = SDPMediaTypesEnum.audio
            };

            media.MediaFormats.Add(psFormat);
            media.AddExtra("a=sendonly");
            media.AddExtra("y=0100000002");
            //media.AddExtra("f=v/////a/1/8/1");
            media.AddFormatParameterAttribute(psFormat.FormatID, psFormat.Name);
            media.Port = port;

            sdp.Media.Add(media);

            return sdp.ToString();
        }
        #endregion

        /// <summary>
        /// SIP响应消息
        /// </summary>
        /// <param name="localEP">本地终结点</param>
        /// <param name="remoteEP">远程终结点</param>
        /// <param name="response">sip响应</param>
        public void AddMessageResponse(SIPEndPoint localEP, SIPEndPoint remoteEP, SIPResponse response)
        {
            if (response.Status == SIPResponseStatusCodesEnum.Ok)
            {
                if (response.Header.CSeqMethod == SIPMethodsEnum.SUBSCRIBE)
                {
                    _nodeMonitorService[response.Header.To.ToURI.User].Subscribe(response);
                }
                else if (response.Header.ContentType.ToLower() == "application/sdp")
                {

                    CommandType cmdType = CommandType.Play;
                    string sessionName = GetSessionName(response.Body);
                    if (sessionName != null)
                    {
                        Enum.TryParse(sessionName, out cmdType);
                    }

                    if (cmdType == CommandType.Download)
                    {
                        cmdType = CommandType.Playback;
                    }
                    lock (_nodeMonitorService)
                    {
                        string ip = GetReceiveIP(response.Body);
                        int port = GetReceivePort(response.Body, SDPMediaTypesEnum.video);
                        _nodeMonitorService[response.Header.To.ToURI.User].AckRequest(response.Header.To.ToTag, ip, port);

                    }
                }

                OnResponseCodeReceived?.Invoke(response.Status, "对方国标平台返回状态【成功】", remoteEP);
            }
            else
            {
                switch (response.Status)
                {
                    case SIPResponseStatusCodesEnum.BadRequest:             //请求失败
                    case SIPResponseStatusCodesEnum.InternalServerError:    //服务器内部错误
                    case SIPResponseStatusCodesEnum.RequestTerminated:      //请求终止
                    case SIPResponseStatusCodesEnum.CallLegTransactionDoesNotExist: //呼叫/事务不存在
                        OnResponseCodeReceived(response.Status, "对方国标平台返回状态【失败】", remoteEP);
                        break;
                    case SIPResponseStatusCodesEnum.Trying:                 //等待处理
                        OnResponseCodeReceived(response.Status, "对方国标平台返回状态【正在尝试】", remoteEP);
                        break;
                    default:
                        OnResponseCodeReceived(response.Status, "对方国标平台返回状态【其他】", remoteEP);
                        break;
                }
            }
        }

        public void OnSIPServiceChange(string msg, ServiceStatus state)
        {
            _serviceState = ServiceStatus.Complete;

            try { OnServiceChanged.Invoke(msg, state); }
            catch { }
            //OnServiceChanged?.GetInvocationList().ToList().ForEach((item) =>
            //{
            //    var handler = item as Action<string, ServiceStatus>;
            //    try { handler?.Invoke(msg, state); }
            //    catch { }
            //});

            //foreach (Action<string, ServiceStatus> handler in OnServiceChanged.GetInvocationList())
            //{
            //    try { OnServiceChanged(msg, state); }
            //    catch { continue; }
            //}
        }

        /// <summary>
        /// 注册消息处理
        /// </summary>
        /// <param name="localEP">本地终结点</param>
        /// <param name="remoteEP">远程终结点</param>
        /// <param name="request">sip请求</param>
        private void RegisterHandle(SIPEndPoint localEP, SIPEndPoint remoteEP, SIPRequest request)
        {
            OnSIPServiceChange(remoteEP.ToHost(), ServiceStatus.Complete);
            lock (_remoteTransEPs)
            {
                if (!_remoteTransEPs.ContainsKey(remoteEP.ToHost()))
                {
                    _remoteTransEPs.Add(remoteEP.ToHost(), request.Header.From.FromURI.User);
                }
            }
            _registrarCore.AddRegisterRequest(localEP, remoteEP, request);
        }

        /// <summary>
        /// 目录查询响应消息处理
        /// </summary>
        /// <param name="localEP">本地终结点</param>
        /// <param name="remoteEP">远程终结点</param>
        /// <param name="request">sip请求</param>
        /// <param name="catalog">目录结构体</param>
        private void CatalogHandle(SIPEndPoint localEP, SIPEndPoint remoteEP, SIPRequest request, Catalog catalog)
        {
            foreach (var cata in catalog.DeviceList.Items)
            {
                if (cata == null)
                {
                    continue;
                }
                cata.RemoteEP = remoteEP.ToHost();
                DevCataType devCata = DevType.GetCataType(cata.DeviceID);
                if (devCata != DevCataType.Device)
                {
                    continue;
                }
                for (int i = 0; i < 2; i++)
                {
                    //   CommandType cmdType = i == 0 ? CommandType.Play : CommandType.Playback;

                    lock (_nodeMonitorService)
                    {
                        if (_nodeMonitorService.ContainsKey(cata.DeviceID))
                        {
                            continue;
                        }
                        remoteEP.Port = _account.KeepaliveInterval;
                        _nodeMonitorService.Add(cata.DeviceID, new SIPMonitorCore(this, cata.DeviceID, remoteEP, _account));
                    }
                }
            }

            OnCatalogReceived?.Invoke(catalog);
        }

        /// <summary>
        /// 心跳消息处理
        /// </summary>
        /// <param name="localEP">本地终结点</param>
        /// <param name="remoteEP">远程终结点</param>
        /// <param name="request">sip请求</param>
        /// <param name="keepAlive">心跳结构体</param>
        private void KeepaliveHandle(SIPEndPoint localEP, SIPEndPoint remoteEP, SIPRequest request, KeepAlive keepAlive)
        {
            //SendResponse(localEP, remoteEP, request);
            OnSIPServiceChange(remoteEP.ToHost(), ServiceStatus.Complete);
            lock (_remoteTransEPs)
            {
                if (!_remoteTransEPs.ContainsKey(remoteEP.ToHost()))
                {
                    _remoteTransEPs.Add(remoteEP.ToHost(), request.Header.From.FromURI.User);
                }
            }
            //if (!_subscribe)
            //{
            //    DeviceCatalogSubscribe(remoteEP, request.Header.From.FromURI.User);
            //}
            OnKeepaliveReceived?.Invoke(remoteEP, keepAlive, request.Header.From.FromURI.User);
            // _subscribe = true;
            logger.Debug("KeepAlive:" + remoteEP.ToHost() + "=====DevID:" + keepAlive.DeviceID + "=====Status:" + keepAlive.Status + "=====SN:" + keepAlive.SN);
        }

        /// <summary>
        /// 录像查询消息处理
        /// </summary>
        /// <param name="localSIPEndPoint">本地终结点</param>
        /// <param name="remoteEndPoint">远程终结点</param>
        /// <param name="response">sip响应</param>
        /// <param name="record">录像xml结构体</param>
        private void RecordInfoHandle(SIPEndPoint localEP, SIPEndPoint remoteEP, SIPRequest request, RecordInfo record)
        {

            _nodeMonitorService[record.DeviceID].RecordQueryTotal(record.SumNum);
            if (OnRecordInfoReceived != null && record.RecordItems != null)
            {
                OnRecordInfoReceived(record);
            }
        }

        /// <summary>
        /// Message消息处理
        /// </summary>
        /// <param name="localEP">本地终结点</param>
        /// <param name="remoteEP">远程终结点</param>
        /// <param name="request">sip请求</param>
        private void MessageHandle(SIPEndPoint localEP, SIPEndPoint remoteEP, SIPRequest request)
        {
            SendResponse(localEP, remoteEP, request);

            //心跳消息
            KeepAlive keepAlive = KeepAlive.Instance.Read(request.Body);
            if (keepAlive != null && keepAlive.CmdType == CommandType.Keepalive)
            {
                KeepaliveHandle(localEP, remoteEP, request, keepAlive);
                return;
            }


            //设备目录
            Catalog catalog = Catalog.Instance.Read(request.Body);
            if (catalog != null && catalog.CmdType == CommandType.Catalog)
            {
                CatalogHandle(localEP, remoteEP, request, catalog);
                return;
            }

            //录像查询
            RecordInfo record = RecordInfo.Instance.Read(request.Body);
            if (record != null && record.CmdType == CommandType.RecordInfo)
            {
                RecordInfoHandle(localEP, remoteEP, request, record);
                return;
            }

            //媒体通知
            MediaStatus mediaStatus = MediaStatus.Instance.Read(request.Body);
            if (mediaStatus != null && mediaStatus.CmdType == CommandType.MediaStatus)
            {
                _nodeMonitorService[request.Header.From.FromURI.User].ByeVideoReq();
                //取值121表示历史媒体文件发送结束（回放结束/下载结束）
                //NotifyType未找到相关文档标明所有该类型值，暂时只处理121
                if (mediaStatus.NotifyType.Equals("121"))
                {
                    OnMediaStatusReceived?.Invoke(remoteEP, mediaStatus);
                }
                return;
            }

            //设备状态查询
            DeviceStatus deviceStatus = DeviceStatus.Instance.Read(request.Body);
            if (deviceStatus != null && deviceStatus.CmdType == CommandType.DeviceStatus)
            {
                OnDeviceStatusReceived?.Invoke(remoteEP, deviceStatus);
                return;
            }

            //设备信息查询
            DeviceInfo deviceInfo = DeviceInfo.Instance.Read(request.Body);
            if (deviceInfo != null && deviceInfo.CmdType == CommandType.DeviceInfo)
            {
                OnDeviceInfoReceived?.Invoke(remoteEP, deviceInfo);
                return;
            }

            //设备配置查询
            DeviceConfigDownload devDownload = DeviceConfigDownload.Instance.Read(request.Body);
            if (devDownload != null && devDownload.CmdType == CommandType.ConfigDownload)
            {
                OnDeviceConfigDownloadReceived?.Invoke(remoteEP, devDownload);
            }

            //预置位查询
            PresetInfo preset = PresetInfo.Instance.Read(request.Body);
            if (preset != null && preset.CmdType == CommandType.PresetQuery)
            {
                OnPresetQueryReceived?.Invoke(remoteEP, preset);
            }

            //报警通知
            Alarm alarm = Alarm.Instance.Read(request.Body);
            if (alarm != null && alarm.CmdType == CommandType.Alarm)//单兵上报经纬度
            {
                OnAlarmReceived?.Invoke(alarm);
            }
        }

        /// <summary>
        /// 目录订阅通知消息处理
        /// </summary>
        /// <param name="localEP">本地终结点</param>
        /// <param name="remoteEP">远程终结点</param>
        /// <param name="request">sip请求</param>
        private void NotifyHandle(SIPEndPoint localEP, SIPEndPoint remoteEP, SIPRequest request)
        {
            SendResponse(localEP, remoteEP, request);
            //目录推送通知
            NotifyCatalog notify = NotifyCatalog.Instance.Read(request.Body);
            if (notify != null && notify.CmdType == CommandType.Catalog)  //设备目录
            {
                OnNotifyCatalogReceived?.Invoke(notify);
            }

            //语音广播通知
            //移动设备位置数据通知
            VoiceBroadcastNotify voice = VoiceBroadcastNotify.Instance.Read(request.Body);
            if (voice != null && voice.CmdType == CommandType.Broadcast)    //语音广播
            {
                OnVoiceBroadcaseReceived?.Invoke(voice);
            }


        }

        /// <summary>
        /// 发送sip响应消息
        /// </summary>
        /// <param name="localEP">本地终结点</param>
        /// <param name="remoteEP">远程终结点</param>
        /// <param name="request">sip请求</param>
        private void SendResponse(SIPEndPoint localEP, SIPEndPoint remoteEP, SIPRequest request)
        {
            if (_serviceState == ServiceStatus.Wait)
            {
                OnSIPServiceChange(remoteEP.ToHost(), ServiceStatus.Wait);
                return;
            }
            SIPResponse res = GetResponse(localEP, remoteEP, SIPResponseStatusCodesEnum.Ok, "", request);
            Transport.SendResponse(res);
        }

        /// <summary>
        /// 发送sip请求消息
        /// </summary>
        /// <param name="remoteEP">远程终结点</param>
        /// <param name="request">sip请求</param>
        public void SendRequest(SIPEndPoint remoteEP, SIPRequest request)
        {
            if (_serviceState == ServiceStatus.Wait)
            {
                OnSIPServiceChange(remoteEP.ToHost(), ServiceStatus.Wait);
                return;
            }
            Transport.SendRequest(remoteEP, request);
        }

        /// <summary>
        /// 发送可靠的sip请求消息
        /// </summary>
        /// <param name="remoteEP">远程终结点</param>
        /// <param name="request">sip请求消息</param>
        public void SendReliableRequest(SIPEndPoint remoteEP, SIPRequest request)
        {
            if (_serviceState == ServiceStatus.Wait)
            {
                OnSIPServiceChange(remoteEP.ToHost(), ServiceStatus.Wait);
                return;
            }
            var trans = Transport.CreateUASTransaction(request, remoteEP, LocalEP, null);
            trans.TransactionStateChanged += Trans_TransactionStateChanged;
            trans.SendReliableRequest();
        }

        private void Trans_TransactionStateChanged(SIPTransaction transaction)
        {
            AddMessageResponse(transaction.LocalSIPEndPoint, transaction.RemoteEndPoint, transaction.TransactionFinalResponse);
        }
        #endregion

        #region SDP消息处理

        private string GetReceiveIP(string sdpStr)
        {
            string[] sdpLines = sdpStr.Split('\n');

            var targetLine = sdpLines.FirstOrDefault(line => line.Trim().StartsWith("c="));

            if (targetLine != null)
            {
                var conn = SDPConnectionInformation.ParseConnectionInformation(targetLine);
                return conn.ConnectionAddress;
            }
            return null;
        }

        /// <summary>
        /// 获取接收方端口号码
        /// </summary>
        /// <param name="sdpStr">SDP</param>
        /// <returns></returns>
        private int GetReceivePort(string sdpStr, SDPMediaTypesEnum mediaType)
        {
            string[] sdpLines = sdpStr.Split('\n');

            foreach (var line in sdpLines)
            {
                if (line.Trim().StartsWith("m="))
                {
                    Match mediaMatch = Regex.Match(line.Substring(2).Trim(), @"(?<type>\w+)\s+(?<port>\d+)\s+(?<transport>\S+)\s+(?<formats>.*)$");
                    if (mediaMatch.Success)
                    {
                        SDPMediaAnnouncement announcement = new SDPMediaAnnouncement
                        {
                            Media = SDPMediaTypes.GetSDPMediaType(mediaMatch.Result("${type}"))
                        };
                        Int32.TryParse(mediaMatch.Result("${port}"), out announcement.Port);
                        announcement.Transport = mediaMatch.Result("${transport}");
                        announcement.ParseMediaFormats(mediaMatch.Result("${formats}"));
                        if (announcement.Media != mediaType)
                        {
                            continue;
                        }
                        return announcement.Port;
                    }
                }
            }
            return 0;
        }
        /// <summary>
        /// 获取SDP协议中SessionName字段值
        /// </summary>
        /// <param name="body">SDP文本</param>
        /// <returns></returns>
        private string GetSessionName(string body)
        {
            body = body.Replace("\r", "");
            string[] text = body.Split('\n');//
            foreach (string item in text)
            {
                string[] values = item.Split('=');
                if (values.Contains("s"))
                {
                    if (values[1] == "Play" || values[1] == "Playback" || values[1] == "Embedded IPC" || values[1] == "Download")
                    {
                        return values[1].Replace(" ", "");
                    }
                }
                else if (values.Contains("t"))
                {
                    if (values[1] == "0 0")
                    {
                        return CommandType.Play.ToString();
                    }
                    else
                    {
                        return CommandType.Playback.ToString();
                    }
                }
            }
            return null;
        }
        #endregion

        #region 响应消息
        private SIPResponse GetResponse(SIPEndPoint localSIPEndPoint, SIPEndPoint remoteEndPoint, SIPResponseStatusCodesEnum responseCode, string reasonPhrase, SIPRequest request)
        {
            try
            {
                SIPResponse response = new SIPResponse(responseCode, reasonPhrase, localSIPEndPoint);
                SIPSchemesEnum sipScheme = (localSIPEndPoint.Protocol == SIPProtocolsEnum.tls) ? SIPSchemesEnum.sips : SIPSchemesEnum.sip;
                SIPFromHeader from = request.Header.From;
                from.FromTag = request.Header.From.FromTag;
                SIPToHeader to = request.Header.To;
                response.Header = new SIPHeader(from, to, request.Header.CSeq, request.Header.CallId)
                {
                    CSeqMethod = request.Header.CSeqMethod,
                    Vias = request.Header.Vias,
                    UserAgent = SIPConstants.SIP_USERAGENT_STRING,
                    CSeq = request.Header.CSeq
                };

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
        #endregion

        #region 目录查询/订阅
        /// <summary>
        /// 设备目录查询
        /// </summary>
        /// <param name="deviceId">目的设备编码</param>
        public void DeviceCatalogQuery()
        {
            lock (_remoteTransEPs)
            {
                foreach (var item in _remoteTransEPs)
                {
                    SIPEndPoint remoteEP = SIPEndPoint.ParseSIPEndPoint("udp:" + item.Key);
                    SIPRequest catalogReq = QueryItems(remoteEP, item.Value);
                    CatalogQuery catalog = new CatalogQuery()
                    {
                        CommandType = CommandType.Catalog,
                        DeviceID = item.Value,
                        SN = new Random().Next(1, ushort.MaxValue)
                    };
                    string xmlBody = CatalogQuery.Instance.Save<CatalogQuery>(catalog);
                    catalogReq.Body = xmlBody;
                    SendRequest(remoteEP, catalogReq);
                }
            }
        }

        /// <summary>
        /// 查询设备目录请求
        /// </summary>
        /// <returns></returns>
        private SIPRequest QueryItems(SIPEndPoint remoteEndPoint, string remoteSIPId)
        {
            string fromTag = CallProperties.CreateNewTag();
            string toTag = CallProperties.CreateNewTag();
            int cSeq = CallProperties.CreateNewCSeq();
            string callId = CallProperties.CreateNewCallId();

            SIPURI remoteUri = new SIPURI(remoteSIPId, remoteEndPoint.ToHost(), "");
            SIPURI localUri = new SIPURI(LocalSIPId, LocalEP.ToHost(), "");
            SIPFromHeader from = new SIPFromHeader(null, localUri, fromTag);
            SIPToHeader to = new SIPToHeader(null, remoteUri, null);
            SIPRequest catalogReq = Transport.GetRequest(SIPMethodsEnum.MESSAGE, remoteUri);
            catalogReq.Header.From = from;
            catalogReq.Header.Contact = null;
            catalogReq.Header.Allow = null;
            catalogReq.Header.To = to;
            catalogReq.Header.UserAgent = SIPConstants.SIP_USERAGENT_STRING;
            catalogReq.Header.CSeq = cSeq;
            catalogReq.Header.CallId = callId;
            catalogReq.Header.ContentType = "application/MANSCDP+xml";

            return catalogReq;
        }

        /// <summary>
        /// 目录订阅
        /// </summary>
        /// <param name="deviceId">目的设备编码</param>
        public void DeviceCatalogSubscribe(SIPEndPoint remoteEP, string remoteID)
        {
            SIPRequest catalogReq = SubscribeCatalog(remoteEP, remoteID);
            CatalogQuery catalog = new CatalogQuery()
            {
                CommandType = CommandType.Catalog,
                DeviceID = remoteID,
                SN = new Random().Next(1, ushort.MaxValue)
                //StartTime = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss"),
                //EndTime = DateTime.Now.AddYears(1).ToString("yyyy-MM-ddTHH:mm:ss")
            };
            string xmlBody = CatalogQuery.Instance.Save<CatalogQuery>(catalog);
            catalogReq.Body = xmlBody;
            SendRequest(remoteEP, catalogReq);
        }

        /// <summary>
        /// 目录订阅请求
        /// </summary>
        /// <returns></returns>
        private SIPRequest SubscribeCatalog(SIPEndPoint remoteEndPoint, string remoteSIPId)
        {
            string fromTag = CallProperties.CreateNewTag();
            int cSeq = CallProperties.CreateNewCSeq();
            string callId = CallProperties.CreateNewCallId();

            SIPURI remoteUri = new SIPURI(remoteSIPId, remoteEndPoint.ToHost(), "");
            SIPURI localUri = new SIPURI(LocalSIPId, LocalEP.ToHost(), "");
            SIPFromHeader from = new SIPFromHeader(null, localUri, fromTag);
            SIPToHeader to = new SIPToHeader(null, remoteUri, null);
            SIPRequest catalogReq = Transport.GetRequest(SIPMethodsEnum.SUBSCRIBE, remoteUri);

            catalogReq.Header.From = from;
            //catalogReq.Header.Contact = null;
            catalogReq.Header.Allow = null;
            catalogReq.Header.To = to;
            catalogReq.Header.UserAgent = SIPConstants.SIP_USERAGENT_STRING;
            catalogReq.Header.Event = "Catalog";//"Catalog;id=1894";
            catalogReq.Header.Expires = 600;
            catalogReq.Header.CSeq = cSeq;
            catalogReq.Header.CallId = callId;
            catalogReq.Header.ContentType = "application/MANSCDP+xml";

            return catalogReq;
        }
        #endregion

        #region 移动设备位置数据订阅

        public void MobileDataSubscription(string devID)
        {
            lock (_remoteTransEPs)
            {
                foreach (var item in _remoteTransEPs)
                {
                    SIPEndPoint remoteEP = SIPEndPoint.ParseSIPEndPoint("udp:" + item.Key);
                    SIPRequest eventSubscribeReq = QueryItems(remoteEP, devID);
                    //eventSubscribeReq.Method = SIPMethodsEnum.SUBSCRIBE;
                    CatalogQuery catalog = new CatalogQuery()
                    {
                        CommandType = CommandType.MobilePosition,
                        DeviceID = devID,
                        SN = new Random().Next(1, ushort.MaxValue)
                    };
                    string xmlBody = CatalogQuery.Instance.Save<CatalogQuery>(catalog);
                    eventSubscribeReq.Body = xmlBody;
                    SendRequest(remoteEP, eventSubscribeReq);
                }
            }
        }

        #endregion

        #region 设置媒体流端口号
        /// <summary>
        /// 设置媒体(rtp/rtcp)端口号
        /// </summary>
        /// <returns></returns>
        public int[] SetMediaPort()
        {
            //int[] port = new int[] {10000,10001 };
            //return port;
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
        #endregion
    }
}
