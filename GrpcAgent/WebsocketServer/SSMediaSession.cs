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
        public override Task<ParametersReply> AskParameters(ParametersRequest request, ServerCallContext context)
        {
            return base.AskParameters(request, context);
        }

        public override Task<MediaSessionReply> AskVideo(MediaSessionRequest request, ServerCallContext context)
        {
            return base.AskVideo(request, context);
        }
    }
}
