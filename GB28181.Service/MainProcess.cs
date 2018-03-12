using Logger4Net;
using SIPSorcery.GB28181.Servers.SIPMessage;
using SIPSorcery.GB28181.Sys;
using SIPSorcery.GB28181.Sys.Config;
using SIPSorcery.GB28181.Sys.Model;
using SIPSorcery.GB28181.Sys.XML;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Proto.Grpc;

namespace GB28181Service
{
    public class MainProcess : IDisposable
    {

        private static ILog logger = AppState.GetLogger("Startup");
        //interface IDisposable implementation
        private bool _already_disposed = false;

        // Thread signal for stop work.
        private readonly ManualResetEvent _eventStopService = new ManualResetEvent(false);

        // Thread signal for infor thread is over.
        private readonly ManualResetEvent _eventThreadExit = new ManualResetEvent(false);

        //signal to exit the main Process
        private readonly AutoResetEvent _eventExitMainProcess = new AutoResetEvent(false);

        public MessageCenter MessagerHandlers { get; set; }

        private Task _mainTask = null;
        private Task _mainSipTask = null;
        private Task _mainWebSocketRpcTask = null;

        private DateTime _keepaliveTime;
        private Queue<HeartBeatEndPoint> _keepAliveQueue = new Queue<HeartBeatEndPoint>();


        private List<CameraInfo> _cameras = null;

        private Queue<Catalog> _catalogQueue = new Queue<Catalog>();

        private readonly ServiceCollection servicesContainer = new ServiceCollection();
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

            servicesContainer.AddSingleton(servicesContainer); // add itself 
            servicesContainer.AddSingleton<ILog, Logger>();
            servicesContainer.AddSingleton(this);
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
                SipStorage.Instance.Read();
                var account = SipStorage.Instance.Accounts.First();
                if (account != null)
                {

                    //Run the Rpc Server End
                    _mainWebSocketRpcTask = Task.Factory.StartNew(() =>
                    {
                        var _mainWebSocketRpcServer = new WebsocketStub();
                        _mainWebSocketRpcServer.RunRpcServerStub();
                    });

                    // start the Listening SipService in main Service
                    _mainSipTask = Task.Factory.StartNew(() =>
                    {

                        var _mainSipService = new SIPCoreMessageService(_cameras, account);

                        MessagerHandlers = new MessageCenter(_mainSipService);
                        _mainSipService.OnKeepaliveReceived += MessagerHandlers.OnKeepaliveReceived;
                        _mainSipService.OnServiceChanged += MessagerHandlers.OnServiceChanged;
                        _mainSipService.OnCatalogReceived += MessagerHandlers.OnCatalogReceived;

                        _mainSipService.OnNotifyCatalogReceived += MessagerHandlers.OnNotifyCatalogReceived;
                        _mainSipService.OnAlarmReceived += MessagerHandlers.OnAlarmReceived;
                        _mainSipService.OnRecordInfoReceived += MessagerHandlers.OnRecordInfoReceived;
                        _mainSipService.OnDeviceStatusReceived += MessagerHandlers.OnDeviceStatusReceived;
                        _mainSipService.OnDeviceInfoReceived += MessagerHandlers.OnDeviceInfoReceived;
                        _mainSipService.OnMediaStatusReceived += MessagerHandlers.OnMediaStatusReceived;
                        _mainSipService.OnPresetQueryReceived += MessagerHandlers.OnPresetQueryReceived;
                        _mainSipService.OnDeviceConfigDownloadReceived += MessagerHandlers.OnDeviceConfigDownloadReceived;

                        _mainSipService.Start();
                    });

                }
                else
                {
                    throw new ApplicationException("Account Config NULL,SIP not started");
                }

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



}
