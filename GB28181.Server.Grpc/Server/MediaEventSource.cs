using Grpc.Core;
using GB28181.Server.Grpc.Protos.Video;

namespace GB28181.Server.Grpc
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
    

    public class MediaEventSource
    {
        public event LivePlayRequestHandler LivePlayRequestReceived = null;
        public event PlaybackRequestHandler PlaybackRequestReceived = null;

        public void FileLivePlayRequestEvent(StartLiveRequest request, ServerCallContext context)
        {
            LivePlayRequestReceived?.Invoke(request, context);
        }

        public void FilePlaybackRequestEvent(StartPlaybackRequest request, ServerCallContext context)
        {
            PlaybackRequestReceived?.Invoke(request, context);
        }
    }
}
