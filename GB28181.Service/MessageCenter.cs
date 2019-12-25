using System;
using System.Collections.Generic;
using SIPSorcery.GB28181.Servers;
using SIPSorcery.GB28181.Servers.SIPMessage;
using SIPSorcery.GB28181.SIP;
using SIPSorcery.GB28181.Sys.XML;
using NATS.Client;
//using System.Diagnostics;
using SIPSorcery.GB28181.Sys;
using System.Text;
using Logger4Net;
using Newtonsoft.Json;
using Google.Protobuf;
//using SIPSorcery.GB28181.SIP.App;
using Manage;
using Grpc.Core;
//using GrpcDeviceCatalog;

namespace GB28181Service
{
    public class MessageCenter
    {
        private static ILog logger = AppState.logger;
        private DateTime _keepaliveTime;
        private Queue<HeartBeatEndPoint> _keepAliveQueue = new Queue<HeartBeatEndPoint>();
        private Queue<Catalog> _catalogQueue = new Queue<Catalog>();
        private List<string> _deviceAlarmSubscribed = new List<string>();
        private ISipMessageCore _sipCoreMessageService;
        private ISIPMonitorCore _sIPMonitorCore;
        private ISIPRegistrarCore _registrarCore;
        //private Dictionary<string, HeartBeatEndPoint> _HeartBeatStatuses = new Dictionary<string, HeartBeatEndPoint>();
        //public Dictionary<string, HeartBeatEndPoint> HeartBeatStatuses => _HeartBeatStatuses;
        private Dictionary<string, DeviceStatus> _DeviceStatuses = new Dictionary<string, DeviceStatus>();
        public Dictionary<string, DeviceStatus> DeviceStatuses => _DeviceStatuses;
        private Dictionary<string, Catalog> _Catalogs = new Dictionary<string, Catalog>();
        public Dictionary<string, Catalog> Catalogs => _Catalogs;
        private Dictionary<string, SIPTransaction> _GBSIPTransactions = new Dictionary<string, SIPTransaction>();
        public Dictionary<string, SIPTransaction> GBSIPTransactions => _GBSIPTransactions;
        SIPSorcery.GB28181.SIP.App.SIPAccount _SIPAccount;

        public MessageCenter(ISipMessageCore sipCoreMessageService, ISIPMonitorCore sIPMonitorCore, ISIPRegistrarCore sipRegistrarCore)
        {
            _sipCoreMessageService = sipCoreMessageService;
            _sIPMonitorCore = sIPMonitorCore;
            _registrarCore = sipRegistrarCore;
            _registrarCore.DeviceAlarmSubscribe += OnDeviceAlarmSubscribeReceived;
            _registrarCore.RPCDmsRegisterReceived += _sipRegistrarCore_RPCDmsRegisterReceived;
        }

        public void OnCatalogReceived(Catalog obj)
        {
            if (!Catalogs.ContainsKey(obj.DeviceID))
            {
                Catalogs.Add(obj.DeviceID, obj);
                logger.Debug("OnCatalogReceived: " + JsonConvert.SerializeObject(obj).ToString());
            }
            if (GBSIPTransactions.ContainsKey(obj.DeviceID))
            {
                SIPTransaction _SIPTransaction = GBSIPTransactions[obj.DeviceID];
                obj.DeviceList.Items.FindAll(item => item != null).ForEach(catalogItem =>
                {
                    var devCata = DevType.GetCataType(catalogItem.DeviceID);
                    if (devCata == DevCataType.Device)
                    {
                        _SIPTransaction.TransactionRequestFrom.URI.User = catalogItem.DeviceID;
                        string gbname = "GB_" + catalogItem.Name;
                        //string gbname = "gb" + _SIPTransaction.TransactionRequest.RemoteSIPEndPoint.Address.ToString();
                        if (!string.IsNullOrEmpty(catalogItem.ParentID) && !obj.DeviceID.Equals(catalogItem.DeviceID))
                        {
                            gbname = "GB_" + catalogItem.Name;
                        }
                        logger.Debug("OnCatalogReceived.DeviceDmsRegister: catalogItem=" + JsonConvert.SerializeObject(catalogItem));

                        //query device info from db
                        string edit = IsDeviceExisted(catalogItem.DeviceID) ? "updated" : "added";

                        //Device Dms Register
                        DeviceDmsRegister(_SIPTransaction, gbname);

                        //Device Edit Event
                        DeviceEditEvent(catalogItem.DeviceID, edit);
                    }
                });
            }
        }

