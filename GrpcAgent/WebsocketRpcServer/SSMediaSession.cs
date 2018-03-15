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
            ///TODO ....
            return base.KeepAlive(request, context);
        }



        public override Task<StartLiveReply> StartLive(StartLiveRequest request, ServerCallContext context)
        {

            // var restult = _sipServiceDirector.MakeVideoRequest(request.Gbid, new int[] { request.Port }, request.Ipaddr);
            var reqeustProcessRsult = Task.Factory.StartNew(() =>
                        {

                            _eventSource?.FireLivePlayRequestEvent(request, context);
                            var resultMsg = _sipServiceDirector.MakeVideoRequest(request.Gbid, new int[] { request.Port }, request.Ipaddr);

                            //get the response .
                            var resReply = new StartLiveReply()
                            {
                                Ipaddr = resultMsg.Item1,
                                Port = resultMsg.Item2,
                                Hdr = request.Hdr,

                                Status = new MediaContract.Status()
                                {
                                    Code = 200,
                                    Msg = "Request Successful!"
                                }

                            };

                            return resReply;

                        });

            reqeustProcessRsult.Wait(System.TimeSpan.FromSeconds(2));

            return reqeustProcessRsult;

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
            return base.Stop(request, context);
        }
    }
}
