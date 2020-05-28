using GB28181.Logger4Net;
using System;
using System.Threading.Tasks;
using GB28181.Service.Protos.Video;
using GB28181.Service.Protos.Ptz;
using GB28181.Service.Protos.DeviceFeature;
using GB28181.Service.Protos.VideoRecord;
using GB28181.Service.Protos.DeviceCatalog;
namespace GB28181.Service
{
    public class GrpcServer : IGrpcServer, IDisposable
    {
        private static ILog logger = LogManager.GetLogger("RpcServer");

        private int _port = 50050;
        private string _ipaddress = "localhost";
//        private Server _server = null;
        private string _name = "DefaultName";
        private VideoSession.VideoSessionBase _videoSession;
        private PtzControl.PtzControlBase _ptzControlService;
        private DeviceCatalog.DeviceCatalogBase _deviceCatalogService;
       // private Manage.Manage.ManageBase _deviceManageService;
        private DeviceFeature.DeviceFeatureBase _deviceFeatureService;
        private VideoOnDemand.VideoOnDemandBase _videoOnDemandService;
        public string Ipaddress { get => _ipaddress; set => _ipaddress = value; }
        public int Port { get => _port; set => _port = value; }

        //RPC Server Name to describe its properties.
        public string Name { get => _name; set => _name = value; }

        private readonly TaskCompletionSource<bool> tokenSource = new TaskCompletionSource<bool>();

        public GrpcServer(VideoSession.VideoSessionBase videoSessionImp, 
            PtzControl.PtzControlBase ptzControlService,
            DeviceCatalog.DeviceCatalogBase deviceCatalogService,
   //         Manage.ManageBase deviceManageService,
            DeviceFeature.DeviceFeatureBase deviceFeatureService,
            VideoOnDemand.VideoOnDemandBase videoOnDemandService)
        {
            _videoSession = videoSessionImp;
            _ptzControlService = ptzControlService;
            _deviceCatalogService = deviceCatalogService;
 //           _deviceManageService = deviceManageService;
            _deviceFeatureService = deviceFeatureService;
            _videoOnDemandService = videoOnDemandService;
        }

        public void Run()
        {
  //          var healthService = new HealthServiceImpl();
 //           _server = new Server
 //           {
 //               Services = { VideoSession.BindService(_videoSession),
 //                   PtzControl.BindService(_ptzControlService),
 //                   DeviceCatalog.BindService(_deviceCatalogService),
 ////                   Manage.Manage.BindService(_deviceManageService),
 //                   DeviceFeature.BindService(_deviceFeatureService),
 //                   VideoOnDemand.BindService(_videoOnDemandService)
 //               },
 //               Ports = { new ServerPort(_ipaddress, _port, ServerCredentials.Insecure) }
 //           };

   //         _server.Start();

        //    healthService.SetStatus("GB28181", Grpc.Health.V1.HealthCheckResponse.Types.ServingStatus.Serving);
            //  var threadId = Thread.CurrentThread.ManagedThreadId;
            logger.Debug("RPCTCPChannel socket on " + _ipaddress + ":" + _port + " listening started.");

            tokenSource.Task.Wait();
  //          _server.ShutdownAsync().Wait();
        }

        protected virtual void Dispose(bool all)
        {

        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void AddIPAdress(string ipaddress)
        {
            _ipaddress = ipaddress;
        }

        public void AddPort(int port)
        {

            _port = port;
            //throw new NotImplementedException();
        }
    }
}
