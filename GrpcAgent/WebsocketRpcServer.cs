using Grpc.Core;
using Logger4Net;
using MediaContract;
using System;
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
        public string Ipaddress { get => _ipaddress; set => _ipaddress = value; }
        public int Port { get => _port; set => _port = value; }

        //RPC Server Name to describe its properties.
        public string Name { get => _name; set => _name = value; }



        public RpcServer(VideoSession.VideoSessionBase videoSessionImp)
        {
            _videoSession = videoSessionImp;
        }


        public void Run()
        {
            _server = new Server
            {
                Services = { VideoSession.BindService(_videoSession) },
                Ports = { new ServerPort(_ipaddress, _port, ServerCredentials.Insecure) }
            };
            _server.Start();

          //  var threadId = Thread.CurrentThread.ManagedThreadId;
            logger.Debug("RPC Server for StreamSever, successfully started at " + _ipaddress + ":" + _port);

            Console.ReadKey();
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
