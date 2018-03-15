using System;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace SIPSorcery.GB28181.Servers
{
    public interface ISIPServiceDirector
    {
        ISIPMonitorService GetTargetMonitorService(string gbid);


        //ip/port/protocol/ 
        Task<Tuple<string, int, ProtocolType>> MakeVideoRequest(string gbid, int[] mediaPort, string receiveIP);


    }
}
