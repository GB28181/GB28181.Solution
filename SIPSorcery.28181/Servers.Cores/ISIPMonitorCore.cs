using SIPSorcery.GB28181.Servers.SIPMonitor;
using SIPSorcery.GB28181.SIP;
using SIPSorcery.GB28181.Sys.XML;

namespace SIPSorcery.GB28181.Servers
{

    /// <summary>
    /// 监控服务统一接口
    /// </summary>
    public interface ISIPMonitorCore :IMediaAction
    {

        string DeviceId { get; set; }
        SIPEndPoint RemoteEndPoint { get; set; }

        void Subscribe(SIPResponse ponse);

        /// <summary>
        /// 停止监控服务
        /// </summary>
        void Stop();


        /// <summary>
        /// PTZ云台控制
        /// </summary>
        /// <param name="ucommand">控制命令</param>
        /// <param name="dwSpeed">速度</param>
        void PtzContrl(PTZCommand ucommand, int dwSpeed);

        /// <summary>
        /// 设备状态查询
        /// </summary>
        void DeviceStateQuery();

        /// <summary>
        /// 设备信息查询
        /// </summary>
        void DeviceInfoQuery();

        /// <summary>
        /// 设备重新启动
        /// </summary>
        void DeviceReboot();

        /// <summary>
        /// 事件订阅
        /// </summary>
        void DeviceEventSubscribe();
        /// <summary>
        /// 报警订阅
        /// </summary>
        /// <param name="remoteEndPoint"></param>
        /// <param name="deviceid"></param>
        void DeviceAlarmSubscribe(SIPEndPoint remoteEndPoint, string deviceid);

        /// <summary>
        /// 目录订阅
        /// </summary>
        void DeviceCatalogSubscribe(bool isStop);

        /// <summary>
        /// 布防
        /// </summary>
        void DeviceControlSetGuard();

        /// <summary>
        /// 撤防
        /// </summary>
        void DeviceControlResetGuard();

        /// <summary>
        /// 报警复位
        /// </summary>
        void DeviceControlResetAlarm();
        void DeviceControlResetAlarm(SIPEndPoint remoteEndPoint, string deviceid);

        /// <summary>
        /// 报警应答
        /// </summary>
        /// <param name="alarm"></param>
        void AlarmResponse(Alarm alarm);

        /// <summary>
        /// 拉框放大/缩小
        /// </summary>
        /// <param name="zoom"></param>
        /// <param name="isIn"></param>
        void DragZoomContrl(DragZoomSet zoom, bool isIn);

        /// <summary>
        /// 看守位设置 ture：开启 false：关闭
        /// </summary>
        void HomePositionControl(bool isEnabled);

        /// <summary>
        /// 停止/开始录像
        /// <param name="isRecord">true开始录像/false停止录像</param>
        /// </summary>
        void DeviceControlRecord(bool isRecord);

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
        void DeviceConfigQuery(string configType);

        /// <summary>
        /// 设备配置
        /// </summary>
        /// <param name="devName">设备名称</param>
        /// <param name="expiration">注册过期时间</param>
        /// <param name="hearBeatInterval">心跳间隔时间</param>
        /// <param name="heartBeatCount">心跳超时次数</param>
        void DeviceConfig(string devName, int expiration, int hearBeatInterval, int heartBeatCount);

        /// <summary>
        /// 设备预置位查询
        /// </summary>
        void DevicePresetQuery();


        void AudioPublishNotify();
        /// <summary>
        /// 移动设备位置订阅
        /// </summary>
        /// <param name="interval">移动设备位置信息上报时间间隔</param>
        /// <param name="isStop">true订阅/false取消订阅</param>
        void MobilePositionQueryRequest(int interval,bool isStop);

        //强制关键帧命令请求
        void MakeKeyFrameRequest();
    }

    /// <summary>
    /// SIP服务状态
    /// </summary>
    public enum ServiceStatus
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
}
