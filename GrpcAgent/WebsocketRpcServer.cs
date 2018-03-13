using Grpc.Core;
using GrpcAgent.WebsocketRpcServer;
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

        public string Ipaddress { get => _ipaddress; set => _ipaddress = value; }
        public int Port { get => _port; set => _port = value; }

        //RPC Server Name to describe its properties.
        public string Name { get => _name; set => _name = value; }


        private MediaEventSource _wsMediaEventSource = null;


        public void Run()
        {

            if (_wsMediaEventSource == null)
            {
                _wsMediaEventSource = new MediaEventSource();
            }

            _wsMediaEventSource.LivePlayRequestReceived += _wsMediaEventSource_LivePlayRequestReceived;

            _server = new Server
            {
                Services = { VideoSession.BindService(new SSMediaSessionImpl(_wsMediaEventSource)) },
                Ports = { new ServerPort(_ipaddress, _port, ServerCredentials.Insecure) }
            };


            _server.Start();

            _server.ShutdownAsync().Wait();
        }

        private void _wsMediaEventSource_LivePlayRequestReceived(StartLiveRequest request, ServerCallContext context)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            if (_wsMediaEventSource != null)
            {
                _wsMediaEventSource.LivePlayRequestReceived -= _wsMediaEventSource_LivePlayRequestReceived;
            }
        }
    }
}
