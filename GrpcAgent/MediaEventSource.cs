using Grpc.Core;
using MediaContract;

namespace GrpcAgent
{

    /// <summary>
    /// 实时视频播放事件处理
    /// </summary>
    /// <param name="msg"></param>
    /// <param name="state"></param>
    public delegate void LivePlayRequestHandler(StartLiveRequest request, ServerCallContext context);
    /// <summary>
    /// 视频回放播放事件处理
    /// </summary>
    /// <param name="cata"></param>
    public delegate void PlaybackRequestHandler(StartPlaybackRequest request, ServerCallContext context);
    /// <summary>
    /// 视频文件下载事件处理
    /// </summary>
    /// <param name="record"></param>
    public delegate void VideoDownloadRequestHandler(StartPlaybackRequest request, ServerCallContext context);



    public class MediaEventSource
    {

        public event LivePlayRequestHandler LivePlayRequestReceived = null;
        public event PlaybackRequestHandler PlaybackRequesReceived = null;
        public event PlaybackRequestHandler DownloadRequestReceived = null;


        public void FireLivePlayRequestEvent(StartLiveRequest request, ServerCallContext context)
        {
            LivePlayRequestReceived?.Invoke(request, context);
        }

        internal void FireDownloadRequestEvent(StartPlaybackRequest request, ServerCallContext context)
        {
            DownloadRequestReceived?.Invoke(request, context);
        }


        internal void FirePlaybackRequestEvent(StartPlaybackRequest request, ServerCallContext context)
        {
            PlaybackRequesReceived?.Invoke(request, context);
        }
    }



}
