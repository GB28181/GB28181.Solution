using Grpc.Core;
using MediaSession;

namespace GrpcAgent.WebsocketServer
{
    class WebsocketRpcServer
    {

        private int _port = 0;
        private string _ipaddress = "localhost";
        private Server _server = null;
        public WebsocketRpcServer(int port = 0)
        {
            if (port < 1)
            {
                port = 50051;
            }
            _port = port;
        }


        public void Run()
        {
            _server = new Server
            {
                Services = { VideoControl.BindService(new SSMediaSessionImpl()) },
                Ports = { new ServerPort(_ipaddress, _port, ServerCredentials.Insecure) }
            };
            _server.Start();


            _server.ShutdownAsync().Wait();
        }




    }
}
