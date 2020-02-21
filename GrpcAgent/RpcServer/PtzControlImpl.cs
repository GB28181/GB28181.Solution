using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Grpc.Core;
using GrpcPtzControl;
using Logger4Net;
using SIPSorcery.GB28181.Servers;
using SIPSorcery.GB28181.Servers.SIPMonitor;

namespace GrpcAgent.WebsocketRpcServer
{
    public class PtzControlImpl : PtzControl.PtzControlBase
    {
        private static ILog logger = LogManager.GetLogger("RpcServer");
        private ISIPServiceDirector _sipServiceDirector = null;

        public PtzControlImpl(ISIPServiceDirector sipServiceDirector)
        {
            _sipServiceDirector = sipServiceDirector;
        }

        public override Task<PtzDirectReply> PtzDirect(PtzDirectRequest request, ServerCallContext context)
        {
            string msg = "OK";
            try
            {
                if (request.Xyz.X == 0 && request.Xyz.Y == 4 && request.Xyz.Z == 0)
                {
                    logger.Debug("PtzDirect: Up");
                    _sipServiceDirector.PtzControl(PTZCommand.Up, request.Speed * 50, request.Deviceid);
                }
                else if (request.Xyz.X == 0 && request.Xyz.Y == -4 && request.Xyz.Z == 0)
                {
                    logger.Debug("PtzDirect: Down");
                    _sipServiceDirector.PtzControl(PTZCommand.Down, request.Speed * 50, request.Deviceid);
                }
                else if (request.Xyz.X == 4 && request.Xyz.Y == 0 && request.Xyz.Z == 0)
                {
                    logger.Debug("PtzDirect: Left");
                    _sipServiceDirector.PtzControl(PTZCommand.Left, request.Speed * 50, request.Deviceid);
                }
                else if (request.Xyz.X == -4 && request.Xyz.Y == 0 && request.Xyz.Z == 0)
                {
                    logger.Debug("PtzDirect: Right");
                    _sipServiceDirector.PtzControl(PTZCommand.Right, request.Speed * 50, request.Deviceid);
                }
                else if (request.Xyz.X == 0 && request.Xyz.Y == 0 && request.Xyz.Z == 4)
                {
                    logger.Debug("PtzDirect: Zoom1");
                    _sipServiceDirector.PtzControl(PTZCommand.Zoom1, 2, request.Deviceid);
                }
                else if (request.Xyz.X == 0 && request.Xyz.Y == 0 && request.Xyz.Z == -4)
                {
                    logger.Debug("PtzDirect: Zoom2");
                    _sipServiceDirector.PtzControl(PTZCommand.Zoom2, 2, request.Deviceid);
                }
                else
                {
                    logger.Debug("PtzDirect: Stop");
                    _sipServiceDirector.PtzControl(PTZCommand.Stop, request.Speed * 50, request.Deviceid);
                }
            }
            catch (Exception ex)
            {
                msg = ex.Message;
                logger.Error("Exception GRPC PtzDirect: " + ex.Message);
            }
            return Task.FromResult(new PtzDirectReply { Message = msg });
        }
    }
}