        public void OnDeviceStatusReceived(SIPEndPoint arg1, DeviceStatus arg2)
        {
            DeviceStatuses.Remove(arg2.DeviceID);
            DeviceStatuses.Add(arg2.DeviceID, arg2);
        }

        internal void OnKeepaliveReceived(SIPEndPoint remoteEP, KeepAlive keapalive, string devId)
        {
            _keepaliveTime = DateTime.Now;
            var hbPoint = new HeartBeatEndPoint()
            {
                RemoteEP = remoteEP,
                Heart = keapalive,
                KeepaliveTime = _keepaliveTime
            };
            _keepAliveQueue.Enqueue(hbPoint);

            //HeartBeatStatuses.Remove(devId);
            //HeartBeatStatuses.Add(devId, hbPoint);
        }

        internal void OnServiceChanged(string msg, ServiceStatus state)
        {
            SetSIPService(msg, state);
        }

        /// <summary>
        /// 设置sip服务状态
        /// </summary>
        /// <param name="state">sip状态</param>
        private void SetSIPService(string msg, ServiceStatus state)
        {
            logger.Debug("SIP Service Status: " + msg + "," + state);
        }

        ///// <summary>
        ///// 目录查询回调
        ///// </summary>
        ///// <param name="cata"></param>
        //public void OnCatalogReceived(Catalog cata)
        //{
        //    _catalogQueue.Enqueue(cata);
        //}

        //设备信息查询回调函数
        private void DeviceInfoReceived(SIPEndPoint remoteEP, DeviceInfo device)
        {
        }

        //设备状态查询回调函数
        private void DeviceStatusReceived(SIPEndPoint remoteEP, DeviceStatus device)
        {
        }

        ///// <summary>
        ///// 录像查询回调
        ///// </summary>
        ///// <param name="record"></param>
        //internal void OnRecordInfoReceived(RecordInfo record)
        //{
        //    SetRecord(record);
        //}

        //private void SetRecord(RecordInfo record)
        //{
        //    foreach (var item in record.RecordItems.Items)
        //    {
        //    }
        //}

        //internal void OnNotifyCatalogReceived(NotifyCatalog notify)
        //{
        //    if (notify.DeviceList == null)
        //    {
        //        return;
        //    }
        //    new Action(() =>
        //    {
        //        foreach (var item in notify.DeviceList.Items)
        //        {
        //        }
        //    }).BeginInvoke(null, null);
        //}

