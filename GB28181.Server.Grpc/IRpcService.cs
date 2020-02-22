
namespace GB28181.Server.Grpc
{
    public interface IRpcService
    {
        void AddIPAdress(string ipaddress);

        void AddPort(int port);

        void Run();

    }
}
