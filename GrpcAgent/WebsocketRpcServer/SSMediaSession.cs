using Grpc.Core;
using MediaContract;
using SIPSorcery.GB28181.Servers.SIPMessage;
using System.Threading.Tasks;
namespace GrpcAgent.WebsocketRpcServer
{
    public class SSMediaSessionImpl : VideoSession.VideoSessionBase
    {

        private MediaEventSource _eventSource = null;
        private SIPCoreMessageService _sipCoreMessageService = null;

        public SSMediaSessionImpl(MediaEventSource eventSource, SIPCoreMessageService sipCoreMessageService)
        {
            _eventSource = eventSource;
            _sipCoreMessageService = sipCoreMessageService;
        }

        public override Task<KeepAliveReply> KeepAlive(KeepAliveRequest request, ServerCallContext context)
        {
            ///TODO ....
            return base.KeepAlive(request, context);
        }



        public override Task<StartLiveReply> StartLive(StartLiveRequest request, ServerCallContext context)
        {
            _eventSource?.FireLivePlayRequestEvent(request, context);

            var result = new StartLiveReply()
            {
                Ipaddr = "127.0.0.1",
                Port = 50005
            };

            //_sipCoreMessageService.MonitorService[request.Gbid]

            return base.StartLive(request, context);
        }

        public override Task<StartPlaybackReply> StartPlayback(StartPlaybackRequest request, ServerCallContext context)
        {
            if (request.IsDownload)
            {
                _eventSource?.FireDownloadRequestEvent(request, context);
            }
            else
            {
                _eventSource?.FirePlaybackRequestEvent(request, context);
            }

            return base.StartPlayback(request, context);
        }

        public override Task<StopReply> Stop(StopRequest request, ServerCallContext context)
        {
            return base.Stop(request, context);
        }
    }
}
