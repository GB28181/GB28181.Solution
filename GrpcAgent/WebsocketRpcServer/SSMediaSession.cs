using Grpc.Core;
using MediaContract;
using System.Threading.Tasks;
using SIPSorcery.GB28181.Servers;
namespace GrpcAgent.WebsocketRpcServer
{
    public class SSMediaSessionImpl : VideoSession.VideoSessionBase
    {

        private MediaEventSource _eventSource = null;
        private ISIPServiceDirector _sipServiceDirector = null;

        public SSMediaSessionImpl(MediaEventSource eventSource, ISIPServiceDirector sipServiceDirector)
        {
            _eventSource = eventSource;
            _sipServiceDirector = sipServiceDirector;
        }

        public override Task<KeepAliveReply> KeepAlive(KeepAliveRequest request, ServerCallContext context)
        {
            var keepAliveReply = new KeepAliveReply()
            {
                Status = new MediaContract.Status()
                {
                    Code = 200,
                    Msg = "KeepAlive Successful!"
                }

            };

            return Task.FromResult(keepAliveReply);
        }


        public override Task<StartLiveReply> StartLive(StartLiveRequest request, ServerCallContext context)
        {
            // var restult = _sipServiceDirector.MakeVideoRequest(request.Gbid, new int[] { request.Port }, request.Ipaddr);
            _eventSource?.FireLivePlayRequestEvent(request, context);
            var reqeustProcessResult = _sipServiceDirector.MakeVideoRequest(request.Gbid, new int[] { request.Port }, request.Ipaddr);

            reqeustProcessResult?.Wait(System.TimeSpan.FromSeconds(1));

            //get the response .
            var resReply = new StartLiveReply()
            {
                // Ipaddr = reqeustProcessResult.Result.Item1,
                //Port = reqeustProcessResult.Result.Item2,
                Ipaddr = "0000000",
                Port = 000000,
                Hdr = request.Hdr,

                Status = new MediaContract.Status()
                {
                    Code = 200,
                    Msg = "Request Successful!"
                }

            };

            return Task.FromResult(resReply);

            //  reqeustProcessResult.Wait(System.TimeSpan.FromSeconds(2));

            //  return reqeustProcessRsult;
            //_sipCoreMessageService.MonitorService[request.Gbid]
            // return base.StartLive(request, context);
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
            // return base.Stop(request, context);

            var stopReply = new StopReply()
            {
                Status = new MediaContract.Status()
                {
                    Code = 200,
                    Msg = "Stop Successful!"
                }

            };

            return Task.FromResult(stopReply);
        }
    }
}
