using SIPSorcery.GB28181.Net;
using SIPSorcery.GB28181.Servers.SIPMessage;
using SIPSorcery.GB28181.Servers.SIPMonitor;
using SIPSorcery.GB28181.SIP;
using SIPSorcery.GB28181.Sys.XML;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIPSorcery.GB28181.Servers
{
    /// <summary>
    /// 监控服务统一接口
    /// </summary>
    public interface ISIPMonitorService
    {
        /// <summary>
        /// 实时视频请求
        /// </summary>
        void RealVideoReq();

        /// <summary>
        /// 取消实时视频请求
        /// </summary>
        void ByeVideoReq();

        /// <summary>
        /// 确认接收实时视频请求
        /// </summary>
        /// <param name="toTag">ToTag</param>
        /// <returns>sip请求</returns>
        void AckRequest(string toTag,string ip,int port);

        void Subscribe(SIPResponse ponse);

        /// <summary>
        /// 停止监控服务
        /// </summary>
        void Stop();

        /// <summary>
        /// 视频流回调完成
        /// </summary>
        event Action<RTPFrame> OnStreamReady;

        #region 录像点播
        /// <summary>
        /// 录像文件查询结果
        /// </summary>
        /// <param name="recordTotal">录像条数</param>
        void RecordQueryTotal(int recordTotal);

        /// <summary>
        /// 录像文件检索
        /// <param name="beginTime">开始时间</param>
        /// <param name="endTime">结束时间</param>
        /// </summary>
        int RecordFileQuery(DateTime beginTime, DateTime endTime,string type);

        /// <summary>
        /// 录像点播视频请求
        /// </summary>
        /// <param name="beginTime">开始时间</param>
        /// <param name="endTime">结束时间</param>
        void BackVideoReq(DateTime beginTime, DateTime endTime);

        /// <summary>
        /// 录像文件下载请求
        /// </summary>
        /// <param name="beginTime">开始时间</param>
        /// <param name="endTime">结束时间</param>
        void VideoDownloadReq(DateTime beginTime, DateTime endTime);

        /// <summary>
        /// 录像点播视频播放速度控制请求
        /// </summary>
        /// <param name="scale">播放快进比例</param>
        /// <param name="range">视频播放时间段</param>
        bool BackVideoPlaySpeedControlReq(string range);

        /// <summary>
        /// 控制录像随机拖拽
        /// </summary>
        /// <param name="range">时间范围</param>
        bool BackVideoPlayPositionControlReq(int range);
        /// <summary>
        /// 录像点播视频继续播放控制请求
        /// </summary>
        void BackVideoContinuePlayingControlReq();
        /// <summary>
        /// 录像点播视频暂停控制请求
        /// </summary>
        void BackVideoPauseControlReq();
        /// <summary>
        /// 录像点播视频停止播放控制请求
        /// </summary>
        void BackVideoStopPlayingControlReq(); 
        #endregion

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
