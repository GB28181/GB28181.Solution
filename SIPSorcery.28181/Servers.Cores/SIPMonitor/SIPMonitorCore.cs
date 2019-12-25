using Logger4Net;
using SIPSorcery.GB28181.Net;
using SIPSorcery.GB28181.Servers.SIPMessage;
using SIPSorcery.GB28181.SIP;
using SIPSorcery.GB28181.SIP.App;
using SIPSorcery.GB28181.Sys;
using SIPSorcery.GB28181.Sys.Config;
using SIPSorcery.GB28181.Sys.XML;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading;

namespace SIPSorcery.GB28181.Servers.SIPMonitor
{
    /// <summary>
    /// sip监控核心服务，每一个接入节点都有一个监控服务实例
    /// </summary>
    public class SIPMonitorCoreService : ISIPMonitorCore
    {
        #region 私有字段
        private static ILog logger = AppState.logger;
        private AutoResetEvent _autoResetEventForRpc = new AutoResetEvent(false);

        //concurent requet/reply
        private ConcurrentDictionary<string, SIPRequest> _syncRequestContext = new ConcurrentDictionary<string, SIPRequest>();
        private ConcurrentQueue<Tuple<SIPRequest, SIPResponse>> _syncResponseContext = new ConcurrentQueue<Tuple<SIPRequest, SIPResponse>>();
        private ISipMessageCore _sipMsgCoreService;
        /// <summary>
        /// 远程终结点
        /// </summary>
        public SIPEndPoint RemoteEndPoint { get; set; }
        /// <summary>
        /// rtp数据通道
        /// </summary>
        // RTP wil be established from other place 
        //  private Channel _channel;
        public string DeviceId { get; set; }

        private SIPRequest _reqSession;
        private int[] _mediaPort;
        //  private SIPContactHeader _contact;
        private string _toTag;
        //  private SIPViaSet _via;
        private int _recordTotal = -1;
        //   private DevStatus _Status;
        private SIPAccount _sipAccount;

        private ISIPTransport _sipTransport;

        #endregion

        #region 事件回调
        /// <summary>
        /// 视频流回调
        /// </summary>
        // public event Action<RTPFrame> OnStreamReady;
        #endregion

        #region 初始化监控
        //public SIPMonitorCoreService(ISipMessageCore sipMsgCoreService, ISIPTransport sipTransport, ISipAccountStorage sipAccountStorage)
        public SIPMonitorCoreService(ISipMessageCore sipMessageCore, ISIPTransport sipTransport, ISipAccountStorage sipAccountStorage)
        {
            _sipMsgCoreService = sipMessageCore;
            _sipTransport = sipTransport;
            _sipAccount = sipAccountStorage.GetLocalSipAccout();
        }

        #endregion

        #region 实时视频
        /// <summary>
        /// 实时视频请求
        /// </summary>
        /// <param name="mediaPort">接收Port</param>
        /// <param name="receiveIP">接收IP</param>
        public int RealVideoReq(int[] mediaPort, string receiveIP, bool needResult = false)
        {
            _mediaPort = mediaPort;
            //  _mediaPort = _msgCore.SetMediaPort();
            //  string localIp = _msgCore.LocalEP.Address.ToString();
            string fromTag = CallProperties.CreateNewTag();
            int cSeq = CallProperties.CreateNewCSeq();
            string callId = CallProperties.CreateNewCallId();
            logger.Debug("RealVideoReq: DeviceId=" + DeviceId);
            SIPURI remoteUri = new SIPURI(DeviceId, RemoteEndPoint.ToHost(), "");
            SIPURI localUri = new SIPURI(_sipMsgCoreService.LocalSIPId, _sipMsgCoreService.LocalEP.ToHost(), "");
            SIPFromHeader from = new SIPFromHeader(null, localUri, fromTag);
            SIPToHeader to = new SIPToHeader(null, remoteUri, null);
            SIPRequest sipRequest = _sipTransport.GetRequest(SIPMethodsEnum.INVITE, remoteUri);
            var contactHeader = new SIPContactHeader(null, localUri);
            sipRequest.Header.Contact.Clear();
            sipRequest.Header.Contact.Add(contactHeader);

            sipRequest.Header.Allow = null;
            sipRequest.Header.From = from;
            sipRequest.Header.To = to;
            sipRequest.Header.UserAgent = SIPConstants.SIP_USERAGENT_STRING;
            sipRequest.Header.CSeq = cSeq;
            sipRequest.Header.CallId = callId;
            sipRequest.Header.Subject = SetSubject();
            sipRequest.Header.ContentType = SIPHeader.ContentTypes.Application_SDP;
            sipRequest.Body = SetMediaReq(receiveIP, mediaPort);
            _sipMsgCoreService.SendReliableRequest(RemoteEndPoint, sipRequest);
            _reqSession = sipRequest;
            if (needResult)
            {
                _syncRequestContext.TryAdd(callId, sipRequest);
            }
            return cSeq;
        }



        /// <summary>
        /// 确认接收视频请求
        /// </summary>
        /// <param name="toTag">toTag</param>
        /// <returns></returns>
        //   public void AckRequest(string toTag, string ip, int port)
        public void AckRequest(SIPResponse response)
        {
            SIPURI localUri = new SIPURI(_sipMsgCoreService.LocalSIPId, _sipMsgCoreService.LocalEP.ToHost(), "");
            SIPURI remoteUri = new SIPURI(DeviceId, RemoteEndPoint.ToHost(), "");
            SIPRequest ackReq = _sipTransport.GetRequest(SIPMethodsEnum.ACK, remoteUri);
            SIPFromHeader from = _reqSession.Header.From;
            SIPToHeader to = _reqSession.Header.To;
            to.ToTag = response.Header.To.ToTag;
            var header = new SIPHeader(from, to, _reqSession.Header.CSeq, _reqSession.Header.CallId)
            {
                CSeqMethod = SIPMethodsEnum.ACK,
                Contact = _reqSession.Header.Contact,
                Vias = _reqSession.Header.Vias,
                UserAgent = SIPConstants.SIP_USERAGENT_STRING,
                Allow = null
            };
            ackReq.Header = header;
            _toTag = response.Header.To.ToTag;

            if (_syncRequestContext.ContainsKey(response.Header.CallId))
            {

                SIPRequest request = _syncRequestContext[response.Header.CallId];

                var targetValue = new Tuple<SIPRequest, SIPResponse>(request, response);
                //update the collection
                _syncResponseContext.Enqueue(targetValue);

                //notice worker to go 
                _autoResetEventForRpc.Set();
                //remove it for collection.


            }
            _sipMsgCoreService.SendRequest(RemoteEndPoint, ackReq);
        }

        private FileStream m_fs;
        //private void RtpChannel_OnFrameReady(RTPFrame frame)
        //{
        //    OnStreamReady?.Invoke(frame);
        //    //foreach (var item in frame.FramePackets)
        //    //{
        //    //    logger.Debug("Seq:" + item.Header.SequenceNumber + "----Timestamp:" + item.Header.Timestamp + "-----Length:" + item.Payload.Length);
        //    //}
        //    //byte[] buffer = frame.GetFramePayload();
        //    //if (this.m_fs == null)
        //    //{
        //    //    this.m_fs = new FileStream("D:\\" + _deviceId + ".h264", FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite, 8 * 1024);
        //    //}
        //    //m_fs.Write(buffer, 0, buffer.Length);
        //    //m_fs.Flush();

        //    //PsToH264(buffer);
        //}

        /// <summary>
        /// 结束实时视频请求
        /// </summary>
        public void ByeVideoReq()
        {
            if (_reqSession == null)
            {
                return;
            }
            SIPURI localUri = new SIPURI(_sipMsgCoreService.LocalSIPId, _sipMsgCoreService.LocalEP.ToHost(), "");
            SIPURI remoteUri = new SIPURI(DeviceId, RemoteEndPoint.ToHost(), "");
            SIPFromHeader from = _reqSession.Header.From;
            SIPToHeader to = _reqSession.Header.To; to.ToTag = _toTag;
            SIPRequest byeReq = _sipTransport.GetRequest(SIPMethodsEnum.BYE, remoteUri);
            SIPHeader header = new SIPHeader(from, to, _reqSession.Header.CSeq + 1, _reqSession.Header.CallId)
            {
                CSeqMethod = SIPMethodsEnum.BYE,
                Vias = _reqSession.Header.Vias,
                UserAgent = SIPConstants.SIP_USERAGENT_STRING,
                Contact = _reqSession.Header.Contact
            };
            byeReq.Header = header;
            Stop();
            _sipMsgCoreService.SendReliableRequest(RemoteEndPoint, byeReq);
        }
        public void ByeVideoReq(string sessionid)
        {
            try
            {
                if (!_syncRequestContext.Keys.Contains(sessionid)) return;
                SIPRequest reqSession = _syncRequestContext[sessionid];
                if (reqSession == null)
                {
                    return;
                }
                SIPURI localUri = new SIPURI(_sipMsgCoreService.LocalSIPId, _sipMsgCoreService.LocalEP.ToHost(), "");
                SIPURI remoteUri = new SIPURI(DeviceId, RemoteEndPoint.ToHost(), "");
                SIPFromHeader from = reqSession.Header.From;
                SIPToHeader to = reqSession.Header.To; to.ToTag = _toTag;
                SIPRequest byeReq = _sipTransport.GetRequest(SIPMethodsEnum.BYE, remoteUri);
                SIPHeader header = new SIPHeader(from, to, reqSession.Header.CSeq + 1, reqSession.Header.CallId)
                {
                    CSeqMethod = SIPMethodsEnum.BYE,
                    Vias = reqSession.Header.Vias,
                    UserAgent = SIPConstants.SIP_USERAGENT_STRING,
                    Contact = reqSession.Header.Contact
                };
                byeReq.Header = header;
                Stop();
                _sipMsgCoreService.SendReliableRequest(RemoteEndPoint, byeReq);
                _syncRequestContext.TryRemove(sessionid, out reqSession);
            }
            catch (Exception ex)
            {
                logger.Error("ByeVideoReq: " + ex.Message);
            }
        }

        /// <summary>
        /// 停止计时器/关闭RTP通道
        /// </summary>
        public void Stop()
        {
            //if (_channel != null)
            //{
            //    _channel.OnFrameReady -= RtpChannel_OnFrameReady;
            //    _channel.Stop();
            //}
            if (m_fs != null)
            {
                m_fs.Close();
                m_fs = null;
            }
        }