        /// <summary>
        /// 报警订阅
        /// </summary>
        /// <param name="sIPTransaction"></param>
        /// <param name="sIPAccount"></param>
        internal void OnDeviceAlarmSubscribeReceived(SIPTransaction sIPTransaction)
        {
            try
            {
                string keyDeviceAlarmSubscribe = sIPTransaction.RemoteEndPoint.ToString() + " - " + sIPTransaction.TransactionRequestFrom.URI.User;
                //if (!_deviceAlarmSubscribed.Contains(keyDeviceAlarmSubscribe))
                //{
                    //_sIPMonitorCore.DeviceControlResetAlarm(sIPTransaction.RemoteEndPoint, sIPTransaction.TransactionRequestFrom.URI.User);
                    //logger.Debug("Device Alarm Reset: " + keyDeviceAlarmSubscribe);
                    //_sIPMonitorCore.DeviceAlarmSubscribe(sIPTransaction.RemoteEndPoint, sIPTransaction.TransactionRequestFrom.URI.User);
                    //logger.Debug("Device Alarm Subscribe: " + keyDeviceAlarmSubscribe);
                    //_deviceAlarmSubscribed.Add(keyDeviceAlarmSubscribe);
                //}
            }
            catch (Exception ex)
            {
                logger.Error("OnDeviceAlarmSubscribeReceived: " + ex.Message);
            }
        }
        /// <summary>
        /// 设备报警
        /// </summary>
        /// <param name="alarm"></param>
        internal void OnAlarmReceived(Alarm alarm)
        {
            try
            {
                //logger.Debug("OnAlarmReceived started.");
                Event.Alarm alm = new Event.Alarm();
                alm.AlarmType = alm.AlarmType = Event.Alarm.Types.AlarmType.CrossingLine ;
                //switch (alarm.AlarmMethod)
                //{
                //    case "1":
                //        break;
                //    case "2":
                //        alm.AlarmType = Event.Alarm.Types.AlarmType.AlarmOutput;
                //        break;
                //}
                alm.Detail = alarm.AlarmDescription ?? string.Empty;
                //alm.DeviceID = alarm.DeviceID;//dms deviceid
                //alm.DeviceName = alarm.DeviceID;//dms name
                string GBServerChannelAddress = EnvironmentVariables.DeviceManagementServiceAddress ?? "devicemanagementservice:8080";
                logger.Debug("Device Management Service Address: " + GBServerChannelAddress);
                Channel channel = new Channel(GBServerChannelAddress, ChannelCredentials.Insecure);
                var client = new Manage.Manage.ManageClient(channel);
                QueryGBDeviceByGBIDsResponse _rep = new QueryGBDeviceByGBIDsResponse();
                QueryGBDeviceByGBIDsRequest req = new QueryGBDeviceByGBIDsRequest();
                logger.Debug("OnAlarmReceived Alarm: " + JsonConvert.SerializeObject(alarm));
                req.GbIds.Add(alarm.DeviceID);
                _rep = client.QueryGBDeviceByGBIDs(req);
                if (_rep.Devices != null && _rep.Devices.Count > 0)
                {
                    alm.DeviceID = _rep.Devices[0].GBID;
                    alm.DeviceName = _rep.Devices[0].Name;
                }
                else
                {
                    logger.Debug("QueryGBDeviceByGBIDsResponse Devices.Count: " + _rep.Devices.Count);
                }
                logger.Debug("QueryGBDeviceByGBIDsRequest-Alarm .Devices: " + _rep.Devices[0].ToString());
                UInt64 timeStamp = (UInt64)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0)).TotalSeconds;
                alm.EndTime = timeStamp;
                alm.StartTime = timeStamp;

                Message message = new Message();
                Dictionary<string, string> dic = new Dictionary<string, string>();
                dic.Add("Content-Type", "application/octet-stream");
                message.Header = dic;
                message.Body = alm.ToByteArray();

