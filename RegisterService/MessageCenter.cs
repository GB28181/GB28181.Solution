using System;
using System.Collections.Generic;
using SIPSorcery.GB28181.Servers;
using SIPSorcery.GB28181.Servers.SIPMessage;
using SIPSorcery.GB28181.SIP;
using SIPSorcery.GB28181.Sys.XML;

namespace RegisterService
{
    public class MessageCenter
    {
        private DateTime _keepaliveTime;
        private Queue<HeartBeatEndPoint> _keepAliveQueue = new Queue<HeartBeatEndPoint>();


        private Queue<Catalog> _catalogQueue = new Queue<Catalog>();


        private SIPCoreMessageService _sipCoreMessageService;

        public MessageCenter(SIPCoreMessageService sipCoreMessageService)
        {
            _sipCoreMessageService = sipCoreMessageService;

        }

        internal void OnKeepaliveReceived(SIPEndPoint remoteEP, KeepAlive keapalive, string devId)
        {
            _keepaliveTime = DateTime.Now;
            var hbPoint = new HeartBeatEndPoint()
            {
                RemoteEP = remoteEP,
                Heart = keapalive
            };
            _keepAliveQueue.Enqueue(hbPoint);
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
            if (state == ServiceStatus.Wait)
            {

            }
            else
            {

            }
        }

        /// <summary>
        /// 目录查询回调
        /// </summary>
        /// <param name="cata"></param>
        public void OnCatalogReceived(Catalog cata)
        {
            _catalogQueue.Enqueue(cata);
        }



        //设备信息查询回调函数
        private void DeviceInfoReceived(SIPEndPoint remoteEP, DeviceInfo device)
        {


        }

        //设备状态查询回调函数
        private void DeviceStatusReceived(SIPEndPoint remoteEP, DeviceStatus device)
        {

        }



        /// <summary>
        /// 录像查询回调
        /// </summary>
        /// <param name="record"></param>
        internal void OnRecordInfoReceived(RecordInfo record)
        {

            SetRecord(record);

        }


        private void SetRecord(RecordInfo record)
        {
            foreach (var item in record.RecordItems.Items)
            {
            }
        }

        internal void OnNotifyCatalogReceived(NotifyCatalog notify)
        {
            if (notify.DeviceList == null)
            {
                return;
            }
            new Action(() =>
            {
                foreach (var item in notify.DeviceList.Items)
                {

                }
            }).BeginInvoke(null, null);
        }

        internal void OnAlarmReceived(Alarm alarm)
        {
            var msg = "DeviceID:" + alarm.DeviceID +
               "\r\nSN:" + alarm.SN +
               "\r\nCmdType:" + alarm.CmdType +
               "\r\nAlarmPriority:" + alarm.AlarmPriority +
               "\r\nAlarmMethod:" + alarm.AlarmMethod +
               "\r\nAlarmTime:" + alarm.AlarmTime +
               "\r\nAlarmDescription:" + alarm.AlarmDescription;
            new Action(() =>
            {

                var key = new MonitorKey()
                {
                    CmdType = CommandType.Play,
                    DeviceID = alarm.DeviceID
                };
                _sipCoreMessageService.MonitorService[key].AlarmResponse(alarm);
            }).Invoke();
        }

        internal void OnDeviceStatusReceived(SIPEndPoint remoteEP, DeviceStatus device)
        {
            var msg = "DeviceID:" + device.DeviceID +
                 "\r\nResult:" + device.Result +
                 "\r\nOnline:" + device.Online +
                 "\r\nState:" + device.Status;
            new Action(() =>
            {

            }).Invoke();
        }

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
    }
}