        /// <summary>
        /// 设置媒体参数请求(实时)
        /// </summary>
        /// <param name="receiveIp">接收端ip地址</param>
        /// <param name="mediaPort">rtp/rtcp媒体端口(10000/10001)</param>
        /// <returns></returns>
        private string SetMediaReq(string receiveIp, int[] mediaPort)
        {
            var sdpConn = new SDPConnectionInformation(receiveIp);

            var sdp = new SDP
            {
                Version = 0,
                SessionId = "0",
                Username = _sipMsgCoreService.LocalSIPId,
                SessionName = CommandType.Play.ToString(),
                Connection = sdpConn,
                Timing = "0 0",
                Address = receiveIp
            };

            SDPMediaFormat psFormat = new SDPMediaFormat(SDPMediaFormatsEnum.PS)
            {
                IsStandardAttribute = false
            };
            SDPMediaFormat h264Format = new SDPMediaFormat(SDPMediaFormatsEnum.H264)
            {
                IsStandardAttribute = false
            };
            SDPMediaAnnouncement media = new SDPMediaAnnouncement
            {
                Media = SDPMediaTypesEnum.video
            };
            media.MediaFormats.Add(psFormat);
            media.MediaFormats.Add(h264Format);
            media.AddExtra("a=recvonly");

            if (_sipAccount.StreamProtocol == ProtocolType.Tcp)
            {
                media.Transport = "TCP/RTP/AVP";
                media.AddExtra("a=setup:" + _sipAccount.TcpMode);
                media.AddExtra("a=connection:new");
            }
            //media.AddExtra("y=0123456789");
            media.AddFormatParameterAttribute(psFormat.FormatID, psFormat.Name);
            media.AddFormatParameterAttribute(h264Format.FormatID, h264Format.Name);
            media.Port = mediaPort[0];

            sdp.Media.Add(media);

            return sdp.ToString();
        }

        /// <summary>
        /// 设置sip主题
        /// </summary>
        /// <returns></returns>
        private string SetSubject()
        {
            return DeviceId + ":" + 0 + "," + _sipMsgCoreService.LocalSIPId + ":" + new Random().Next(100, ushort.MaxValue);
        }
        #endregion

        #region 录像点播

        public void RecordQueryTotal(int recordTotal)
        {
            _recordTotal = recordTotal;
        }

        /// <summary>
        /// 录像文件查询
        /// </summary>
        /// <param name="startTime">开始时间</param>
        /// <param name="endTime">结束时间</param>
        public int RecordFileQuery(DateTime startTime, DateTime endTime, string type)
        {

            this.Stop();

            SIPURI remoteUri = new SIPURI(DeviceId, RemoteEndPoint.ToHost(), "");
            SIPURI localUri = new SIPURI(_sipMsgCoreService.LocalSIPId, _sipMsgCoreService.LocalEP.ToHost(), "");
            SIPFromHeader from = new SIPFromHeader(null, localUri, CallProperties.CreateNewTag());
            SIPToHeader to = new SIPToHeader(null, remoteUri, CallProperties.CreateNewTag());
            SIPRequest recordFileReq = _sipTransport.GetRequest(SIPMethodsEnum.MESSAGE, remoteUri);
            SIPContactHeader contactHeader = new SIPContactHeader(null, localUri);
            recordFileReq.Header.Contact.Clear();
            recordFileReq.Header.Contact.Add(contactHeader);

            recordFileReq.Header.Allow = null;
            recordFileReq.Header.From = from;
            recordFileReq.Header.To = to;
            recordFileReq.Header.UserAgent = SIPConstants.SIP_USERAGENT_STRING;
            recordFileReq.Header.CSeq = CallProperties.CreateNewCSeq();
            recordFileReq.Header.CallId = CallProperties.CreateNewCallId();
            recordFileReq.Header.ContentType = "application/MANSCDP+xml";

            string bTime = startTime.ToString("yyyy-MM-ddTHH:mm:ss");
            string eTime = endTime.ToString("yyyy-MM-ddTHH:mm:ss");
            RecordQuery record = new RecordQuery()
            {
                DeviceID = DeviceId,
                SN = new Random().Next(1, 3000),
                CmdType = CommandType.RecordInfo,
                Secrecy = 0,
                StartTime = bTime,
                EndTime = eTime,
                Type = type
            };

            _recordTotal = -1;
            string xmlBody = RecordQuery.Instance.Save<RecordQuery>(record);
            recordFileReq.Body = xmlBody;
            _sipMsgCoreService.SendRequest(RemoteEndPoint, recordFileReq);
            DateTime recordQueryTime = DateTime.Now;
            while (_recordTotal < 0)
            {
                Thread.Sleep(50);
                if (DateTime.Now.Subtract(recordQueryTime).TotalSeconds > 2)
                {
                    logger.Debug("[" + DeviceId + "] 等待录像查询超时");
                    _recordTotal = 0;
                    break;
                }
            }

            return _recordTotal;
        }
        /// <summary>
        /// 录像点播视频请求(回放)
        /// </summary>
        /// <param name="beginTime">开始时间</param>
        /// <param name="endTime">结束时间</param>
        public void BackVideoReq(DateTime beginTime, DateTime endTime)
        {
            _mediaPort = _sipMsgCoreService.SetMediaPort();

            uint startTime = TimeConvert.DateToTimeStamp(beginTime);
            uint stopTime = TimeConvert.DateToTimeStamp(endTime);

            string localIp = _sipMsgCoreService.LocalEP.Address.ToString();
            string fromTag = CallProperties.CreateNewTag();
            int cSeq = CallProperties.CreateNewCSeq();
            string callId = CallProperties.CreateNewCallId();
            logger.Debug("BackVideoReq: DeviceId=" + DeviceId);
            SIPURI remoteUri = new SIPURI(DeviceId, RemoteEndPoint.ToHost(), "");
            SIPURI localUri = new SIPURI(_sipMsgCoreService.LocalSIPId, _sipMsgCoreService.LocalEP.ToHost(), "");
            SIPFromHeader from = new SIPFromHeader(null, localUri, fromTag);
            SIPToHeader to = new SIPToHeader(null, remoteUri, null);
            SIPRequest backReq = _sipTransport.GetRequest(SIPMethodsEnum.INVITE, remoteUri);
            SIPContactHeader contactHeader = new SIPContactHeader(null, localUri);
            backReq.Header.Contact.Clear();
            backReq.Header.Contact.Add(contactHeader);

            backReq.Header.Allow = null;
            backReq.Header.From = from;
            backReq.Header.To = to;
            backReq.Header.UserAgent = SIPConstants.SIP_USERAGENT_STRING;
            backReq.Header.CSeq = cSeq;
            backReq.Header.CallId = callId;
            backReq.Header.Subject = SetSubject();
            backReq.Header.ContentType = "application/sdp";

            backReq.Body = SetMediaReq(localIp, _mediaPort, startTime, stopTime);
            _sipMsgCoreService.SendRequest(RemoteEndPoint, backReq);
            _reqSession = backReq;
        }
        public int BackVideoReq(ulong beginTime, ulong endTime, int[] mediaPort, string receiveIP, bool needResult = false)
        {
            //_mediaPort = _sipMsgCoreService.SetMediaPort();

            ulong startTime = beginTime;
            ulong stopTime = endTime;

            //string localIp = _sipMsgCoreService.LocalEP.Address.ToString();
            string fromTag = CallProperties.CreateNewTag();
            int cSeq = CallProperties.CreateNewCSeq();
            string callId = CallProperties.CreateNewCallId();

            SIPURI remoteUri = new SIPURI(DeviceId, RemoteEndPoint.ToHost(), "");
            SIPURI localUri = new SIPURI(_sipMsgCoreService.LocalSIPId, _sipMsgCoreService.LocalEP.ToHost(), "");
            SIPFromHeader from = new SIPFromHeader(null, localUri, fromTag);
            SIPToHeader to = new SIPToHeader(null, remoteUri, null);
            SIPRequest backReq = _sipTransport.GetRequest(SIPMethodsEnum.INVITE, remoteUri);
            SIPContactHeader contactHeader = new SIPContactHeader(null, localUri);
            backReq.Header.Contact.Clear();
            backReq.Header.Contact.Add(contactHeader);

            backReq.Header.Allow = null;
            backReq.Header.From = from;
            backReq.Header.To = to;
            backReq.Header.UserAgent = SIPConstants.SIP_USERAGENT_STRING;
            backReq.Header.CSeq = cSeq;
            backReq.Header.CallId = callId;
            backReq.Header.Subject = SetSubject();
            backReq.Header.ContentType = "application/sdp";

            backReq.Body = SetMediaReq(receiveIP, mediaPort, startTime, stopTime);
            _sipMsgCoreService.SendRequest(RemoteEndPoint, backReq);
            _reqSession = backReq;

            if (needResult)
            {
                _syncRequestContext.TryAdd(callId, backReq);
            }
            return cSeq;
        }

        /// <summary>
        /// 录像文件下载
        /// </summary>
        /// <param name="beginTime">开始时间</param>
        /// <param name="endTime">结束时间</param>
        public void VideoDownloadReq(DateTime beginTime, DateTime endTime)
        {
            _mediaPort = _sipMsgCoreService.SetMediaPort();

            uint startTime = TimeConvert.DateToTimeStamp(beginTime);
            uint stopTime = TimeConvert.DateToTimeStamp(endTime);


            string localIp = _sipMsgCoreService.LocalEP.Address.ToString();
            string fromTag = CallProperties.CreateNewTag();
            int cSeq = CallProperties.CreateNewCSeq();
            string callId = CallProperties.CreateNewCallId();

            SIPURI remoteUri = new SIPURI(DeviceId, RemoteEndPoint.ToHost(), "");
            SIPURI localUri = new SIPURI(_sipMsgCoreService.LocalSIPId, _sipMsgCoreService.LocalEP.ToHost(), "");
            SIPFromHeader from = new SIPFromHeader(null, localUri, fromTag);
            SIPToHeader to = new SIPToHeader(null, remoteUri, null);
            SIPRequest backReq = _sipTransport.GetRequest(SIPMethodsEnum.INVITE, remoteUri);
            SIPContactHeader contactHeader = new SIPContactHeader(null, localUri);
            backReq.Header.Contact.Clear();
            backReq.Header.Contact.Add(contactHeader);

            backReq.Header.Allow = null;
            backReq.Header.From = from;
            backReq.Header.To = to;
            backReq.Header.UserAgent = SIPConstants.SIP_USERAGENT_STRING;
            backReq.Header.CSeq = cSeq;
            backReq.Header.CallId = callId;
            backReq.Header.Subject = SetSubject();
            backReq.Header.ContentType = "application/sdp";


            backReq.Body = SetMediaDownloadReq(localIp, _mediaPort, startTime, stopTime);
            _sipMsgCoreService.SendRequest(RemoteEndPoint, backReq);
            _reqSession = backReq;

            //SIPRequest realReq = VideoDownloadReq(_mediaPort, startTime, stopTime);
            //_msgCore.SendRequest(_remoteEP, realReq);
            //_realTask = new TaskTiming(realReq, _msgCore.Transport);
            //_msgCore.SendRequestTimeout += _realTask.MessageSendRequestTimeout;
            //_realTask.OnCloseRTPChannel += Task_OnCloseRTPChannel;
            //_realTask.Start();
        }
        public int VideoDownloadReq(DateTime beginTime, DateTime endTime, int[] mediaPort, string receiveIP, bool needResult = false)
        {
            //_mediaPort = _sipMsgCoreService.SetMediaPort();

            uint startTime = (UInt32)(beginTime - new DateTime(1970, 1, 1, 0, 0, 0, 0)).TotalSeconds;
            uint stopTime = (UInt32)(endTime - new DateTime(1970, 1, 1, 0, 0, 0, 0)).TotalSeconds;


            //string localIp = _sipMsgCoreService.LocalEP.Address.ToString();
            string fromTag = CallProperties.CreateNewTag();
            int cSeq = CallProperties.CreateNewCSeq();
            string callId = CallProperties.CreateNewCallId();

            SIPURI remoteUri = new SIPURI(DeviceId, RemoteEndPoint.ToHost(), "");
            SIPURI localUri = new SIPURI(_sipMsgCoreService.LocalSIPId, _sipMsgCoreService.LocalEP.ToHost(), "");
            SIPFromHeader from = new SIPFromHeader(null, localUri, fromTag);
            SIPToHeader to = new SIPToHeader(null, remoteUri, null);
            SIPRequest backReq = _sipTransport.GetRequest(SIPMethodsEnum.INVITE, remoteUri);
            SIPContactHeader contactHeader = new SIPContactHeader(null, localUri);
            backReq.Header.Contact.Clear();
            backReq.Header.Contact.Add(contactHeader);

            backReq.Header.Allow = null;
            backReq.Header.From = from;
            backReq.Header.To = to;
            backReq.Header.UserAgent = SIPConstants.SIP_USERAGENT_STRING;
            backReq.Header.CSeq = cSeq;
            backReq.Header.CallId = callId;
            backReq.Header.Subject = SetSubject();
            backReq.Header.ContentType = "application/sdp";


            backReq.Body = SetMediaDownloadReq(receiveIP, mediaPort, startTime, stopTime);
            _sipMsgCoreService.SendRequest(RemoteEndPoint, backReq);
            _reqSession = backReq;

            if (needResult)
            {
                _syncRequestContext.TryAdd(callId, backReq);
            }
            return cSeq;
        }


