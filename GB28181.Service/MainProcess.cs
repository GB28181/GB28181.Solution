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
using System.IO;
using Microsoft.Extensions.Configuration;
using SIPSorcery.GB28181.SIP;

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
        public MainProcess()
        {
            _cameras.Add(new CameraInfo()
            {
                DeviceID = "34010000001310000001",
                IPAddress = "192.168.230.100",
                Port = 5060
            });
        }

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

            // config info for .net core https://www.cnblogs.com/Leo_wl/p/5745772.html#_label3
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddXmlFile("Config/gb28181.xml", false, reloadOnChange: true);
            var config = builder.Build();//// Console.WriteLine(config["sipaccount:ID"]);

            var sect = config.GetSection("sipaccounts");

            //Config Service & and run
            ConfigServices(config);

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

        private void ConfigServices(IConfigurationRoot configuration)
        {
            //we should initialize resource here then use them.
            servicesContainer.AddSingleton(servicesContainer); // add itself 
            servicesContainer.AddSingleton(configuration); // add configuration 
            servicesContainer.AddSingleton<ILog, Logger>();
            servicesContainer.AddSingleton<ISipAccount, SipAccountStorage>();
            servicesContainer.AddSingleton<MediaEventSource>();
            servicesContainer.AddScoped<VideoSession.VideoSessionBase, SSMediaSessionImpl>();
            servicesContainer.AddScoped<ISIPServiceDirector, SIPServiceDirector>();
            servicesContainer.AddSingleton<IRpcService, RpcServer>();
            servicesContainer.AddTransient<ISIPTransactionEngine, SIPTransactionEngine>();
            servicesContainer.AddSingleton<ISIPTransport, SIPTransport>();
            servicesContainer.AddSingleton<ISipCoreService, SIPCoreMessageService>();
            servicesContainer.AddSingleton<MessageCenter>();
            _serviceProvider = servicesContainer.BuildServiceProvider();

        }


        private void MainServiceProcessing()
        {
            _keepaliveTime = DateTime.Now;
            try
            {
                var sipStorage = _serviceProvider.GetService<ISipAccount>();

                // start the Listening SipService in main Service
                _mainSipTask = Task.Factory.StartNew(() =>
                {
                    var _mainSipService = _serviceProvider.GetRequiredService<ISipCoreService>();
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
                    _mainSipService.Initialize(_cameras);
                    _mainSipService.Start();

                });

                //Run the Rpc Server End
                _mainWebSocketRpcTask = Task.Factory.StartNew(() =>
                {
                    var _mainWebSocketRpcServer = _serviceProvider.GetService<IRpcService>();
                    _mainWebSocketRpcServer.AddIPAdress("127.0.0.1");
                    _mainWebSocketRpcServer.AddPort(50051);
                    _mainWebSocketRpcServer.Run();
                });

                var abc = WaitUserCmd();

                abc.Wait();

                //wait main service exit
                _eventStopService.WaitOne();

                //signal main process exit
                _eventExitMainProcess.Set();
            }
            catch (Exception exMsg)
            {
                logger.Error(exMsg.Message);
                _eventExitMainProcess.Set();
            }
            finally
            {

            }

        }


        private async Task WaitUserCmd()
        {
            await Task.Run(() =>
             {
                 while (true)
                 {
                     Console.WriteLine("\ninput command : I -Invite, E -Exit");
                     var inputkey = Console.ReadKey();
                     switch (inputkey.Key)
                     {
                         case ConsoleKey.I:
                             {
                                 var mockCaller = _serviceProvider.GetService<ISIPServiceDirector>();
                                 mockCaller.MakeVideoRequest("34010000001310000001", new int[] { 50000 }, "192.168.20.21");
                             }
                             break;

                         case ConsoleKey.E:
                             Console.WriteLine("\nexit Process!");
                             break;
                         default:
                             break;
                     }
                     if (inputkey.Key == ConsoleKey.E)
                     {
                         return 0;
                     }
                     else
                     {
                         continue;
                     }
                 }

             });
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
