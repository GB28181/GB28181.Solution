
namespace GrpcAgent
{
    public interface IRpcService
    {
        void AddIPAdress(string ipaddress);

        void AddPort(int port);

        void Run();

    }
}
