using SIPSorcery.GB28181.Net;
using SIPSorcery.GB28181.SIP;
using System;

namespace SIPSorcery.GB28181.Servers
{
    public interface IMediaAction
    {
        /// <summary>
        /// 实时视频请求
        /// </summary>
        int RealVideoReq(int[] mediaPort, string receiveIP, bool needResult);

        //if an operation need Result you can wait the Result by WaitRequestResult
        Tuple<SIPRequest, SIPResponse> WaitRequestResult();

        /// <summary>
        /// 取消实时视频请求
        /// </summary>
        void ByeVideoReq();
        void ByeVideoReq(string sessionid);

        /// <summary>
        /// 确认接收实时视频请求
        /// </summary>
        /// <param name="toTag">ToTag</param>
        /// <returns>sip请求</returns>
        void AckRequest(SIPResponse response);


        /// <summary>
        /// 视频流回调完成
        /// </summary>
       // event Action<RTPFrame> OnStreamReady;


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
        int RecordFileQuery(DateTime beginTime, DateTime endTime, string type);

        /// <summary>
        /// 录像点播视频请求
        /// </summary>
        /// <param name="beginTime">开始时间</param>
        /// <param name="endTime">结束时间</param>
        void BackVideoReq(DateTime beginTime, DateTime endTime);
        int BackVideoReq(ulong beginTime, ulong endTime, int[] mediaPort, string receiveIP, bool needResult = false);

        /// <summary>
        /// 录像文件下载请求
        /// </summary>
        /// <param name="beginTime">开始时间</param>
        /// <param name="endTime">结束时间</param>
        void VideoDownloadReq(DateTime beginTime, DateTime endTime);
        int VideoDownloadReq(DateTime beginTime, DateTime endTime, int[] mediaPort, string receiveIP, bool needResult = false);

        /// <summary>
        /// 录像点播视频播放速度控制请求
        /// </summary>
        /// <param name="scale">播放快进比例</param>
        /// <param name="range">视频播放时间段</param>
        bool BackVideoPlaySpeedControlReq(string range);
        bool BackVideoPlaySpeedControlReq(string sessionid, float scale);

        /// <summary>
        /// 控制录像随机拖拽
        /// </summary>
        /// <param name="range">时间范围</param>
        bool BackVideoPlayPositionControlReq(int range);
        bool BackVideoPlayPositionControlReq(string sessionid, long time);
        /// <summary>
        /// 录像点播视频继续播放控制请求
        /// </summary>
        void BackVideoContinuePlayingControlReq();
        bool BackVideoContinuePlayingControlReq(string sessionid);
        /// <summary>
        /// 录像点播视频暂停控制请求
        /// </summary>
        void BackVideoPauseControlReq();
        bool BackVideoPauseControlReq(string sessionid);
        /// <summary>
        /// 录像点播视频停止播放控制请求
        /// </summary>
        void BackVideoStopPlayingControlReq();
        bool BackVideoStopPlayingControlReq(string sessionid);
        #endregion



    }
}
