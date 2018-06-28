using Grpc.Core;
using Grpc.HealthCheck;
using Logger4Net;
using MediaContract;
using System;
using System.Threading;
using System.Threading.Tasks;
using GrpcPtzControl;

namespace GrpcAgent
{
    public class RpcServer : IRpcService, IDisposable
    {
        private static ILog logger = LogManager.GetLogger("RpcServer");

        private int _port = 50050;
        private string _ipaddress = "localhost";
        private Server _server = null;
        private string _name = "DefaultName";
        private VideoSession.VideoSessionBase _videoSession;
        private PtzControl.PtzControlBase _ptzControlService;
        public string Ipaddress { get => _ipaddress; set => _ipaddress = value; }
        public int Port { get => _port; set => _port = value; }

        //RPC Server Name to describe its properties.
        public string Name { get => _name; set => _name = value; }

        private readonly TaskCompletionSource<bool> tokenSource = new TaskCompletionSource<bool>();

        public RpcServer(VideoSession.VideoSessionBase videoSessionImp, PtzControl.PtzControlBase ptzControlService)
        {
            _videoSession = videoSessionImp;
            _ptzControlService = ptzControlService;
        }


        public void Run()
        {
            var healthService = new HealthServiceImpl();
            _server = new Server
            {
                Services = { VideoSession.BindService(_videoSession), PtzControl.BindService(_ptzControlService)},
                Ports = { new ServerPort(_ipaddress, _port, ServerCredentials.Insecure) }
            };

            _server.Start();

            healthService.SetStatus("GB28181", Grpc.Health.V1.HealthCheckResponse.Types.ServingStatus.Serving);
            //  var threadId = Thread.CurrentThread.ManagedThreadId;
            logger.Debug("RPC Server for StreamSever, successfully started at " + _ipaddress + ":" + _port);

            tokenSource.Task.Wait();
            _server.ShutdownAsync().Wait();
        }




        public void Dispose()
        {
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
