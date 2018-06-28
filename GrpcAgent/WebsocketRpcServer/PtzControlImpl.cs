using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Grpc.Core;
using GrpcPtzControl;
//using SIPSorcery.GB28181.Servers;
using SIPSorcery.GB28181.Servers.SIPMessage;

namespace GrpcAgent.WebsocketRpcServer
{
    public class PtzControlImpl : PtzControl.PtzControlBase
    {
        //private ISIPMonitorCore _sipMonitorCore = null;
        private ISipMessageCore _sipMessageCore = null;

        public PtzControlImpl(ISipMessageCore sipMessageCore)
        {
            _sipMessageCore = sipMessageCore;
        }

        // Server side handler of the SayHello RPC
        public override Task<PtzDirectReply> PtzDirect(PtzDirectRequest request, ServerCallContext context)
        {
            _sipMessageCore.PtzControl((SIPSorcery.GB28181.Servers.SIPMonitor.PTZCommand)request.Ucommand, request.DwSpeed);
            string x = request.Deviceid + "," + request.Ucommand + "," + request.DwSpeed;
            x = "Status: 200 OK";
            return Task.FromResult(new PtzDirectReply { Message = x });
        }
    }
}