        /// <summary>
        /// 设置媒体参数请求(回放)
        /// </summary>
        /// <param name="localIp">本地ip</param>
        /// <param name="mediaPort">rtp/rtcp媒体端口(10000/10001)</param>
        /// <param name="startTime">录像开始时间</param>
        /// <param name="stopTime">录像结束数据</param>
        /// <returns></returns>
        private string SetMediaReq(string localIp, int[] mediaPort, ulong startTime, ulong stopTime)
        {

            var sdpConn = new SDPConnectionInformation(localIp);

            SDP sdp = new SDP
            {
                Version = 0,
                SessionId = "0",
                Username = _sipMsgCoreService.LocalSIPId,
                SessionName = CommandType.Playback.ToString(),
                Connection = sdpConn,
                Timing = startTime + " " + stopTime,
                Address = localIp,
                URI = DeviceId + ":" + 3
            };

            SDPMediaFormat psFormat = new SDPMediaFormat(SDPMediaFormatsEnum.PS)
            {
                IsStandardAttribute = false
            };
            SDPMediaFormat h264Format = new SDPMediaFormat(SDPMediaFormatsEnum.H264)
            {
                IsStandardAttribute = false
            };
            //SDPMediaFormat mpeg4Format = new SDPMediaFormat(SDPMediaFormatsEnum.MPEG4)
            //{
            //    IsStandardAttribute = false
            //};
            SDPMediaAnnouncement media = new SDPMediaAnnouncement
            {
                Media = SDPMediaTypesEnum.video
            };

            media.MediaFormats.Add(psFormat);
            media.MediaFormats.Add(h264Format);
            //media.MediaFormats.Add(mpeg4Format);
            media.AddExtra("a=recvonly");
            if (_sipAccount.StreamProtocol == ProtocolType.Tcp)
            {
                media.Transport = "TCP/RTP/AVP";
                media.AddExtra("a=setup:" + _sipAccount.TcpMode);
                media.AddExtra("a=connection:new");
            }
            media.AddFormatParameterAttribute(psFormat.FormatID, psFormat.Name);
            media.AddFormatParameterAttribute(h264Format.FormatID, h264Format.Name);
            media.Port = mediaPort[0];

            sdp.Media.Add(media);

            return sdp.ToString();
        }

        /// <summary>
        /// 录像文件下载
        /// </summary>
        /// <param name="localIp">本地ip</param>
        /// <param name="mediaPort">rtp/rtcp媒体端口(10000/10001)</param>
        /// <param name="startTime">录像开始时间</param>
        /// <param name="stopTime">录像结束数据</param>
        /// <returns></returns>
        private string SetMediaDownloadReq(string localIp, int[] mediaPort, uint startTime, uint stopTime)
        {
            SDPConnectionInformation sdpConn = new SDPConnectionInformation(localIp);

            SDP sdp = new SDP
            {
                Version = 0,
                SessionId = "0",
                Username = _sipMsgCoreService.LocalSIPId,
                SessionName = CommandType.Download.ToString(),
                Connection = sdpConn,
                Timing = startTime + " " + stopTime,
                Address = localIp,
                URI = DeviceId + ":" + 3
            };

            SDPMediaFormat psFormat = new SDPMediaFormat(SDPMediaFormatsEnum.PS)
            {
                IsStandardAttribute = false
            };
            SDPMediaFormat h264Format = new SDPMediaFormat(SDPMediaFormatsEnum.H264)
            {
                IsStandardAttribute = false
            };
            //SDPMediaFormat mpeg4Format = new SDPMediaFormat(SDPMediaFormatsEnum.MPEG4)
            //{
            //    IsStandardAttribute = false
            //};
            SDPMediaAnnouncement media = new SDPMediaAnnouncement
            {
                Media = SDPMediaTypesEnum.video
            };

            media.MediaFormats.Add(psFormat);
            media.MediaFormats.Add(h264Format);
            //media.MediaFormats.Add(mpeg4Format);
            media.AddExtra("a=recvonly");
            if (_sipAccount.StreamProtocol == ProtocolType.Tcp)
            {
                media.Transport = "TCP/RTP/AVP";
                media.AddExtra("a=setup:" + _sipAccount.TcpMode);
                media.AddExtra("a=connection:new");
            }

            //media.AddExtra("y=010000" + _ssrc.ToString("D4"));
            media.AddExtra("a=downloadspeed:" + "2");
            //media.AddExtra("a=username:34020000001320000001");
            //media.AddExtra("a=password:12345678");
            //media.AddExtra("f=v/2/4///a///");
            media.AddFormatParameterAttribute(psFormat.FormatID, psFormat.Name);
            media.AddFormatParameterAttribute(h264Format.FormatID, h264Format.Name);
            media.Port = mediaPort[0];

            sdp.Media.Add(media);

            return sdp.ToString();
        }

