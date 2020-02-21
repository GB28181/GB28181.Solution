using System.Threading.Tasks;
using Grpc.Core;
using GrpcDeviceCatalog;
using SIPSorcery.GB28181.Servers;
using SIPSorcery.GB28181.Sys.XML;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using Logger4Net;

namespace GrpcAgent.WebsocketRpcServer
{
    public class DeviceCatalogImpl : DeviceCatalog.DeviceCatalogBase
    {
        private static ILog logger = LogManager.GetLogger("RpcServer");
        private ISIPServiceDirector _sipServiceDirector = null;

        public DeviceCatalogImpl(ISIPServiceDirector sipServiceDirector)
        {
            _sipServiceDirector = sipServiceDirector;
        }

        public override Task<DeviceCatalogQueryReply> DeviceCatalogQuery(DeviceCatalogQueryRequest request, ServerCallContext context)
        {
            Instance instance = null;
            Catalog _Catalog = null;
            try
            {
                _sipServiceDirector.DeviceCatalogQuery(request.Deviceid);
                while (true)
                {
                    foreach (Catalog obj in _sipServiceDirector.Catalogs.Values)
                    {
                        if (request.Deviceid.Equals(obj.DeviceID))
                        {
                            _Catalog = obj;
                        }
                    }
                    if (_Catalog == null)
                    {
                        System.Threading.Thread.Sleep(500);
                    }
                    else
                    {
                        break;
                    }
                }
                
                List<Catalog.Item> lstCatalogItems = _Catalog.DeviceList.Items;
                string jsonCatalog = JsonConvert.SerializeObject(_Catalog)
                    .Replace("\"Certifiable\":null", "\"Certifiable\":0")
                    .Replace("\"ErrCode\":null", "\"ErrCode\":0")
                    .Replace("\"Secrecy\":null", "\"Secrecy\":0")
                    .Replace("\"Longitude\":null", "\"Longitude\":0")
                    .Replace("\"Latitude\":null", "\"Latitude\":0")
                    .Replace("\"Parental\":null", "\"Parental\":0")
                    .Replace("\"SafetyWay\":null", "\"SafetyWay\":0")
                    .Replace("\"RegisterWay\":null", "\"RegisterWay\":0")
                    .Replace("\"Port\":null", "\"Port\":0")
                    .Replace(":null", ":\"null\"")
                    .Replace(",\"InfList\":\"null\"", "");//delete InfList
                instance = JsonConvert.DeserializeObject<Instance>(jsonCatalog);
                foreach (Catalog.Item cataLogItem in lstCatalogItems)
                {
                    foreach (Item instanceItem in instance.DeviceList.Items)
                    {
                        if (cataLogItem.DeviceID == instanceItem.DeviceID)
                        {
                            string jsonInfList = JsonConvert.SerializeObject(cataLogItem.InfList)
                                .Replace(":null", ":\"null\"");
                            Info instanceInfo = JsonConvert.DeserializeObject<Info>(jsonInfList);
                            instanceItem.InfList = instanceInfo;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error("Exception GRPC DeviceCatalogQuery: " + ex.Message);
            }
            return Task.FromResult(new DeviceCatalogQueryReply { Catalog = instance });
        }

        public override Task<DeviceCatalogSubscribeReply> DeviceCatalogSubscribe(DeviceCatalogSubscribeRequest request, ServerCallContext context)
        {
            string msg = "OK";
            try
            {
                _sipServiceDirector.DeviceCatalogSubscribe(request.Deviceid);
            }
            catch (Exception ex)
            {
                msg = ex.Message;
            }
            return Task.FromResult(new DeviceCatalogSubscribeReply { Message = msg });
        }

        public override Task<DeviceCatalogNotifyReply> DeviceCatalogNotify(DeviceCatalogNotifyRequest request, ServerCallContext context)
        {
            string msg = "OK";
            NotifyCatalog.Item _NotifyCatalogItem = null;
            Item instance = null;
            try
            {
                while (_sipServiceDirector.NotifyCatalogItem.Count > 0)
                {
                    lock (_sipServiceDirector.NotifyCatalogItem)
                    {
                        _NotifyCatalogItem = _sipServiceDirector.NotifyCatalogItem.Dequeue();
                    }
                }
                string jsonObj = JsonConvert.SerializeObject(_NotifyCatalogItem)
                .Replace("\"Block\":null", "\"Block\":\"null\"")
                .Replace("\"ParentID\":null", "\"ParentID\":\"null\"")
                .Replace("\"BusinessGroupID\":null", "\"BusinessGroupID\":\"null\"")
                .Replace("\"CertNum\":null", "\"CertNum\":\"null\"")
                .Replace("\"Certifiable\":null", "\"Certifiable\":0")
                .Replace("\"ErrCode\":null", "\"ErrCode\":0")
                .Replace("\"EndTime\":null", "\"EndTime\":\"null\"")
                .Replace("\"Secrecy\":null", "\"Secrecy\":0")
                .Replace("\"Password\":null", "\"Password\":\"null\"")
                .Replace("\"Longitude\":null", "\"Longitude\":0")
                .Replace("\"Latitude\":null", "\"Latitude\":0")
                .Replace("\"Parental\":null", "\"Parental\":0")
                .Replace("\"SafetyWay\":null", "\"SafetyWay\":0")
                .Replace("\"RegisterWay\":null", "\"RegisterWay\":0")
                .Replace("\"Port\":null", "\"Port\":0")
                .Replace(":null", ":\"null\"")
                .Replace(",\"InfList\":\"null\"", "");//delete InfList
                instance = JsonConvert.DeserializeObject<Item>(jsonObj);
            }
            catch (Exception ex)
            {
                msg = ex.Message;
            }
            return Task.FromResult(new DeviceCatalogNotifyReply { Item = instance });
        }
    }
}
