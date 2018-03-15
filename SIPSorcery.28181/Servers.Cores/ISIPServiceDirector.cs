using System;
using System.Net.Sockets;
namespace SIPSorcery.GB28181.Servers
{
    public interface ISIPServiceDirector
    {
        ISIPMonitorService GetTargetMonitorService(string gbid);


        //ip/port/protocol/ 
        Tuple<string, int, ProtocolType> MakeVideoRequest(string gbid, int[] mediaPort, string receiveIP);


    }
}
