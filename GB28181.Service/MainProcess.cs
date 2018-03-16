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
using GrpcAgent;
using GrpcAgent.WebsocketRpcServer;
using MediaContract;
using SIPSorcery.GB28181.Servers;

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

        private Task _mainTask = null;
        private Task _mainSipTask = null;
        private Task _mainWebSocketRpcTask = null;

        private DateTime _keepaliveTime;
        private Queue<HeartBeatEndPoint> _keepAliveQueue = new Queue<HeartBeatEndPoint>();


        private List<CameraInfo> _cameras = new List<CameraInfo>();

        private Queue<Catalog> _catalogQueue = new Queue<Catalog>();

        private readonly ServiceCollection servicesContainer = new ServiceCollection();

        private ServiceProvider _serviceProvider = null;
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

            //Config Service & and run
            ConfigServices();

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



        private void ConfigServices()
        {
            //we should initialize resource here then use them.
            servicesContainer.AddSingleton(servicesContainer); // add itself 
            servicesContainer.AddSingleton<ILog, Logger>();
            servicesContainer.AddSingleton<IStorageConfig, SipStorage>();
            servicesContainer.AddSingleton<MediaEventSource>();
            servicesContainer.AddScoped<VideoSession.VideoSessionBase, SSMediaSessionImpl>();
            servicesContainer.AddScoped<ISIPServiceDirector, SIPServiceDirector>();
            servicesContainer.AddSingleton<IRpcService, RpcServer>();
            servicesContainer.AddSingleton<ISipCoreService, SIPCoreMessageService>();
            servicesContainer.AddSingleton<MessageCenter>();

            _serviceProvider = servicesContainer.BuildServiceProvider();

        }


        private void MainServiceProcessing()
        {
            _keepaliveTime = DateTime.Now;
            try
            {
                var sipStorage = _serviceProvider.GetService<IStorageConfig>();
                var account = sipStorage.Accounts.First();
                if (account != null)
                {
                    //Run the Rpc Server End
                    _mainWebSocketRpcTask = Task.Factory.StartNew(() =>
                    {
                        var _mainWebSocketRpcServer = _serviceProvider.GetService<IRpcService>();
                        _mainWebSocketRpcServer.Run();
                    });
                    // start the Listening SipService in main Service
                    _mainSipTask = Task.Factory.StartNew(() =>
                    {

                        var _mainSipService = _serviceProvider.GetService<ISipCoreService>();
                        //Get meassage Handler
                        var messageHandler = _serviceProvider.GetService<MessageCenter>();
                        _mainSipService.OnKeepaliveReceived += messageHandler.OnKeepaliveReceived;
                        _mainSipService.OnServiceChanged += messageHandler.OnServiceChanged;
                        _mainSipService.OnCatalogReceived += messageHandler.OnCatalogReceived;
                        _mainSipService.OnNotifyCatalogReceived += messageHandler.OnNotifyCatalogReceived;
                        _mainSipService.OnAlarmReceived += messageHandler.OnAlarmReceived;
                        _mainSipService.OnRecordInfoReceived += messageHandler.OnRecordInfoReceived;
                        _mainSipService.OnDeviceStatusReceived += messageHandler.OnDeviceStatusReceived;
                        _mainSipService.OnDeviceInfoReceived += messageHandler.OnDeviceInfoReceived;
                        _mainSipService.OnMediaStatusReceived += messageHandler.OnMediaStatusReceived;
                        _mainSipService.OnPresetQueryReceived += messageHandler.OnPresetQueryReceived;
                        _mainSipService.OnDeviceConfigDownloadReceived += messageHandler.OnDeviceConfigDownloadReceived;
                        _mainSipService.Initialize(_cameras, account);
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
