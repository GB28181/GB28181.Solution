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

namespace RegisterService
{
    public class MainProcess : IDisposable
    {


        //interface IDisposable implementation
        private bool _already_disposed = false;

        // Thread signal for stop work.
        private readonly ManualResetEvent eventStopService = new ManualResetEvent(false);

        // Thread signal for infor thread is over.
        private readonly ManualResetEvent eventThreadExit = new ManualResetEvent(false);

        //signal to exit the main Process
        private readonly AutoResetEvent eventExitMainProcess = new AutoResetEvent(false);



        private Thread _mainWorkThread = null;

        private DateTime _keepaliveTime;
        private Queue<Keep> _keepQueue = new Queue<Keep>();

        #region 委托回调

        /// <summary>
        /// 设置心跳消息回调
        /// </summary>
        /// <param name="remoteEP"></param>
        /// <param name="keepalive"></param>
        public delegate void SetKeepaliveText(string remoteEP, KeepAlive keepalive);
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
                    eventStopService.Close();
                    eventThreadExit.Close();
                }
            }

            _already_disposed = true;
        }

        #endregion

        private void MainProcessing()
        {

            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

            _keepaliveTime = DateTime.Now;

            try
            {

                Task.Factory.StartNew(() =>
                {
                    var cameras = new List<CameraInfo>();
                    var account = SIPSqlite.Instance.Accounts.First();
                    var _messageCore = new SIPMessageCore(cameras, account);
                    _messageCore.Start();
                });

                eventExitMainProcess.Set();
                eventStopService.WaitOne();
            }
            catch (Exception)
            {
                eventExitMainProcess.Set();
            }
            finally
            {

            }

        }

        public void Run()
        {
            eventStopService.Reset();
            eventThreadExit.Reset();
            _mainWorkThread = new Thread(new ThreadStart(MainProcessing))
            {
                IsBackground = true
            };
            _mainWorkThread.Start();

            eventExitMainProcess.WaitOne();
        }



        public void Stop()
        {
            eventStopService.Set();
            eventThreadExit.WaitOne();

        }

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
    public class Keep
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