                byte[] payload = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message));
                string subject = Event.AlarmTopic.OriginalAlarmTopic.ToString();//"OriginalAlarmTopic"
                #region
                Options opts = ConnectionFactory.GetDefaultOptions();
                opts.Url = EnvironmentVariables.GBNatsChannelAddress ?? Defaults.Url;
                logger.Debug("GB Nats Channel Address: " + opts.Url);
                using (IConnection c = new ConnectionFactory().CreateConnection(opts))
                {
                    c.Publish(subject, payload);
                    c.Flush();
                    logger.Debug("Device alarm created connection and published.");
                }
                #endregion

                new Action(() =>
                {
                    logger.Debug("OnAlarmReceived AlarmResponse: " + alm.ToString());

                    _sipCoreMessageService.NodeMonitorService[alarm.DeviceID].AlarmResponse(alarm);
                }).Invoke();
            }
            catch (Exception ex)
            {
                logger.Error("OnAlarmReceived Exception: " + ex.Message);
            }
        }

        /// <summary>
        /// 设备状态上报
        /// </summary>
        internal void DeviceStatusReport()
        {
            //logger.Debug("DeviceStatusReport started.");
            TimeSpan pre = new TimeSpan(DateTime.Now.Ticks);
            while (true)
            {
                //report status every 8 seconds 
                System.Threading.Thread.Sleep(8000);
                try
                {
                    foreach (string deviceid in _sipCoreMessageService.NodeMonitorService.Keys)
                    {
                        //if not device then skip
                        if (!DevType.GetCataType(deviceid).Equals(DevCataType.Device)) continue;

                        Event.Status stat = new Event.Status();
                        stat.Status_ = false;
                        stat.OccurredTime = (UInt64)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0)).TotalSeconds;
                        #region waiting DeviceStatuses add in for 500 Milliseconds
                        _sipCoreMessageService.DeviceStateQuery(deviceid);
                        TimeSpan t1 = new TimeSpan(DateTime.Now.Ticks);
                        while (true)
                        {
                            System.Threading.Thread.Sleep(100);
                            TimeSpan t2 = new TimeSpan(DateTime.Now.Ticks);
                            if (DeviceStatuses.ContainsKey(deviceid))
                            {
                                //on line
                                stat.Status_ = DeviceStatuses[deviceid].Status.Equals("ON") ? true : false;
                                //logger.Debug("Device status of [" + deviceid + "]: " + DeviceStatuses[deviceid].Status);
                                DeviceStatuses.Remove(deviceid);
                                break;
                            }
                            else if (t2.Subtract(t1).Duration().Milliseconds > 500)
                            {
                                //off line
                                //logger.Debug("Device status of [" + deviceid + "]: OFF");
                                break;
                            }
                        }
                        #endregion
                        string GBServerChannelAddress = EnvironmentVariables.DeviceManagementServiceAddress ?? "devicemanagementservice:8080";
                        Channel channel = new Channel(GBServerChannelAddress, ChannelCredentials.Insecure);
                        var client = new Manage.Manage.ManageClient(channel);
                        QueryGBDeviceByGBIDsResponse rep = new QueryGBDeviceByGBIDsResponse();
                        QueryGBDeviceByGBIDsRequest req = new QueryGBDeviceByGBIDsRequest();
                        req.GbIds.Add(deviceid);
                        rep = client.QueryGBDeviceByGBIDs(req);
                        if (rep.Devices != null && rep.Devices.Count > 0)
                        {
                            stat.DeviceID = rep.Devices[0].Guid;
                            stat.DeviceName = rep.Devices[0].Name;

                            Message message = new Message();
                            Dictionary<string, string> dic = new Dictionary<string, string>();
                            dic.Add("Content-Type", "application/octet-stream");
                            message.Header = dic;
                            message.Body = stat.ToByteArray();
                            byte[] payload = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message));
                            string subject = Event.StatusTopic.OriginalStatusTopic.ToString();
                            #region
                            Options opts = ConnectionFactory.GetDefaultOptions();
                            opts.Url = EnvironmentVariables.GBNatsChannelAddress ?? Defaults.Url;
                            using (IConnection c = new ConnectionFactory().CreateConnection(opts))
                            {
                                c.Publish(subject, payload);
                                c.Flush();
                                //logger.Debug("Device on/off line status published.");
                            }
                            #endregion
                        }
                        else
                        {
                            logger.Debug("QueryGBDeviceByGBIDsRequest: Devices[" + deviceid + "] can't be found in database");
                            continue;
                        }
                        //logger.Debug("QueryGBDeviceByGBIDsRequest-Status .Devices: " + rep.Devices[0].ToString());
                    }
                }
                catch (Exception ex)
                {
                    logger.Error("DeviceStatusReport Exception: " + ex.Message);
                }
            }
        }

        /// <summary>
        /// 设备注册事件
        /// </summary>
        /// <param name="sipTransaction"></param>
        /// <param name="sIPAccount"></param>
        private void _sipRegistrarCore_RPCDmsRegisterReceived(SIPTransaction sipTransaction, SIPSorcery.GB28181.SIP.App.SIPAccount sIPAccount)
        {
            try
            {
                _SIPAccount = sIPAccount;
                string deviceid = sipTransaction.TransactionRequestFrom.URI.User;

                //GB SIPTransactions Dictionary
                GBSIPTransactions.Remove(deviceid);
                GBSIPTransactions.Add(deviceid, sipTransaction);

                //Device Catalog Query
                _sipCoreMessageService.DeviceCatalogQuery(deviceid);

                ////query device info from db
                //string edit = IsDeviceExisted(deviceid) ? "updated" : "added";

                ////Device Dms Register
                //DeviceDmsRegister(sipTransaction,"gb");

                ////Device Edit Event
                //DeviceEditEvent(deviceid, edit);
            }
            catch (Exception ex)
            {
                logger.Error("_sipRegistrarCore_RPCDmsRegisterReceived Exception: " + ex.Message);
            }
        }
        private void DeviceDmsRegister(SIPTransaction sipTransaction, string gbname)
        {
            try
            {
                //Device insert into database
                Device _device = new Device();
                SIPRequest sipRequest = sipTransaction.TransactionRequest;
                _device.Guid = Guid.NewGuid().ToString();
                _device.IP = sipTransaction.TransactionRequest.RemoteSIPEndPoint.Address.ToString();//IPC
                _device.Name = gbname;
                _device.LoginUser.Add(new LoginUser() { LoginName = _SIPAccount.SIPUsername ?? "admin", LoginPwd = _SIPAccount.SIPPassword ?? "123456" });//same to GB config service
                _device.Port = Convert.ToUInt32(sipTransaction.TransactionRequest.RemoteSIPEndPoint.Port);//5060
                _device.GBID = sipTransaction.TransactionRequestFrom.URI.User;//42010000001180000184
                _device.PtzType = 0;
                _device.ProtocolType = 0;
                _device.ShapeType = ShapeType.Dome;
                //var options = new List<ChannelOption> { new ChannelOption(ChannelOptions.MaxMessageLength, int.MaxValue) };
                Channel channel = new Channel(EnvironmentVariables.DeviceManagementServiceAddress ?? "devicemanagementservice:8080", ChannelCredentials.Insecure);
                logger.Debug("Device Management Service Address: " + (EnvironmentVariables.DeviceManagementServiceAddress ?? "devicemanagementservice:8080"));
                var client = new Manage.Manage.ManageClient(channel);
                //if (!_sipCoreMessageService.NodeMonitorService.ContainsKey(_device.GBID))
                //{
                //    AddDeviceRequest _AddDeviceRequest = new AddDeviceRequest();
                //    _AddDeviceRequest.Device.Add(_device);
                //    _AddDeviceRequest.LoginRoleId = "XXXX";
                //    var reply = client.AddDevice(_AddDeviceRequest);
                //    if (reply.Status == OP_RESULT_STATUS.OpSuccess)
                //    {
                //        logger.Debug("Device[" + sipTransaction.TransactionRequest.RemoteSIPEndPoint + "] have added registering DMS service.");
                //        DeviceEditEvent(_device.GBID, "add");
                //    }
                //    else
                //    {
                //        logger.Error("_sipRegistrarCore_RPCDmsRegisterReceived.AddDevice: " + reply.Status.ToString());
                //    }
                //}
                //else
                //{
                //    UpdateDeviceRequest _UpdateDeviceRequest = new UpdateDeviceRequest();
                //    _UpdateDeviceRequest.DeviceItem.Add(_device);
                //    _UpdateDeviceRequest.LoginRoleId = "XXXX";
                //    var reply = client.UpdateDevice(_UpdateDeviceRequest);
                //    if (reply.Status == OP_RESULT_STATUS.OpSuccess)
                //    {
                //        logger.Debug("Device[" + sipTransaction.TransactionRequest.RemoteSIPEndPoint + "] have updated registering DMS service.");
                //    }
                //    else
                //    {
                //        logger.Error("_sipRegistrarCore_RPCDmsRegisterReceived.UpdateDevice: " + reply.Status.ToString());
                //    }
                //}

                //add & update device
                AddDeviceRequest _AddDeviceRequest = new AddDeviceRequest();
                _AddDeviceRequest.Device.Add(_device);
                _AddDeviceRequest.LoginRoleId = "XXXX";
                var reply = client.AddDevice(_AddDeviceRequest);
                if (reply.Status == OP_RESULT_STATUS.OpSuccess)
                {
                    logger.Debug("Device added into DMS service: " + JsonConvert.SerializeObject(_device));
                }
                else
                {
                    logger.Warn("DeviceDmsRegister.AddDevice: " + reply.Status.ToString());
                }
            }
            catch (Exception ex)
            {
                logger.Error("DeviceDmsRegister Exception: " + ex.Message);
            }
        }
        /// <summary>
        /// query device info from db
        /// </summary>
        /// <param name="deviceid"></param>
        /// <returns></returns>
        private bool IsDeviceExisted(string deviceid)
        {
            bool tf = false;
            //var options = new List<ChannelOption> { new ChannelOption(ChannelOptions.MaxMessageLength, int.MaxValue) };
            Channel channel = new Channel(EnvironmentVariables.DeviceManagementServiceAddress ?? "devicemanagementservice:8080", ChannelCredentials.Insecure);
            logger.Debug("Device Management Service Address: " + (EnvironmentVariables.DeviceManagementServiceAddress ?? "devicemanagementservice:8080"));
            var client = new Manage.Manage.ManageClient(channel);
            QueryGBDeviceByGBIDsResponse rep = new QueryGBDeviceByGBIDsResponse();
            QueryGBDeviceByGBIDsRequest req = new QueryGBDeviceByGBIDsRequest();
            req.GbIds.Add(deviceid);
            rep = client.QueryGBDeviceByGBIDs(req);
            tf = rep.Devices.Count > 0;
            return tf;
        }

        /// <summary>
        /// 设备编辑事件
        /// </summary>
        internal void DeviceEditEvent(string DeviceID, string edittype)
        {
            try
            {
                Event.Event evt = new Event.Event();
                evt.EventType = Event.Event.Types.EventType.MediaConfigurationChanged;
                evt.Detail = "DeviceEditEvent: " + edittype + " " + DeviceID;
                evt.OccurredTime = (UInt64)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0)).TotalSeconds;

                string GBServerChannelAddress = EnvironmentVariables.DeviceManagementServiceAddress ?? "devicemanagementservice:8080";
                Channel channel = new Channel(GBServerChannelAddress, ChannelCredentials.Insecure);
                var client = new Manage.Manage.ManageClient(channel);
                QueryGBDeviceByGBIDsResponse rep = new QueryGBDeviceByGBIDsResponse();
                QueryGBDeviceByGBIDsRequest req = new QueryGBDeviceByGBIDsRequest();
                req.GbIds.Add(DeviceID);
                rep = client.QueryGBDeviceByGBIDs(req);
                if (rep.Devices.Count > 0)
                {
                    evt.DeviceID = rep.Devices[0].Guid;
                    evt.DeviceName = rep.Devices[0].Name;
                    logger.Debug("DeviceEditEvent: " + edittype + " " + rep.Devices[0].ToString());
                }
                else
                {
                    logger.Warn("DeviceEditEvent: Devices[" + DeviceID + "] can't be found in database");
                    return;
                }

                Message message = new Message();
                Dictionary<string, string> dic = new Dictionary<string, string>();
                dic.Add("Content-Type", "application/octet-stream");
                message.Header = dic;
                message.Body = evt.ToByteArray();
                byte[] payload = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message));
                string subject = Event.EventTopic.OriginalEventTopic.ToString();//"OriginalEventTopic"
                #region
                Options opts = ConnectionFactory.GetDefaultOptions();
                opts.Url = EnvironmentVariables.GBNatsChannelAddress ?? Defaults.Url;
                using (IConnection c = new ConnectionFactory().CreateConnection(opts))
                {
                    c.Publish(subject, payload);
                    c.Flush();
                    logger.Debug("Device add/update event published.");
                }
                #endregion
            }
            catch (Exception ex)
            {
                logger.Error("DeviceEditEvent Exception: " + ex.Message);
            }
        }

        //internal void OnDeviceStatusReceived(SIPEndPoint remoteEP, DeviceStatus device)
        //{
        //    var msg = "DeviceID:" + device.DeviceID +
        //         "\r\nResult:" + device.Result +
        //         "\r\nOnline:" + device.Online +
        //         "\r\nState:" + device.Status;
        //    new Action(() =>
        //    {
        //    }).Invoke();
        //}

        internal void OnDeviceInfoReceived(SIPEndPoint arg1, DeviceInfo arg2)
        {
            throw new NotImplementedException();
        }

        internal void OnMediaStatusReceived(SIPEndPoint arg1, MediaStatus arg2)
        {
            throw new NotImplementedException();
        }

        internal void OnPresetQueryReceived(SIPEndPoint arg1, PresetInfo arg2)
        {
            throw new NotImplementedException();
        }

        internal void OnDeviceConfigDownloadReceived(SIPEndPoint arg1, DeviceConfigDownload arg2)
        {
            throw new NotImplementedException();
        }
        internal void OnResponseCodeReceived(SIPResponseStatusCodesEnum status, string msg, SIPEndPoint remoteEP)
        {
            logger.Debug("OnResponseCodeReceived: " + msg);
        }
    }

    /// <summary>
    /// 心跳
    /// </summary>
    public class HeartBeatEndPoint
    {
        /// <summary>
        /// 远程终结点
        /// </summary>
        public SIPEndPoint RemoteEP { get; set; }

        /// <summary>
        /// 心跳周期
        /// </summary>
        public KeepAlive Heart { get; set; }

        public DateTime KeepaliveTime { get; set; }
    }

    public class Message
    {
        public Dictionary<string, string> Header { get; set; }
        public byte[] Body { get; set; }
    }
}