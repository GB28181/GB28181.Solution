using Grpc.Core;
using MediaContract;
using System.Threading.Tasks;
namespace GrpcAgent.WebsocketRpcServer
{
    public class SSMediaSessionImpl : VideoSession.VideoSessionBase
    {

        private MediaEventSource _eventSource = null;

        public SSMediaSessionImpl(MediaEventSource eventSource)
        {
            _eventSource = eventSource;
        }

        public override Task<KeepAliveReply> KeepAlive(KeepAliveRequest request, ServerCallContext context)
        {
            ///TODO ....
            return base.KeepAlive(request, context);
        }

 

        public override Task<StartLiveReply> StartLive(StartLiveRequest request, ServerCallContext context)
        {
            _eventSource?.FireLivePlayRequestEvent(request, context);
            return base.StartLive(request, context);
        }

        public override Task<StartPlaybackReply> StartPlayback(StartPlaybackRequest request, ServerCallContext context)
        {
            if (request.IsDownload)
            {
                _eventSource?.FireDownloadRequestEvent(request, context);
            }

            return base.StartPlayback(request, context);
        }

        public override Task<StopReply> Stop(StopRequest request, ServerCallContext context)
        {
            return base.Stop(request, context);
        }
    }
}
