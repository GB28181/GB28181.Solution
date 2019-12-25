using SIPSorcery.GB28181.Net;
using SIPSorcery.GB28181.Servers.SIPMonitor;
using SIPSorcery.GB28181.SIP;
using SIPSorcery.GB28181.Sys.XML;
using System;
using System.Collections.Concurrent;

namespace SIPSorcery.GB28181.Servers.SIPMessage
{
    public interface ISipMessageCore
    {

        SIPEndPoint LocalEP { get; set; }

        string LocalSIPId { get; set; }

        ISIPTransport Transport { get; }

        ConcurrentDictionary<string, ISIPMonitorCore> NodeMonitorService { get; }

        void Start();
        void Stop();

        int[] SetMediaPort();

        void SendReliableRequest(SIPEndPoint remoteEP, SIPRequest request);

        void SendRequest(SIPEndPoint remoteEP, SIPRequest request);

        string GetReceiveIP(string content);

        int GetReceivePort(string content, SDPMediaTypesEnum sDPMediaTypes);

        void AddMessageRequest(SIPEndPoint localEP, SIPEndPoint remoteEP, SIPRequest request);

        void AddMessageResponse(SIPEndPoint localEP, SIPEndPoint remoteEP, SIPResponse response);

        void PtzControl(PTZCommand ptzcmd, int dwSpeed, string deviceId);
        void DeviceStateQuery(string deviceId);

        void DeviceCatalogQuery();
        void DeviceCatalogQuery(string deviceId);
        void DeviceCatalogSubscribe(string deviceId);
        int RecordFileQuery(string deviceId, DateTime startTime, DateTime endTime, string type);


        #region 事件
        /// <summary>
        /// sip服务状态
        /// </summary>
        event Action<string, ServiceStatus> OnServiceChanged;

        /// <summary>
        /// 录像文件接收
        /// </summary>
        event Action<RecordInfo> OnRecordInfoReceived;

        /// <summary>
        /// 设备目录接收
        /// </summary>
        event Action<Catalog> OnCatalogReceived;

        /// <summary>
        /// 设备目录通知
        /// </summary>
        event Action<NotifyCatalog> OnNotifyCatalogReceived;

        /// <summary>
        /// 语音广播通知
        /// </summary>
        event Action<VoiceBroadcastNotify> OnVoiceBroadcaseReceived;

        /// <summary>
        /// 报警通知
        /// </summary>
        event Action<Alarm> OnAlarmReceived;

        /// <summary>
        /// 平台之间心跳接收
        /// </summary>
        event Action<SIPEndPoint, KeepAlive, string> OnKeepaliveReceived;
        /// <summary>
        /// 设备状态查询接收
        /// </summary>
        event Action<SIPEndPoint, DeviceStatus> OnDeviceStatusReceived;
        /// <summary>
        /// 设备信息查询接收
        /// </summary>
        event Action<SIPEndPoint, DeviceInfo> OnDeviceInfoReceived;

        /// <summary>
        /// 设备配置查询接收
        /// </summary>
        event Action<SIPEndPoint, DeviceConfigDownload> OnDeviceConfigDownloadReceived;
        /// <summary>
        /// 历史媒体发送结束接收
        /// </summary>
        event Action<SIPEndPoint, MediaStatus> OnMediaStatusReceived;
        /// <summary>
        /// 响应状态码接收
        /// </summary>
        event Action<SIPResponseStatusCodesEnum, string, SIPEndPoint> OnResponseCodeReceived;

        /// <summary>
        /// 预置位查询接收
        /// </summary>
        event Action<SIPEndPoint, PresetInfo> OnPresetQueryReceived;

        #endregion



    }
}
