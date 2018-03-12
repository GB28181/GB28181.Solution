using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Grpc.Core;
using MediaSession;

namespace GrpcAgent.WebsocketRpcServer
{
  public  class SSMediaSessionImpl : VideoControl.VideoControlBase
    {
        public override Task<KeepAliveReply> KeepAlive(KeepAliveRequest request, ServerCallContext context)
        {
            ///TODO ....
            return base.KeepAlive(request, context);
        }

        public override Task<LivePlayReply> LivePlay(LivePlayRequest request, ServerCallContext context)
        {
            ///TODO ....
            return base.LivePlay(request, context);
        }

        public override Task<PlaybackReply> PlayBack(PlaybackRequest request, ServerCallContext context)
        {
            ///TODO ....
            return base.PlayBack(request, context);
        }
    }
}
