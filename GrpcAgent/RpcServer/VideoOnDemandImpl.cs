using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Grpc.Core;
using GrpcVideoOnDemand;
using Logger4Net;
using Newtonsoft.Json;
using SIPSorcery.GB28181.Servers;
using SIPSorcery.GB28181.Servers.SIPMonitor;
using SIPSorcery.GB28181.Sys.XML;

namespace GrpcAgent.WebsocketRpcServer
{
    public class VideoOnDemandImpl : VideoOnDemand.VideoOnDemandBase
    {
        private static ILog logger = LogManager.GetLogger("RpcServer");
        private ISIPServiceDirector _sipServiceDirector = null;

        public VideoOnDemandImpl(ISIPServiceDirector sipServiceDirector)
        {
            _sipServiceDirector = sipServiceDirector;
        }

        public override Task<RecordFileQueryReply> RecordFileQuery(RecordFileQueryRequest request, ServerCallContext context)
        {
            RecordInfo _RecordInfo = null;
            Instance instance = null;
            try
            {
                _sipServiceDirector.RecordFileQuery(request.Deviceid, DateTime.Parse(request.StartTime), DateTime.Parse(request.EndTime), request.Type);
                while (true)
                {
                    foreach (RecordInfo obj in _sipServiceDirector.RecordInfoes.Values)
                    {
                        if (request.Deviceid.Equals(obj.DeviceID))
                        {
                            _RecordInfo = obj;
                        }
                    }
                    if (_RecordInfo == null)
                    {
                        System.Threading.Thread.Sleep(500);
                    }
                    else
                    {
                        break;
                    }
                }
                List<RecordInfo.Item> lstItems = _RecordInfo.RecordItems.Items;
                _RecordInfo.RecordItems = null;
                string jsonRecordInfo = JsonConvert.SerializeObject(_RecordInfo)
                    .Replace("\"SN\":null", "\"SN\":0")
                    .Replace("\"SumNum\":null", "\"SumNum\":0")
                    .Replace(":null", ":\"null\"")
                    .Replace(",\"RecordItems\":\"null\"", "");//delete RecordItems
                instance = JsonConvert.DeserializeObject<Instance>(jsonRecordInfo);
                foreach (RecordInfo.Item _item in lstItems)
                {
                    string jsonRecordItems = JsonConvert.SerializeObject(_item)
                    .Replace(":null", ":\"null\"");
                    Item item = JsonConvert.DeserializeObject<Item>(jsonRecordItems);
                    instance.RecordItems.Add(item);
                }
            }
            catch (Exception ex)
            {
                logger.Error("Exception GRPC RecordFileQuery: " + ex.Message);
            }
            return Task.FromResult(new RecordFileQueryReply { RecordInfo = instance });
        }
    }
}
