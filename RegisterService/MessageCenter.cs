using System;
using System.Collections.Generic;
using SIPSorcery.GB28181.Servers;
using SIPSorcery.GB28181.SIP;
using SIPSorcery.GB28181.Sys.XML;

namespace RegisterService
{
    public class MessageCenter
    {
        private DateTime _keepaliveTime;
        private Queue<HeartBeatEndPoint> _keepAliveQueue = new Queue<HeartBeatEndPoint>();


        private Queue<Catalog> _catalogQueue = new Queue<Catalog>();

        public void OnKeepaliveReceived(SIPEndPoint remoteEP, KeepAlive keapalive, string devId)
        {
            _keepaliveTime = DateTime.Now;
            var hbPoint = new HeartBeatEndPoint()
            {
                RemoteEP = remoteEP,
                Heart = keapalive
            };
            _keepAliveQueue.Enqueue(hbPoint);
        }

        public void OnServiceChanged(string msg, ServiceStatus state)
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
        public void OnRecordInfoReceived(RecordInfo record)
        {

            SetRecord(record);

        }


        private void SetRecord(RecordInfo record)
        {
            foreach (var item in record.RecordItems.Items)
            {
            }
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