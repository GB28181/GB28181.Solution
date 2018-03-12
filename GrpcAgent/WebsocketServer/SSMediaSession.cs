using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Grpc.Core;
using MediaSession;

namespace GrpcAgent.WebsocketServer
{
    class SSMediaSessionImpl : VideoControl.VideoControlBase
    {
        public override Task<KeepAliveReply> KeepAlive(KeepAliveRequest request, ServerCallContext context)
        {
            return base.KeepAlive(request, context);
        }

        public override Task<LivePlayReply> LivePlay(LivePlayRequest request, ServerCallContext context)
        {
            return base.LivePlay(request, context);
        }

        public override Task<PlaybackReply> PlayBack(PlaybackRequest request, ServerCallContext context)
        {
            return base.PlayBack(request, context);
        }
    }
}
