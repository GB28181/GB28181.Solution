using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using SIPSorcery.GB28181.Servers.SIPMessage;
using SIPSorcery.GB28181.Sys.Config;
using SIPSorcery.GB28181.Sys.Model;
using SIPSorcery.GB28181.Sys.XML;
using SIPSorcery.GB28181.SIP;
using SIPSorcery.GB28181.Servers;

namespace RegisterService
{
    public class MainProcess : IDisposable
    {

        //interface IDisposable implementation
        private bool _already_disposed = false;

        // Thread signal for stop work.
        private readonly ManualResetEvent _eventStopService = new ManualResetEvent(false);

        // Thread signal for infor thread is over.
        private readonly ManualResetEvent _eventThreadExit = new ManualResetEvent(false);

        //signal to exit the main Process
        private readonly AutoResetEvent _eventExitMainProcess = new AutoResetEvent(false);


        private Task _mainTask = null;
        private Task _mainService = null;

        private DateTime _keepaliveTime;
        private Queue<HeartBeatEndPoint> _keepAliveQueue = new Queue<HeartBeatEndPoint>();


        private List<CameraInfo> _cameras = null;

        private Queue<Catalog> _catalogQueue = new Queue<Catalog>();

        #region 委托回调

        /// <summary>
        /// 设置服务状态回调
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="state"></param>
        public delegate void SIPServiceStatusHandler(string msg, ServiceStatus state);
        /// <summary>
        /// 设置设备查询目录回调
        /// </summary>
        /// <param name="cata"></param>
        public delegate void CatalogQueryHandler(Catalog cata);
        /// <summary>
        /// 设置录像文件查询回调
        /// </summary>
        /// <param name="record"></param>
        public delegate void RecordQueryHandler(RecordInfo record);
        /// <summary>
        /// 设置心跳消息回调
        /// </summary>
        /// <param name="remoteEP"></param>
        /// <param name="keepalive"></param>
        public delegate void KeepAliveHandler(string remoteEP, KeepAlive keepalive);
        #endregion

        public MainProcess() { }

        #region IDisposable interface

        public void Dispose()
        {
            // tell the GC that the Finalize process no longer needs to be run for this object.
            GC.SuppressFinalize(this);
            Dispose(true);
        }


        protected virtual void Dispose(bool disposing)
        {

            lock (this)
            {
                if (_already_disposed)
                    return;

                if (disposing)
                {
                    _eventStopService.Close();
                    _eventThreadExit.Close();
                }
            }

            _already_disposed = true;
        }

        #endregion

        public void Run()
        {
            _eventStopService.Reset();
            _eventThreadExit.Reset();

            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

            //Start the main serice
            _mainTask = Task.Factory.StartNew(() => MainServiceProcessing());

            //wait the process exit of main
            _eventExitMainProcess.WaitOne();
        }



        public void Stop()
        {
            // signal main service exit
            _eventStopService.Set();
            _eventThreadExit.WaitOne();

        }

        private void MainServiceProcessing()
        {

            _keepaliveTime = DateTime.Now;

            try
            {
                _cameras = new List<CameraInfo>();
                // start the Listening SipService in main Service
                _mainService = Task.Factory.StartNew(() =>
                {

                    var _messageCore = new SIPCoreMessageService(_cameras, SipStorage.Instance.Accounts.First());

                    _messageCore.OnKeepaliveReceived += MessageCore_OnKeepaliveReceived;
                    _messageCore.OnServiceChanged += SIPServiceChangeHandle;
                    _messageCore.OnCatalogReceived += M_msgCore_OnCatalogReceived;

                    _messageCore.Start();
                });

                //wait main service exit
                _eventStopService.WaitOne();

                //signal main process exit
                _eventExitMainProcess.Set();
            }
            catch (Exception)
            {
                _eventExitMainProcess.Set();
            }
            finally
            {

            }

        }


        private void SetRecord(RecordInfo record)
        {
            foreach (var item in record.RecordItems.Items)
            {
            }
        }


        #region Event Call back



        private void MessageCore_OnKeepaliveReceived(SIPEndPoint remoteEP, KeepAlive keapalive, string devId)
        {
            _keepaliveTime = DateTime.Now;
            var hbPoint = new HeartBeatEndPoint()
            {
                RemoteEP = remoteEP,
                Heart = keapalive
            };
            _keepAliveQueue.Enqueue(hbPoint);
        }

        private void SIPServiceChangeHandle(string msg, ServiceStatus state)
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
        private void M_msgCore_OnCatalogReceived(Catalog cata)
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
        private void MessageCore_OnRecordInfoReceived(RecordInfo record)
        {

            SetRecord(record);

        }

        #endregion



        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject != null)
            {
                if (e.ExceptionObject is Exception exceptionObj)
                {
                    throw exceptionObj;
                }
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
