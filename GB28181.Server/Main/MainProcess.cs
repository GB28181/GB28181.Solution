using System;
using System.Threading;
using System.Threading.Tasks;
using GB28181.Logger4Net;
using GB28181.Config;
using GB28181.Servers;
using GB28181.Servers.SIPMessage;
using GB28181.Sys;
using Microsoft.Extensions.Configuration;

namespace GB28181.Server.Main
{

    public interface IMainProcess
    {
        void Run();
        void Stop();
    }

    public partial class MainProcess : IMainProcess
    {
        private static readonly ILog logger = AppState.GetLogger("Process");
     
        private readonly CancellationTokenSource _processingServiceToken = new CancellationTokenSource();

        private readonly CancellationTokenSource _registryServiceToken = new CancellationTokenSource();

        private readonly CancellationTokenSource _deviceStatusReportToken = new CancellationTokenSource();

        // private Queue<HeartBeatEndPoint> _keepAliveQueue = new Queue<HeartBeatEndPoint>();
        //private readonly IServiceCollection servicesContainer = new ServiceCollection();
        // private ServiceProvider _serviceProvider = null;

        private ISipMessageCore _mainSipService;
        private MessageHub messageCenter;
        private ISIPRegistrarCore registry;

        public MainProcess() { }
        public MainProcess(ISipMessageCore sipMessageCore, MessageHub messageHub, ISIPRegistrarCore sipRegistrarCore)
        {
            _mainSipService = sipMessageCore;
            messageCenter = messageHub;
            registry = sipRegistrarCore;
           // _serviceProvider = services.BuildServiceProvider();
        }

        public void Stop()
        {
            _processingServiceToken.Cancel();
            _registryServiceToken.Cancel();
        }

        public void Run()
        {
            // config info for .net core https://www.cnblogs.com/Leo_wl/p/5745772.html#_label3
            var builder = new ConfigurationBuilder();
            builder.SetBasePath(System.IO.Directory.GetCurrentDirectory());
            builder.AddXmlFile("Config/gb28181.xml", false, reloadOnChange: true);
            var config = builder.Build();

            //InitServer
            SipStorage.RPCGBServerConfigReceived += SipAccountStorage_RPCGBServerConfigReceived;

            ////Config Service & and run
            //ConfigServices(config);

            //Start the sip main service
            Task.Factory.StartNew(() => Processing(), _processingServiceToken.Token);
        }

        //private void ConfigServices(IConfigurationRoot config)
        //{
        //    //we should initialize resource here then use them.
        //    servicesContainer.AddSingleton(servicesContainer); // add itself 
        //    _serviceProvider = servicesContainer.BuildServiceProvider();
        //}

        private void Processing()
        {
            //  _keepaliveTime = DateTime.Now;
            try
            {
               // var _mainSipService = _serviceProvider.GetRequiredService<ISipMessageCore>();
                //Get meassage Handler
              //  var messageCenter = _serviceProvider.GetRequiredService<MessageHub>();
                // start the Listening SipService in main Service
                Task.Run(() =>
                {
                    _mainSipService.OnKeepaliveReceived += messageCenter.OnKeepaliveReceived;
                    _mainSipService.OnServiceChanged += messageCenter.OnServiceChanged;
                    _mainSipService.OnCatalogReceived += messageCenter.OnCatalogReceived;
                    //_mainSipService.OnNotifyCatalogReceived += messageHandler.OnNotifyCatalogReceived;
                    _mainSipService.OnAlarmReceived += messageCenter.OnAlarmReceived;
                    //_mainSipService.OnRecordInfoReceived += messageHandler.OnRecordInfoReceived;
                    _mainSipService.OnDeviceStatusReceived += messageCenter.OnDeviceStatusReceived;
                    _mainSipService.OnDeviceInfoReceived += messageCenter.OnDeviceInfoReceived;
                    _mainSipService.OnMediaStatusReceived += messageCenter.OnMediaStatusReceived;
                    _mainSipService.OnPresetQueryReceived += messageCenter.OnPresetQueryReceived;
                    _mainSipService.OnDeviceConfigDownloadReceived += messageCenter.OnDeviceConfigDownloadReceived;
                    _mainSipService.OnResponseCodeReceived += messageCenter.OnResponseCodeReceived;
                    _mainSipService.Start();

                });

                // run the register service
              //  var registry = _serviceProvider.GetRequiredService<ISIPRegistrarCore>();

                Task.Factory.StartNew(() => registry.ProcessRegisterRequest(), _registryServiceToken.Token);

                //Device Status Report
                Task.Factory.StartNew(() => messageCenter.DeviceStatusReport(), _deviceStatusReportToken.Token);
            }
            catch (Exception exMsg)
            {
                logger.Error(exMsg.Message);
                throw exMsg;
            }

        }

    }
}
