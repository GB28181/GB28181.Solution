using Grpc.Core;
using GrpcAgent.WebsocketRpcServer;
using MediaContract;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace GrpcAgent
{
    public class RpcServer : IRpcService, IDisposable
    {
        private ServiceProvider _serviceProvider = null;

        private int _port = 0;
        private string _ipaddress = "localhost";
        private Server _server = null;
        private string _name = "DefaultName";

        private ServiceCollection _serviceCollection = null;
        public string Ipaddress { get => _ipaddress; set => _ipaddress = value; }
        public int Port { get => _port; set => _port = value; }

        //RPC Server Name to describe its properties.
        public string Name { get => _name; set => _name = value; }



        public RpcServer(ServiceCollection serviceCollection)
        {
            _serviceCollection = serviceCollection;
            _serviceProvider = _serviceCollection.BuildServiceProvider();
        }


        public void Run()
        {
            var tagetService = _serviceProvider.GetRequiredService<SSMediaSessionImpl>();
            _server = new Server
            {
                Services = { VideoSession.BindService(tagetService) },
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