        /// <summary>
        /// 结束录像点播视频请求
        /// </summary>
        /// <returns></returns>
        public void BackVideoStopPlayingControlReq()
        {
            if (_reqSession == null)
            {
                return;
            }
            SIPURI localUri = new SIPURI(_sipMsgCoreService.LocalSIPId, _sipMsgCoreService.LocalEP.ToHost(), "");
            SIPURI remoteUri = new SIPURI(DeviceId, RemoteEndPoint.ToHost(), "");
            SIPFromHeader from = new SIPFromHeader(null, localUri, _reqSession.Header.From.FromTag);
            SIPToHeader to = new SIPToHeader(null, remoteUri, _reqSession.Header.To.ToTag);
            SIPRequest byeReq = _sipTransport.GetRequest(SIPMethodsEnum.BYE, remoteUri);
            SIPHeader header = new SIPHeader(from, to, _reqSession.Header.CSeq, _reqSession.Header.CallId)
            {
                CSeqMethod = byeReq.Header.CSeqMethod,
                Vias = byeReq.Header.Vias,
                MaxForwards = byeReq.Header.MaxForwards,
                UserAgent = SIPConstants.SIP_USERAGENT_STRING
            };
            byeReq.Header.From = from;
            byeReq.Header = header;
            this.Stop();
            _sipMsgCoreService.SendRequest(RemoteEndPoint, byeReq);
        }
        public bool BackVideoStopPlayingControlReq(string sessionid)
        {
            try
            {
                if (!_syncRequestContext.Keys.Contains(sessionid)) return false;
                SIPRequest reqSession = _syncRequestContext[sessionid];
                if (reqSession == null)
                {
                    return false;
                }
                SIPURI localUri = new SIPURI(_sipMsgCoreService.LocalSIPId, _sipMsgCoreService.LocalEP.ToHost(), "");
                SIPURI remoteUri = new SIPURI(DeviceId, RemoteEndPoint.ToHost(), "");
                SIPFromHeader from = new SIPFromHeader(null, localUri, reqSession.Header.From.FromTag);
                SIPToHeader to = new SIPToHeader(null, remoteUri, reqSession.Header.To.ToTag);
                SIPRequest byeReq = _sipTransport.GetRequest(SIPMethodsEnum.BYE, remoteUri);
                SIPHeader header = new SIPHeader(from, to, reqSession.Header.CSeq, reqSession.Header.CallId)
                {
                    CSeqMethod = byeReq.Header.CSeqMethod,
                    Vias = byeReq.Header.Vias,
                    MaxForwards = byeReq.Header.MaxForwards,
                    UserAgent = SIPConstants.SIP_USERAGENT_STRING
                };
                byeReq.Header.From = from;
                byeReq.Header = header;
                this.Stop();
                _sipMsgCoreService.SendRequest(RemoteEndPoint, byeReq);
                _syncRequestContext.TryRemove(sessionid, out reqSession);
                return true;
            }
            catch (Exception ex)
            {
                logger.Error("BackVideoStopPlayingControlReq: " + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// 控制录像点播播放速度
        /// </summary>
        /// <param name="range"></param>
        /// <returns></returns>
        public bool BackVideoPlaySpeedControlReq(string range)
        {
            _mediaPort = _sipMsgCoreService.SetMediaPort();

            string localIp = _sipMsgCoreService.LocalEP.Address.ToString();
            string fromTag = CallProperties.CreateNewTag();
            int cSeq = CallProperties.CreateNewCSeq();
            string callId = CallProperties.CreateNewCallId();

            SIPRequest realReq = BackVideoPlaySpeedControlReq(localIp, _mediaPort, range, fromTag, cSeq, callId);
            _sipMsgCoreService.SendRequest(RemoteEndPoint, realReq);
            return true;
        }
        public bool BackVideoPlaySpeedControlReq(string sessionid, float scale)
        {
            try
            {
                //_mediaPort = _sipMsgCoreService.SetMediaPort();
                //uint time = TimeConvert.DateToTimeStamp(range);
                //string localIp = _sipMsgCoreService.LocalEP.Address.ToString();
                string fromTag = CallProperties.CreateNewTag();
                int cSeq = CallProperties.CreateNewCSeq();
                string callId = CallProperties.CreateNewCallId();
                SIPRequest realReq = BackVideoPlaySpeedControlReq(sessionid, scale, fromTag, cSeq, callId);
                _sipMsgCoreService.SendRequest(RemoteEndPoint, realReq);
                return true;
            }
            catch (Exception ex)
            {
                logger.Error("BackVideoPlaySpeedControlReq: " + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// 控制录像点播播放速度
        /// </summary>
        /// <param name="localIp">本地ip</param>
        /// <param name="mediaPort">端口号</param>
        /// <param name="scale">播放速率</param>
        /// <param name="Time">时间范围</param>
        /// <param name="fromTag">from tag</param>
        /// <param name="cSeq">序号</param>
        /// <param name="callId">呼叫编号</param>
        /// <returns></returns>

        private SIPRequest BackVideoPlaySpeedControlReq(string localIp, int[] mediaPort, string scale, string fromTag, int cSeq, string callId)
        {
            SIPURI remoteUri = new SIPURI(DeviceId, RemoteEndPoint.ToHost(), "");
            SIPURI localUri = new SIPURI(_sipMsgCoreService.LocalSIPId, _sipMsgCoreService.LocalEP.ToHost(), "");
            SIPFromHeader from = new SIPFromHeader(null, localUri, _reqSession.Header.From.FromTag);
            SIPToHeader to = new SIPToHeader(null, remoteUri, _reqSession.Header.To.ToTag);
            SIPRequest backReq = _sipTransport.GetRequest(SIPMethodsEnum.INFO, remoteUri);
            SIPContactHeader contactHeader = new SIPContactHeader(null, localUri);
            backReq.Header.Contact.Clear();
            backReq.Header.Contact.Add(contactHeader);

            backReq.Header.Allow = null;
            backReq.Header.From = from;
            backReq.Header.To = to;
            backReq.Header.UserAgent = SIPConstants.SIP_USERAGENT_STRING;
            backReq.Header.CSeq = _reqSession.Header.CSeq + 1;
            backReq.Header.CallId = _reqSession.Header.CallId;
            backReq.Header.Subject = SetSubject();
            backReq.Header.ContentType = "Application/MANSRTSP";

            backReq.Body = SetMediaControlReq(localIp, mediaPort, scale, cSeq);
            _reqSession = backReq;
            return backReq;
        }
        private SIPRequest BackVideoPlaySpeedControlReq(string sessionid, float scale, string fromTag, int cSeq, string callId)
        {
            if (!_syncRequestContext.Keys.Contains(sessionid)) return null;
            SIPRequest reqSession = _syncRequestContext[sessionid];
            if (reqSession == null)
            {
                return null;
            }
            SIPURI remoteUri = new SIPURI(DeviceId, RemoteEndPoint.ToHost(), "");
            SIPURI localUri = new SIPURI(_sipMsgCoreService.LocalSIPId, _sipMsgCoreService.LocalEP.ToHost(), "");
            SIPFromHeader from = new SIPFromHeader(null, localUri, reqSession.Header.From.FromTag);
            SIPToHeader to = new SIPToHeader(null, remoteUri, reqSession.Header.To.ToTag);
            SIPRequest backReq = _sipTransport.GetRequest(SIPMethodsEnum.INFO, remoteUri);
            SIPContactHeader contactHeader = new SIPContactHeader(null, localUri);
            backReq.Header.Contact.Clear();
            backReq.Header.Contact.Add(contactHeader);
            backReq.Header.Allow = null;
            backReq.Header.From = from;
            backReq.Header.To = to;
            backReq.Header.UserAgent = SIPConstants.SIP_USERAGENT_STRING;
            backReq.Header.CSeq = cSeq;
            backReq.Header.CallId = callId;
            backReq.Header.Subject = SetSubject();
            backReq.Header.ContentType = "Application/MANSRTSP";
            backReq.Body = SetMediaSpeedReq(scale, cSeq);
            _reqSession = backReq;
            return backReq;
        }
        /// <summary>
        /// 设置媒体参数请求(快放/慢放)
        /// </summary>
        /// <param name="localIp">本地ip</param>
        /// <param name="mediaPort">rtp/rtcp媒体端口(10000/10001)</param>
        /// <param name="scale">播放速率(0.25,0.5,1,2,4)</param>
        /// <returns></returns>
        private string SetMediaControlReq(string localIp, int[] mediaPort, string scale, int cseq)
        {
            string str =
                "PLAY MANSRTSP/1.0\r\n" +
                "CSeq: 3\r\n" +
                "Scale: " + scale + "\r\n";
            return str;
        }
        private string SetMediaSpeedReq(float scale, int cseq)
        {
            string str =
                "PLAY MANSRTSP/1.0\r\n" +
                "CSeq: " + cseq + "\r\n" +
                "Scale: " + scale + "\r\n";
            return str;
        }

        /// <summary>
        /// 设置媒体参数请求(回放 播放速率)
        /// </summary>
        /// <param name="localIp">本地ip</param>
        /// <param name="mediaPort">rtp/rtcp媒体端口(10000/10001)</param>
        /// <param name="scale">播放速率</param>
        /// <param name="Time">时间范围</param>
        /// <returns></returns>
        private string SetMediaReq(string localIp, int[] mediaPort, int Time, string scale = "1")
        {

            //uint time = TimeConvert.DateToTimeStamp(default(DateTime));
            string str = string.Empty;
            str += "PLAY MANSRTSP/1.0" + "\r\n";
            str += "CSeq: 2" + "\r\n";
            str += "Scale: " + scale + "\r\n";
            //if (Time == 0 || time == Time)
            //{
            //    str += "Range: npt=now-" + "\r\n";
            //}
            //else
            //{
            str += "Range: npt=" + Time + "- " + "\r\n";
            //}
            return str;
        }
        /// <summary>
        /// 恢复录像播放
        /// </summary>
        public void BackVideoContinuePlayingControlReq()
        {
            if (_mediaPort == null)
            {
                _mediaPort = _sipMsgCoreService.SetMediaPort();
            }

            string localIp = _sipMsgCoreService.LocalEP.Address.ToString();
            string fromTag = CallProperties.CreateNewTag();
            int cSeq = CallProperties.CreateNewCSeq();
            string callId = CallProperties.CreateNewCallId();
            //this.Stop();
            SIPRequest realReq = BackVideoContinuePlayingControlReq(localIp, _mediaPort, fromTag, cSeq, callId);
            _sipMsgCoreService.SendRequest(RemoteEndPoint, realReq);
        }
        public bool BackVideoContinuePlayingControlReq(string sessionid)
        {
            try
            {
                //if (_mediaPort == null)
                //{
                //    _mediaPort = _sipMsgCoreService.SetMediaPort();
                //}
                string localIp = _sipMsgCoreService.LocalEP.Address.ToString();
                string fromTag = CallProperties.CreateNewTag();
                int cSeq = CallProperties.CreateNewCSeq();
                string callId = CallProperties.CreateNewCallId();
                //this.Stop();
                SIPRequest realReq = BackVideoContinuePlayingControlReq(sessionid, fromTag, cSeq, callId);
                _sipMsgCoreService.SendRequest(RemoteEndPoint, realReq);
                return false;
            }
            catch (Exception ex)
            {
                logger.Error("BackVideoContinuePlayingControlReq: " + ex.Message);
                return true;
            }
        }
        /// <summary>
        /// 恢复录像播放
        /// </summary>
        /// <param name="localIp">本地ip</param>
        /// <param name="mediaPort">端口号</param>
        /// <param name="fromTag">from tag</param>
        /// <param name="cSeq">序号</param>
        /// <param name="callId">呼叫编号</param>
        /// <returns></returns>
        public SIPRequest BackVideoContinuePlayingControlReq(string localIp, int[] mediaPort, string fromTag, int cSeq, string callId)
        {
            SIPURI remoteUri = new SIPURI(DeviceId, RemoteEndPoint.ToHost(), "");
            SIPURI localUri = new SIPURI(_sipMsgCoreService.LocalSIPId, _sipMsgCoreService.LocalEP.ToHost(), "");
            SIPFromHeader from = new SIPFromHeader(null, localUri, _reqSession.Header.From.FromTag);
            SIPToHeader to = new SIPToHeader(null, remoteUri, _reqSession.Header.To.ToTag);
            SIPRequest backReq = _sipTransport.GetRequest(SIPMethodsEnum.INFO, remoteUri);
            SIPContactHeader contactHeader = new SIPContactHeader(null, localUri);
            backReq.Header.Contact.Clear();
            backReq.Header.Contact.Add(contactHeader);

            backReq.Header.Allow = null;
            backReq.Header.From = from;
            backReq.Header.To = to;
            backReq.Header.UserAgent = SIPConstants.SIP_USERAGENT_STRING;
            backReq.Header.CSeq = _reqSession.Header.CSeq + 1;
            backReq.Header.CallId = _reqSession.Header.CallId;
            //backReq.Header.Subject = SetSubject();
            backReq.Header.ContentType = "Application/MANSRTSP";

            backReq.Body = SetMediaResumeReq(cSeq);
            _reqSession = backReq;
            return backReq;
        }
        public SIPRequest BackVideoContinuePlayingControlReq(string sessionid, string fromTag, int cSeq, string callId)
        {
            if (!_syncRequestContext.Keys.Contains(sessionid)) return null;
            SIPRequest reqSession = _syncRequestContext[sessionid];
            if (reqSession == null)
            {
                return null;
            }
            SIPURI remoteUri = new SIPURI(DeviceId, RemoteEndPoint.ToHost(), "");
            SIPURI localUri = new SIPURI(_sipMsgCoreService.LocalSIPId, _sipMsgCoreService.LocalEP.ToHost(), "");
            SIPFromHeader from = new SIPFromHeader(null, localUri, reqSession.Header.From.FromTag);
            SIPToHeader to = new SIPToHeader(null, remoteUri, reqSession.Header.To.ToTag);
            SIPRequest backReq = _sipTransport.GetRequest(SIPMethodsEnum.INFO, remoteUri);
            SIPContactHeader contactHeader = new SIPContactHeader(null, localUri);
            backReq.Header.Contact.Clear();
            backReq.Header.Contact.Add(contactHeader);
            backReq.Header.Allow = null;
            backReq.Header.From = from;
            backReq.Header.To = to;
            backReq.Header.UserAgent = SIPConstants.SIP_USERAGENT_STRING;
            backReq.Header.CSeq = reqSession.Header.CSeq + 1;
            backReq.Header.CallId = reqSession.Header.CallId;
            //backReq.Header.Subject = SetSubject();
            backReq.Header.ContentType = "Application/MANSRTSP";
            backReq.Body = SetMediaResumeReq(cSeq);
            _reqSession = backReq;
            return backReq;
        }
        /// <summary>
        /// 设置录像恢复播放包体信息
        /// </summary>
        /// <returns></returns>
        private string SetMediaResumeReq(int cseq)
        {
            string str =
                "PLAY MANSRTSP/1.0\r\n" +
                "CSeq: 2\r\n" +
                //"Scale: 1.0\r\n"+
                "Range: npt=now-\r\n";
            return str;
        }
        /// <summary>
        /// 暂停录像播放
        /// </summary>
        public void BackVideoPauseControlReq()
        {
            if (_mediaPort == null)
            {
                _mediaPort = _sipMsgCoreService.SetMediaPort();

            }

            string localIp = _sipMsgCoreService.LocalEP.Address.ToString();
            string fromTag = CallProperties.CreateNewTag();
            int cSeq = CallProperties.CreateNewCSeq();
            string callId = CallProperties.CreateNewCallId();
            SIPRequest realReq = BackVideoPauseControlReq(localIp, _mediaPort, fromTag, cSeq, callId);
            _sipMsgCoreService.SendRequest(RemoteEndPoint, realReq);
        }
        public bool BackVideoPauseControlReq(string sessionid)
        {
            try
            {
                //if (_mediaPort == null)
                //{
                //    _mediaPort = _sipMsgCoreService.SetMediaPort();
                //}
                //string localIp = _sipMsgCoreService.LocalEP.Address.ToString();
                string fromTag = CallProperties.CreateNewTag();
                int cSeq = CallProperties.CreateNewCSeq();
                string callId = CallProperties.CreateNewCallId();
                SIPRequest realReq = BackVideoPauseControlReq(sessionid, fromTag, cSeq, callId);
                _sipMsgCoreService.SendRequest(RemoteEndPoint, realReq);
                return true;
            }
            catch (Exception ex)
            {
                logger.Error("BackVideoPauseControlReq: " + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// 暂停录像播放
        /// </summary>
        /// <param name="localIp">本地ip</param>
        /// <param name="mediaPort">端口号</param>
        /// <param name="fromTag">from tag</param>
        /// <param name="cSeq">序号</param>
        /// <param name="callId">呼叫编号</param>
        /// <returns></returns>
        public SIPRequest BackVideoPauseControlReq(string localIp, int[] mediaPort, string fromTag, int cSeq, string callId)
        {
            SIPURI remoteUri = new SIPURI(DeviceId, RemoteEndPoint.ToHost(), "");
            SIPURI localUri = new SIPURI(_sipMsgCoreService.LocalSIPId, _sipMsgCoreService.LocalEP.ToHost(), "");
            SIPFromHeader from = new SIPFromHeader(null, localUri, _reqSession.Header.From.FromTag);
            SIPToHeader to = new SIPToHeader(null, remoteUri, _reqSession.Header.To.ToTag);
            SIPRequest backReq = _sipTransport.GetRequest(SIPMethodsEnum.INFO, remoteUri);
            SIPContactHeader contactHeader = new SIPContactHeader(null, localUri);
            backReq.Header.Contact.Clear();
            backReq.Header.Contact.Add(contactHeader);

            backReq.Header.Allow = null;
            backReq.Header.From = from;
            backReq.Header.To = to;
            backReq.Header.UserAgent = SIPConstants.SIP_USERAGENT_STRING;
            //backReq.Header.Vias = _reqSession.Header.Vias;
            backReq.Header.CSeq = _reqSession.Header.CSeq + 1;
            backReq.Header.CallId = _reqSession.Header.CallId;
            //backReq.Header.Subject = SetSubject();
            backReq.Header.ContentType = "Application/MANSRTSP";

            backReq.Body = SetMediaPauseReq(_reqSession.Header.CSeq);
            _reqSession = backReq;
            return backReq;
        }
        public SIPRequest BackVideoPauseControlReq(string sessionid, string fromTag, int cSeq, string callId)
        {
            if (!_syncRequestContext.Keys.Contains(sessionid)) return null;
            SIPRequest reqSession = _syncRequestContext[sessionid];
            if (reqSession == null)
            {
                return null;
            }
            SIPURI remoteUri = new SIPURI(DeviceId, RemoteEndPoint.ToHost(), "");
            SIPURI localUri = new SIPURI(_sipMsgCoreService.LocalSIPId, _sipMsgCoreService.LocalEP.ToHost(), "");
            SIPFromHeader from = new SIPFromHeader(null, localUri, reqSession.Header.From.FromTag);
            SIPToHeader to = new SIPToHeader(null, remoteUri, reqSession.Header.To.ToTag);
            SIPRequest backReq = _sipTransport.GetRequest(SIPMethodsEnum.INFO, remoteUri);
            SIPContactHeader contactHeader = new SIPContactHeader(null, localUri);
            backReq.Header.Contact.Clear();
            backReq.Header.Contact.Add(contactHeader);
            backReq.Header.Allow = null;
            backReq.Header.From = from;
            backReq.Header.To = to;
            backReq.Header.UserAgent = SIPConstants.SIP_USERAGENT_STRING;
            //backReq.Header.Vias = _reqSession.Header.Vias;
            backReq.Header.CSeq = _reqSession.Header.CSeq + 1;
            backReq.Header.CallId = _reqSession.Header.CallId;
            //backReq.Header.Subject = SetSubject();
            backReq.Header.ContentType = "Application/MANSRTSP";
            backReq.Body = SetMediaPauseReq(reqSession.Header.CSeq);
            _reqSession = backReq;
            return backReq;
        }

        /// <summary>
        /// 设置录像暂停包体信息
        /// </summary>
        /// <returns></returns>
        private string SetMediaPauseReq(int cseq)
        {
            string str =
                "PAUSE MANSRTSP/1.0\r\n" +
                "CSeq: 1\r\n" +
                //"Scale: 1.0\r\n"+
                "PauseTime: now\r\n";
            return str;
        }

        /// <summary>
        /// 停止/开启录像(录像控制手动录像)
        /// </summary>
        /// <param name="isRecord">true: 开始录像  false：停止录像</param>
        public void DeviceControlRecord(bool isRecord)
        {
            SIPRequest recordControlReq = QueryItems(RemoteEndPoint, DeviceId);
            Control catalog = new Control()
            {
                CommandType = CommandType.DeviceControl,
                DeviceID = DeviceId,
                SN = new Random().Next(1, ushort.MaxValue),
                RecordCmd = isRecord ? "Record" : "StopRecord"
            };
            string xmlBody = Control.Instance.Save<Control>(catalog);
            recordControlReq.Body = xmlBody;
            _sipMsgCoreService.SendRequest(RemoteEndPoint, recordControlReq);
        }
        #endregion

        /// <summary>
        /// 控制录像随机拖拽
        /// </summary>
        /// <param name="range">时间范围</param>
        public bool BackVideoPlayPositionControlReq(int range)
        {
            _mediaPort = _sipMsgCoreService.SetMediaPort();
            //uint time = TimeConvert.DateToTimeStamp(range);
            string localIp = _sipMsgCoreService.LocalEP.Address.ToString();
            string fromTag = CallProperties.CreateNewTag();
            int cSeq = CallProperties.CreateNewCSeq();
            string callId = CallProperties.CreateNewCallId();

            SIPRequest realReq = BackVideoPlayPositionControlReq(localIp, _mediaPort, range, fromTag, cSeq, callId);
            _sipMsgCoreService.SendRequest(RemoteEndPoint, realReq);
            return true;
        }
        public bool BackVideoPlayPositionControlReq(string sessionid, long time)
        {
            try
            {
                //_mediaPort = _sipMsgCoreService.SetMediaPort();
                //uint time = TimeConvert.DateToTimeStamp(range);
                string localIp = _sipMsgCoreService.LocalEP.Address.ToString();
                string fromTag = CallProperties.CreateNewTag();
                int cSeq = CallProperties.CreateNewCSeq();
                string callId = CallProperties.CreateNewCallId();

                SIPRequest realReq = BackVideoPlayPositionControlReq(sessionid, time, fromTag, cSeq, callId);
                _sipMsgCoreService.SendRequest(RemoteEndPoint, realReq);
                return true;
            }
            catch (Exception ex)
            {
                logger.Error("BackVideoPlayPositionControlReq: " + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// 控制录像随机拖拽
        /// </summary>
        /// <param name="localIp">本地ip</param>
        /// <param name="mediaPort">端口号</param>
        /// <param name="scale">播放速率</param>
        /// <param name="Time">时间范围</param>
        /// <param name="fromTag">from tag</param>
        /// <param name="cSeq">序号</param>
        /// <param name="callId">呼叫编号</param>
        /// <returns></returns>
        private SIPRequest BackVideoPlayPositionControlReq(string localIp, int[] mediaPort, int Time, string fromTag, int cSeq, string callId)
        {
            SIPURI remoteUri = new SIPURI(DeviceId, RemoteEndPoint.ToHost(), "");
            SIPURI localUri = new SIPURI(_sipMsgCoreService.LocalSIPId, _sipMsgCoreService.LocalEP.ToHost(), "");
            SIPFromHeader from = new SIPFromHeader(null, localUri, _reqSession.Header.From.FromTag);
            SIPToHeader to = new SIPToHeader(null, remoteUri, _reqSession.Header.To.ToTag);
            SIPRequest backReq = _sipTransport.GetRequest(SIPMethodsEnum.INFO, remoteUri);
            SIPContactHeader contactHeader = new SIPContactHeader(null, localUri);
            backReq.Header.Contact.Clear();
            backReq.Header.Contact.Add(contactHeader);

            backReq.Header.Allow = null;
            backReq.Header.From = from;
            backReq.Header.To = to;
            backReq.Header.UserAgent = SIPConstants.SIP_USERAGENT_STRING;
            backReq.Header.CSeq = _reqSession.Header.CSeq + 1;
            backReq.Header.CallId = _reqSession.Header.CallId;
            backReq.Header.Subject = SetSubject();
            backReq.Header.ContentType = SIPHeader.ContentTypes.Application_MANSRTSP;

            backReq.Body = SetMediaReq(localIp, mediaPort, Time, cSeq);
            _reqSession = backReq;
            return backReq;
        }
        private SIPRequest BackVideoPlayPositionControlReq(string sessionid, long time, string fromTag, int cSeq, string callId)
        {
            if (!_syncRequestContext.Keys.Contains(sessionid)) return null;
            SIPRequest reqSession = _syncRequestContext[sessionid];
            if (reqSession == null)
            {
                return null;
            }
            SIPURI remoteUri = new SIPURI(DeviceId, RemoteEndPoint.ToHost(), "");
            SIPURI localUri = new SIPURI(_sipMsgCoreService.LocalSIPId, _sipMsgCoreService.LocalEP.ToHost(), "");
            SIPFromHeader from = new SIPFromHeader(null, localUri, reqSession.Header.From.FromTag);
            SIPToHeader to = new SIPToHeader(null, remoteUri, reqSession.Header.To.ToTag);
            SIPRequest backReq = _sipTransport.GetRequest(SIPMethodsEnum.INFO, remoteUri);
            SIPContactHeader contactHeader = new SIPContactHeader(null, localUri);
            backReq.Header.Contact.Clear();
            backReq.Header.Contact.Add(contactHeader);
            backReq.Header.Allow = null;
            backReq.Header.From = from;
            backReq.Header.To = to;
            backReq.Header.UserAgent = SIPConstants.SIP_USERAGENT_STRING;
            backReq.Header.CSeq = reqSession.Header.CSeq + 1;
            backReq.Header.CallId = reqSession.Header.CallId;
            backReq.Header.Subject = SetSubject();
            backReq.Header.ContentType = SIPHeader.ContentTypes.Application_MANSRTSP;
            backReq.Body = SetMediaPositionReq(time, cSeq);
            _reqSession = backReq;
            return backReq;
        }

        /// <summary>
        /// 控制录像随机拖拽
        /// </summary>
        /// <param name="localIp">本地ip</param>
        /// <param name="mediaPort">rtp/rtcp媒体端口(10000/10001)</param>
        /// <param name="Time">时间点(0到终点时间)</param>
        /// <returns></returns>
        private string SetMediaReq(string localIp, int[] mediaPort, int Time, int cseq)
        {
            string str =
                "PLAY MANSRTSP/1.0\r\n" +
                "CSeq: " + cseq + "\r\n" +
                "Range: npt=" + Time + "-\r\n";
            return str;
        }
        private string SetMediaPositionReq(long time, int cseq)
        {
            string str =
                "PLAY MANSRTSP/1.0\r\n" +
                "CSeq: " + cseq + "\r\n" +
                "Range: npt=" + time + "-\r\n";
            return str;
        }

        #region PTZ云台控制

        /// <summary>
        /// PTZ云台控制
        /// </summary>
        /// <param name="ucommand">控制命令</param>
        /// <param name="dwStop">开始或结束</param>
        /// <param name="dwSpeed">速度</param>
        public void PtzContrl(PTZCommand ucommand, int dwSpeed)
        {
            string fromTag = CallProperties.CreateNewTag();
            string toTag = CallProperties.CreateNewTag();
            int cSeq = CallProperties.CreateNewCSeq();
            string callId = CallProperties.CreateNewCallId();
            logger.Debug("PtzContrl() start to PTZRequest.");
            SIPRequest ptzReq = PTZRequest(fromTag, toTag, cSeq, callId);
            string cmdStr = GetPtzCmd(ucommand, dwSpeed);

            PTZControl ptz = new PTZControl()
            {
                CommandType = CommandType.DeviceControl,
                DeviceID = DeviceId,
                SN = 11,//new Random().Next(9999),
                PTZCmd = cmdStr
            };
            string xmlBody = PTZControl.Instance.Save<PTZControl>(ptz);
            ptzReq.Body = xmlBody;
            //logger.Debug("PtzContrl() start to SendRequest...");
            _sipMsgCoreService.SendRequest(RemoteEndPoint, ptzReq);

        }

        /// <summary>
        /// PTZ云台控制请求
        /// </summary>
        /// <returns></returns>
        private SIPRequest PTZRequest(string fromTag, string toTag, int cSeq, string callId)
        {
            SIPURI remoteUri = new SIPURI(DeviceId, RemoteEndPoint.ToHost(), "");
            SIPURI localUri = new SIPURI(_sipMsgCoreService.LocalSIPId, _sipMsgCoreService.LocalEP.ToHost(), "");
            SIPFromHeader from = new SIPFromHeader(null, localUri, fromTag);
            SIPToHeader to = new SIPToHeader(null, remoteUri, toTag);
            SIPRequest ptzReq = _sipTransport.GetRequest(SIPMethodsEnum.MESSAGE, remoteUri);
            //logger.Debug("PtzContrl() .PTZRequest() .GetRequest() ended.");
            ptzReq.Header.From = from;
            ptzReq.Header.Contact = null;
            ptzReq.Header.Allow = null;
            ptzReq.Header.To = to;
            ptzReq.Header.UserAgent = SIPConstants.SIP_USERAGENT_STRING;
            ptzReq.Header.CSeq = cSeq;
            ptzReq.Header.CallId = callId;
            ptzReq.Header.ContentType = "application/MANSCDP+xml";
            return ptzReq;
        }


        /// <summary>
        /// 拼接ptz控制指令
        /// </summary>
        /// <param name="ucommand">控制命令</param>
        /// <param name="dwSpeed">速度</param>
        /// <returns></returns>
        private string GetPtzCmd(PTZCommand ucommand, int dwSpeed)
        {
            List<int> cmdList = new List<int>(8)
            {
                0xA5,
                0x0F,
                0x01
            };
            switch (ucommand)
            {
                case PTZCommand.Stop:
                    cmdList.Add(00);
                    cmdList.Add(00);
                    cmdList.Add(00);
                    cmdList.Add(00);
                    break;
                case PTZCommand.Up:
                    cmdList.Add(0x08);
                    cmdList.Add(dwSpeed);
                    cmdList.Add(dwSpeed);
                    cmdList.Add(00);
                    break;
                case PTZCommand.Down:
                    cmdList.Add(0x04);
                    cmdList.Add(dwSpeed);
                    cmdList.Add(dwSpeed);
                    cmdList.Add(00);
                    break;
                case PTZCommand.Left:
                    cmdList.Add(0x02);
                    cmdList.Add(dwSpeed);
                    cmdList.Add(dwSpeed);
                    cmdList.Add(00);
                    break;
                case PTZCommand.Right:
                    cmdList.Add(0x01);
                    cmdList.Add(dwSpeed);
                    cmdList.Add(dwSpeed);
                    cmdList.Add(00);
                    break;
                case PTZCommand.UpRight:
                    cmdList.Add(0x09);
                    cmdList.Add(dwSpeed);
                    cmdList.Add(dwSpeed);
                    cmdList.Add(00);
                    break;
                case PTZCommand.DownRight:
                    cmdList.Add(0x05);
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
                case PTZCommand.SetPreset: //设置预置位
                    cmdList.Add(0x81);
                    cmdList.Add(00);
                    cmdList.Add(dwSpeed);
                    cmdList.Add(00);
                    break;
                case PTZCommand.GetPreset:  //调用预置位
                    cmdList.Add(0x82);
                    cmdList.Add(00);
                    cmdList.Add(dwSpeed);
                    cmdList.Add(00);
                    break;
                case PTZCommand.RemovePreset:   //删除预置位
                    cmdList.Add(0x83);
                    cmdList.Add(00);
                    cmdList.Add(dwSpeed);
                    cmdList.Add(00);
                    break;
                default:
                    break;
            }

            int checkBit = 0;
            foreach (int cmdItem in cmdList)
            {
                checkBit += cmdItem;
            }
            checkBit = checkBit % 256;
            cmdList.Add(checkBit);

            string cmdStr = string.Empty;
            foreach (var cmdItemStr in cmdList)
            {
                cmdStr += cmdItemStr.ToString("X").PadLeft(2, '0');
            }
            return cmdStr;
        }
        #endregion

        #region 拉框放大/缩小
        public void DragZoomContrl(DragZoomSet zoom, bool isIn)
        {
            string fromTag = CallProperties.CreateNewTag();
            string toTag = CallProperties.CreateNewTag();
            int cSeq = CallProperties.CreateNewCSeq();
            string callId = CallProperties.CreateNewCallId();
            SIPRequest catalogReq = ZoomRequest(fromTag, toTag, cSeq, callId);

            DragZoom ptz = new DragZoom()
            {
                CommandType = CommandType.DeviceControl,
                DeviceID = DeviceId,
                SN = new Random().Next(9999)
            };
            if (isIn)
            {
                DragZoomSet DragZoomIn = new DragZoomSet()
                {
                    Width = 720,
                    Length = 1280,
                    MidPointX = 614,
                    MidPointY = 333,
                    LengthX = 841,
                    LengthY = 366
                };
                ptz.DragZoomIn = DragZoomIn;
            }
            else
            {
                DragZoomSet DragZoomOut = new DragZoomSet()
                {
                    Width = 720,
                    Length = 1280,
                    MidPointX = 731,
                    MidPointY = 426,
                    LengthX = 471,
                    LengthY = 323
                };
                ptz.DragZoomOut = DragZoomOut;
            }
            string xmlBody = DragZoom.Instance.Save<DragZoom>(ptz);
            catalogReq.Body = xmlBody;
            _sipMsgCoreService.SendRequest(RemoteEndPoint, catalogReq);

        }

        private SIPRequest ZoomRequest(string fromTag, string toTag, int cSeq, string callId)
        {
            SIPURI remoteUri = new SIPURI(DeviceId, RemoteEndPoint.ToHost(), "");
            SIPURI localUri = new SIPURI(_sipMsgCoreService.LocalSIPId, _sipMsgCoreService.LocalEP.ToHost(), "");
            SIPFromHeader from = new SIPFromHeader(null, localUri, fromTag);
            SIPToHeader to = new SIPToHeader(null, remoteUri, toTag);
            SIPRequest catalogReq = _sipTransport.GetRequest(SIPMethodsEnum.MESSAGE, remoteUri);
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
        #endregion

        #region 看守位控制
        /// <summary>
        /// 看守位设置 ture：开启 false：关闭
        /// </summary>
        public void HomePositionControl(bool isEnabled)
        {
            string fromTag = CallProperties.CreateNewTag();
            int cSeq = CallProperties.CreateNewCSeq();
            string callId = CallProperties.CreateNewCallId();

            SIPURI remoteUri = new SIPURI(DeviceId, RemoteEndPoint.ToHost(), "");
            SIPURI localUri = new SIPURI(_sipMsgCoreService.LocalSIPId, _sipMsgCoreService.LocalEP.ToHost(), "");
            SIPFromHeader from = new SIPFromHeader(null, localUri, fromTag);
            SIPToHeader to = new SIPToHeader(null, remoteUri, null);
            SIPRequest presetReq = _sipTransport.GetRequest(SIPMethodsEnum.MESSAGE, remoteUri);
            presetReq.Header.From = from;
            //presetReq.Header.Contact = null;
            presetReq.Header.Allow = null;
            presetReq.Header.To = to;
            presetReq.Header.UserAgent = SIPConstants.SIP_USERAGENT_STRING;
            presetReq.Header.CSeq = cSeq;
            presetReq.Header.CallId = callId;
            presetReq.Header.ContentType = "Application/MANSCDP+xml";
            HomePositionCmd cmd = new HomePositionCmd()
            {
                CommandType = CommandType.DeviceControl,
                DeviceID = DeviceId,
                SN = new Random().Next(9999),
                HomePosition = new HomePositionSet()
                {
                    Enabled = isEnabled ? 1 : 0,
                    ResetTime = 10,
                    PresetIndex = 24
                }
            };
            string xmlBody = HomePositionCmd.Instance.Save<HomePositionCmd>(cmd);
            presetReq.Body = xmlBody;
            _sipMsgCoreService.SendRequest(RemoteEndPoint, presetReq);
        }
        #endregion

        #region 其他功能

        /// <summary>
        /// 设备预置位查询
        /// </summary>
        public void DevicePresetQuery()
        {
            SIPRequest presetQuery = QueryItems(RemoteEndPoint, DeviceId);
            PresetQuery preset = new PresetQuery()
            {
                CmdType = CommandType.PresetQuery,
                DeviceID = DeviceId,
                SN = new Random().Next(1, ushort.MaxValue)
            };
            string xmlBody = PresetQuery.Instance.Save<PresetQuery>(preset);
            presetQuery.Body = xmlBody;
            _sipMsgCoreService.SendRequest(RemoteEndPoint, presetQuery);
        }

        /// <summary>
        /// 系统设备配置查询
        /// 查询配置参数类型，可查询的配置类型包括：
        /// 1，基本参数配置：BasicParam,
        /// 2，视频参数范围：VideoParamOpt
        /// 3，SVAC编码配置：SVACEncodeConfig
        /// 4，SVAC解码配置：SVACDecodeConfig
        /// 可同时查询多个配置类型，各类型以“/”分割
        /// 可返回与查询SN值相同的多个响应，每个响应对应一个配置类型
        /// <param name="configType">配置类型参数</param>
        /// </summary>
        public void DeviceConfigQuery(string configType)
        {
            SIPRequest stateReq = QueryItems(RemoteEndPoint, DeviceId);
            DeviceConfigType config = new DeviceConfigType()
            {
                CommandType = CommandType.ConfigDownload,
                DeviceID = DeviceId,
                SN = new Random().Next(1, ushort.MaxValue),
                ConfigType = configType
            };
            string xmlBody = DeviceConfigType.Instance.Save<DeviceConfigType>(config);
            stateReq.Body = xmlBody;
            _sipMsgCoreService.SendRequest(RemoteEndPoint, stateReq);
        }
        /// <summary>
        /// 状态查询
        /// </summary>
        /// <param name="deviceId">目的设备编码</param>
        public void DeviceStateQuery()
        {
            SIPRequest stateReq = QueryItems(RemoteEndPoint, DeviceId);
            CatalogQuery catalog = new CatalogQuery()
            {
                CommandType = CommandType.DeviceStatus,
                DeviceID = DeviceId,
                SN = new Random().Next(1, ushort.MaxValue)
            };
            string xmlBody = CatalogQuery.Instance.Save<CatalogQuery>(catalog);
            stateReq.Body = xmlBody;
            _sipMsgCoreService.SendRequest(RemoteEndPoint, stateReq);
        }

        /// <summary>
        /// 设备信息查询
        /// </summary>
        /// <param name="deviceId">目的设备编码</param>
        public void DeviceInfoQuery()
        {
            SIPRequest infoReq = QueryItems(RemoteEndPoint, DeviceId);
            CatalogQuery catalog = new CatalogQuery()
            {
                CommandType = CommandType.DeviceInfo,
                DeviceID = DeviceId,
                SN = new Random().Next(1, ushort.MaxValue)
            };
            string xmlBody = CatalogQuery.Instance.Save<CatalogQuery>(catalog);
            infoReq.Body = xmlBody;
            _sipMsgCoreService.SendRequest(RemoteEndPoint, infoReq);
        }

        /// <summary>
        /// 设备重启
        /// </summary>
        /// <param name="deviceId">目的设备编码</param>
        public void DeviceReboot()
        {
            SIPRequest rebootReq = QueryItems(RemoteEndPoint, DeviceId);
            Control catalog = new Control()
            {
                CommandType = CommandType.DeviceControl,
                DeviceID = DeviceId,
                SN = new Random().Next(1, ushort.MaxValue),
                TeleBoot = "Boot"
            };
            string xmlBody = Control.Instance.Save<Control>(catalog);
            rebootReq.Body = xmlBody;
            _sipMsgCoreService.SendRequest(RemoteEndPoint, rebootReq);
        }

        /// <summary>
        /// 事件订阅
        /// </summary>
        /// <param name="deviceId">目的设备编码</param>
        public void DeviceEventSubscribe()
        {
            SIPRequest eventSubscribeReq = QueryItems(RemoteEndPoint, DeviceId);
            eventSubscribeReq.Method = SIPMethodsEnum.SUBSCRIBE;
            CatalogQuery catalog = new CatalogQuery()
            {
                CommandType = CommandType.Alarm,
                DeviceID = DeviceId,
                SN = new Random().Next(1, ushort.MaxValue),
                StartAlarmPriority = "0",
                EndAlarmPriority = "0",
                AlarmMethod = "0",
                StartTime = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss"),
                EndTime = DateTime.Now.AddHours(10).ToString("yyyy-MM-ddTHH:mm:ss")
            };
            string xmlBody = CatalogQuery.Instance.Save<CatalogQuery>(catalog);
            eventSubscribeReq.Body = xmlBody;
            _sipMsgCoreService.SendRequest(RemoteEndPoint, eventSubscribeReq);
        }
        /// <summary>
        /// 报警订阅
        /// </summary>
        /// <param name="remoteEndPoint"></param>
        /// <param name="deviceid"></param>
        public void DeviceAlarmSubscribe(SIPEndPoint remoteEndPoint, string deviceid)
        {
            SIPRequest eventSubscribeReq = QueryItemsSubscribe(remoteEndPoint, deviceid);
            eventSubscribeReq.Method = SIPMethodsEnum.SUBSCRIBE;
            CatalogQuery catalog = new CatalogQuery()
            {
                CommandType = CommandType.Alarm,
                DeviceID = deviceid,
                SN = new Random().Next(1, ushort.MaxValue),
                StartAlarmPriority = "1",
                EndAlarmPriority = "4",
                AlarmMethod = "0",
                StartTime = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss"),
                EndTime = DateTime.Now.AddHours(10).ToString("yyyy-MM-ddTHH:mm:ss")
            };
            string xmlBody = CatalogQuery.Instance.Save<CatalogQuery>(catalog);
            eventSubscribeReq.Body = xmlBody;
            _sipMsgCoreService.SendRequest(remoteEndPoint, eventSubscribeReq);
        }

        /// <summary>
        /// 目录订阅
        /// </summary>
        public void DeviceCatalogSubscribe(bool isStop)
        {
        }

        /// <summary>
        /// 布防
        /// </summary>
        /// <param name="deviceId">目的设备编码</param>
        public void DeviceControlSetGuard()
        {
            SIPRequest catalogReq = QueryItems(RemoteEndPoint, DeviceId);
            Control catalog = new Control()
            {
                CommandType = CommandType.DeviceControl,
                DeviceID = DeviceId,
                SN = new Random().Next(1, ushort.MaxValue),
                GuardCmd = "SetGuard"
            };
            string xmlBody = Control.Instance.Save<Control>(catalog);
            catalogReq.Body = xmlBody;
            _sipMsgCoreService.SendRequest(RemoteEndPoint, catalogReq);
        }

        /// <summary>
        /// 撤防
        /// </summary>
        /// <param name="deviceId">目的设备编码</param>
        public void DeviceControlResetGuard()
        {
            SIPRequest catalogReq = QueryItems(RemoteEndPoint, DeviceId);
            Control catalog = new Control()
            {
                CommandType = CommandType.DeviceControl,
                DeviceID = DeviceId,
                SN = new Random().Next(1, ushort.MaxValue),
                GuardCmd = "ResetGuard"
            };
            string xmlBody = Control.Instance.Save<Control>(catalog);
            catalogReq.Body = xmlBody;
            _sipMsgCoreService.SendRequest(RemoteEndPoint, catalogReq);
        }

        /// <summary>
        /// 报警应答
        /// </summary>
        /// <param name="alarm"></param>
        public void AlarmResponse(Alarm alarm)
        {
            SIPRequest catalogReq = QueryItems(RemoteEndPoint, DeviceId);
            AlarmInfo alarmInfo = new AlarmInfo()
            {
                CmdType = CommandType.Alarm,
                DeviceID = alarm.DeviceID,
                SN = alarm.SN,
                Result = "OK"
            };
            string xmlBody = Control.Instance.Save<AlarmInfo>(alarmInfo);
            catalogReq.Body = xmlBody;
            _sipMsgCoreService.SendRequest(RemoteEndPoint, catalogReq);
        }

        /// <summary>
        /// 报警复位
        /// </summary>
        /// <param name="deviceId">目的设备编码</param>
        public void DeviceControlResetAlarm()
        {
            SIPRequest catalogReq = QueryItems(RemoteEndPoint, DeviceId);
            Control catalog = new Control()
            {
                CommandType = CommandType.DeviceControl,
                DeviceID = DeviceId,
                SN = new Random().Next(1, ushort.MaxValue),
                AlarmCmd = "ResetAlarm"
            };
            string xmlBody = Control.Instance.Save<Control>(catalog);
            catalogReq.Body = xmlBody;
            _sipMsgCoreService.SendRequest(RemoteEndPoint, catalogReq);
        }
        /// <summary>
        /// 报警复位
        /// </summary>
        /// <param name="remoteEndPoint"></param>
        /// <param name="deviceid"></param>
        public void DeviceControlResetAlarm(SIPEndPoint remoteEndPoint, string deviceid)
        {
            SIPRequest catalogReq = QueryItems(remoteEndPoint, deviceid);
            Control catalog = new Control()
            {
                CommandType = CommandType.DeviceControl,
                DeviceID = DeviceId,
                SN = new Random().Next(1, ushort.MaxValue),
                AlarmCmd = "ResetAlarm"
            };
            string xmlBody = Control.Instance.Save<Control>(catalog);
            catalogReq.Body = xmlBody;
            _sipMsgCoreService.SendRequest(remoteEndPoint, catalogReq);
        }

        /// <summary>
        /// 查询请求
        /// </summary>
        /// <returns></returns>
        private SIPRequest QueryItems(SIPEndPoint remoteEndPoint, string remoteSIPId)
        {
            string fromTag = CallProperties.CreateNewTag();
            string toTag = CallProperties.CreateNewTag();
            int cSeq = CallProperties.CreateNewCSeq();
            string callId = CallProperties.CreateNewCallId();

            SIPURI remoteUri = new SIPURI(remoteSIPId, remoteEndPoint.ToHost(), "");
            SIPURI localUri = new SIPURI(_sipMsgCoreService.LocalSIPId, _sipMsgCoreService.LocalEP.ToHost(), "");
            SIPFromHeader from = new SIPFromHeader(null, localUri, fromTag);
            SIPToHeader to = new SIPToHeader(null, remoteUri, null);
            SIPRequest queryReq = _sipTransport.GetRequest(SIPMethodsEnum.MESSAGE, remoteUri);
            queryReq.Header.From = from;
            //queryReq.Header.Contact = null;
            queryReq.Header.Allow = null;
            queryReq.Header.To = to;
            queryReq.Header.UserAgent = SIPConstants.SIP_USERAGENT_STRING;
            queryReq.Header.CSeq = cSeq;
            queryReq.Header.CallId = callId;//_reqSession.Header.CallId; //
            queryReq.Header.ContentType = "Application/MANSCDP+xml";

            return queryReq;
        }
        /// <summary>
        /// 订阅请求
        /// </summary>
        /// <param name="remoteEndPoint"></param>
        /// <param name="remoteSIPId"></param>
        /// <returns></returns>
        private SIPRequest QueryItemsSubscribe(SIPEndPoint remoteEndPoint, string remoteSIPId)
        {
            string fromTag = CallProperties.CreateNewTag();
            string toTag = CallProperties.CreateNewTag();
            int cSeq = CallProperties.CreateNewCSeq();
            string callId = CallProperties.CreateNewCallId();

            SIPURI remoteUri = new SIPURI(remoteSIPId, remoteEndPoint.ToHost(), "");
            SIPURI localUri = new SIPURI(_sipMsgCoreService.LocalSIPId, _sipMsgCoreService.LocalEP.ToHost(), "");
            SIPFromHeader from = new SIPFromHeader(null, localUri, fromTag);
            SIPToHeader to = new SIPToHeader(null, remoteUri, null);
            SIPRequest queryReq = _sipTransport.GetRequest(SIPMethodsEnum.SUBSCRIBE, remoteUri);
            queryReq.Header.From = from;
            queryReq.Header.Contact = new List<SIPContactHeader>
            {
                new SIPContactHeader(null, localUri)
            };
            queryReq.Header.Allow = null;
            queryReq.Header.To = to;
            queryReq.Header.UserAgent = SIPConstants.SIP_USERAGENT_STRING;
            queryReq.Header.CSeq = cSeq;
            queryReq.Header.Expires = 90;
            queryReq.Header.Event = "presence";
            queryReq.Header.CallId = callId;
            queryReq.Header.ContentType = "Application/MANSCDP+xml";

            return queryReq;
        }
        #endregion

        #region  语音广播通知

        //语音广播通知
        public void AudioPublishNotify()
        {
            //_mediaPort = _msgCore.SetMediaPort();
            //string localIp = _msgCore.LocalEP.Address.ToString();
            //string fromTag = CallProperties.CreateNewTag();
            //int cSeq = CallProperties.CreateNewCSeq();
            //string callId = CallProperties.CreateNewCallId();

            //SIPURI remoteUri = new SIPURI(_deviceId, _remoteEP.ToHost(), "");
            //SIPURI localUri = new SIPURI(_msgCore.LocalSIPId, _msgCore.LocalEP.ToHost(), "");
            //SIPFromHeader from = new SIPFromHeader(null, localUri, fromTag);
            //SIPToHeader to = new SIPToHeader(null, remoteUri, null);
            //SIPRequest realReq = _msgCore.Transport.GetRequest(SIPMethodsEnum.INVITE, remoteUri);
            //SIPContactHeader contactHeader = new SIPContactHeader(null, localUri);
            //realReq.Header.Contact.Clear();
            //realReq.Header.Contact.Add(contactHeader);

            //realReq.Header.Allow = null;
            //realReq.Header.From = from;
            //realReq.Header.To = to;
            //realReq.Header.UserAgent = SIPConstants.SIP_USERAGENT_STRING;
            //realReq.Header.CSeq = cSeq;
            //realReq.Header.CallId = callId;
            //realReq.Header.Subject = SetSubject();
            //realReq.Header.ContentType = "application/sdp";

            //realReq.Body = SetMediaReq(localIp, _mediaPort);
            //_msgCore.SendReliableRequest(_remoteEP, realReq);
            //_reqSession = realReq;


            SIPRequest catalogReq = QueryItems(RemoteEndPoint, DeviceId);
            VoiceBroadcastNotify nty = new VoiceBroadcastNotify
            {
                CmdType = CommandType.Broadcast,
                SN = new Random().Next(1, ushort.MaxValue),
                SourceID = _sipAccount.SIPPassword,
                TargetID = DeviceId
            };
            string xmlBody = Control.Instance.Save<VoiceBroadcastNotify>(nty);
            catalogReq.Body = xmlBody;
            _sipMsgCoreService.SendRequest(RemoteEndPoint, catalogReq);

        }

        private string callId = string.Empty;
        private string toTag;

        public void Subscribe(SIPResponse response)
        {
            _fromTag = response.Header.From.FromTag;
            _vvia = response.Header.Vias;
            toTag = response.Header.To.ToTag;
        }


        private string _fromTag = string.Empty;
        int cSeq = CallProperties.CreateNewCSeq();
        SIPViaSet _vvia;
        //  private int sn = 1;
        /// <summary>
        /// 移动设备位置订阅
        /// </summary>
        /// <param name="interval">移动设备位置信息上报时间间隔</param>
        /// <param name="isStop">true订阅/false取消订阅</param>
        public void MobilePositionQueryRequest(int interval, bool isStop)
        {
            // code conflict

        }

        public void PositioninfoSubNotify()
        {
            //_mediaPort = _msgCore.SetMediaPort();
            //string localIp = _msgCore.LocalEP.Address.ToString();
            //string fromTag = CallProperties.CreateNewTag();
            //int cSeq = CallProperties.CreateNewCSeq();
            //string callId = CallProperties.CreateNewCallId();

            //SIPURI remoteUri = new SIPURI(_deviceId, _remoteEP.ToHost(), "");
            //SIPURI localUri = new SIPURI(_msgCore.LocalSIPId, _msgCore.LocalEP.ToHost(), "");
            //SIPFromHeader from = new SIPFromHeader(null, localUri, fromTag);
            //SIPToHeader to = new SIPToHeader(null, remoteUri, null);
            //SIPRequest realReq = _msgCore.Transport.GetRequest(SIPMethodsEnum.INVITE, remoteUri);
            //SIPContactHeader contactHeader = new SIPContactHeader(null, localUri);
            //realReq.Header.Contact.Clear();
            //realReq.Header.Contact.Add(contactHeader);

            //realReq.Header.Allow = null;
            //realReq.Header.From = from;
            //realReq.Header.To = to;
            //realReq.Header.UserAgent = SIPConstants.SIP_USERAGENT_STRING;
            //realReq.Header.CSeq = cSeq;
            //realReq.Header.CallId = callId;
            //realReq.Header.Subject = SetSubject();
            //realReq.Header.ContentType = "application/sdp";

            //realReq.Body = SetMediaReq(localIp, _mediaPort);
            //_msgCore.SendReliableRequest(_remoteEP, realReq);
            //_reqSession = realReq;


            SIPRequest catalogReq = QueryItems(RemoteEndPoint, DeviceId);
            PositionInfoSubNotify nty = new PositionInfoSubNotify
            {
                CmdType = CommandType.MobilePosition,
                DeviceID = DeviceId,
                SN = new Random().Next(1, ushort.MaxValue),
            };
            string xmlBody = Control.Instance.Save(nty);
            catalogReq.Body = xmlBody;
            _sipMsgCoreService.SendRequest(RemoteEndPoint, catalogReq);

        }
        #endregion

        #region 设备配置

        /// <summary>
        /// UTF8转换成GB2312
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static string Gb2312_gbk(string text)
        {
            //声明字符集   
            System.Text.Encoding gbk, gb2312;
            //utf8   
            gbk = System.Text.Encoding.GetEncoding("gb2312");
            //gb2312   
            gb2312 = System.Text.Encoding.GetEncoding("gbk");
            byte[] utf;
            utf = gbk.GetBytes(text);
            utf = System.Text.Encoding.Convert(gbk, gb2312, utf);
            //返回转换后的字符   
            return gb2312.GetString(utf);
        }
        /// 设备配置
        /// </summary>
        /// <param name="devName">设备名称</param>
        /// <param name="expiration">注册过期时间</param>
        /// <param name="hearBeatInterval">心跳间隔时间</param>
        /// <param name="heartBeatCount">心跳超时次数</param>
        public void DeviceConfig(string devName, int expiration, int hearBeatInterval, int heartBeatCount)
        {
            SIPRequest configReq = QueryItems(RemoteEndPoint, DeviceId);

            DeviceConfig config = new DeviceConfig()
            {
                CommandType = CommandType.DeviceConfig,
                DeviceID = DeviceId,
                SN = new Random().Next(1, ushort.MaxValue),
                BasicParam = new DeviceParam()
                {
                    Name = devName,
                    Expiration = expiration,
                    HeartBeatInterval = hearBeatInterval,
                    HeartBeatCount = heartBeatCount
                }
            };
            string xmlBody = SIPSorcery.GB28181.Sys.XML.DeviceConfig.Instance.Save<DeviceConfig>(config);
            configReq.Body = xmlBody;
            _sipMsgCoreService.SendRequest(RemoteEndPoint, configReq);
        }
        #endregion

        public void MakeKeyFrameRequest()
        {
            string fromTag = CallProperties.CreateNewTag();
            int cSeq = CallProperties.CreateNewCSeq();
            string callId = CallProperties.CreateNewCallId();

            SIPURI remoteUri = new SIPURI(DeviceId, RemoteEndPoint.ToHost(), "");
            SIPURI localUri = new SIPURI(_sipMsgCoreService.LocalSIPId, _sipMsgCoreService.LocalEP.ToHost(), "");
            SIPFromHeader from = new SIPFromHeader(null, localUri, fromTag);
            SIPToHeader to = new SIPToHeader(null, remoteUri, null);
            SIPRequest presetReq = _sipTransport.GetRequest(SIPMethodsEnum.MESSAGE, remoteUri);
            presetReq.Header.From = from;
            presetReq.Header.Contact = null;
            presetReq.Header.Allow = null;
            presetReq.Header.To = to;
            presetReq.Header.UserAgent = SIPConstants.SIP_USERAGENT_STRING;
            presetReq.Header.CSeq = cSeq;
            presetReq.Header.CallId = callId;
            presetReq.Header.ContentType = "application/MANSCDP+xml";
            KeyFrameCmd cmd = new KeyFrameCmd()
            {
                CommandType = CommandType.DeviceControl,
                DeviceID = DeviceId,
                SN = new Random().Next(9999),
                IFameCmd = "Send"
            };
            string xmlBody = KeyFrameCmd.Instance.Save<KeyFrameCmd>(cmd);
            presetReq.Body = xmlBody;
            _sipMsgCoreService.SendRequest(RemoteEndPoint, presetReq);
        }

        //Wait Result of the Operation
        public Tuple<SIPRequest, SIPResponse> WaitRequestResult()
        {
            _autoResetEventForRpc.WaitOne();
            _syncResponseContext.TryDequeue(out Tuple<SIPRequest, SIPResponse> sipResult);
            return sipResult;
        }
    }
}
