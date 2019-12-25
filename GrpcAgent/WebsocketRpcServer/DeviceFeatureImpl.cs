using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Grpc.Core;
using GrpcDeviceFeature;
using Logger4Net;
using Newtonsoft.Json;
using SIPSorcery.GB28181.Servers;
using SIPSorcery.GB28181.Servers.SIPMonitor;
using SIPSorcery.GB28181.Sys.XML;

namespace GrpcAgent.WebsocketRpcServer
{
    public class DeviceFeatureImpl : DeviceFeature.DeviceFeatureBase
    {
        private static ILog logger = LogManager.GetLogger("RpcServer");
        private ISIPServiceDirector _sipServiceDirector = null;

        public DeviceFeatureImpl(ISIPServiceDirector sipServiceDirector)
        {
            _sipServiceDirector = sipServiceDirector;
        }

        public override Task<DeviceStateQueryReply> DeviceStateQuery(DeviceStateQueryRequest request, ServerCallContext context)
        {
            DeviceStatus _DeviceStatus = null;
            Instance instance = null;
            try
            {
                _sipServiceDirector.DeviceStateQuery(request.Deviceid);
                while (true)
                {
                    foreach (DeviceStatus obj in _sipServiceDirector.DeviceStatuses.Values)
                    {
                        if (request.Deviceid.Equals(obj.DeviceID))
                        {
                            _DeviceStatus = obj;
                        }
                    }
                    if (_DeviceStatus == null)
                    {
                        System.Threading.Thread.Sleep(500);
                    }
                    else
                    {
                        break;
                    }
                }
                string json = JsonConvert.SerializeObject(_DeviceStatus)
                    .Replace("\"SN\":null", "\"SN\":0")
                    .Replace(":null", ":\"null\"");
                instance = JsonConvert.DeserializeObject<Instance>(json);
            }
            catch (Exception ex)
            {
                logger.Error("Exception GRPC DeviceStateQuery: " + ex.Message);
            }
            return Task.FromResult(new DeviceStateQueryReply { DeviceStatus = instance });
        }
    }
}
