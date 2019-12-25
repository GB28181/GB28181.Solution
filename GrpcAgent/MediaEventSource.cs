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
    

    public class MediaEventSource
    {
        public event LivePlayRequestHandler LivePlayRequestReceived = null;
        public event PlaybackRequestHandler PlaybackRequestReceived = null;

        internal void FileLivePlayRequestEvent(StartLiveRequest request, ServerCallContext context)
        {
            LivePlayRequestReceived?.Invoke(request, context);
        }
                
        internal void FilePlaybackRequestEvent(StartPlaybackRequest request, ServerCallContext context)
        {
            PlaybackRequestReceived?.Invoke(request, context);
        }
    }
}
