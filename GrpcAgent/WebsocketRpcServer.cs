using Grpc.Core;
using MediaContract;
using System;

namespace GrpcAgent
{
    public class RpcServer : IRpcService, IDisposable
    {
        private int _port = 0;
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

            _server.ShutdownAsync().Wait();
        }

        public void Dispose()
        {
        }
    }
}
