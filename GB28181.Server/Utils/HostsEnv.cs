using GB28181.Sys;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace GB28181.Server.Utils
{
    public static class HostsEnv
    {
        public static string GetIPAddress()
        {
            string hostname = Dns.GetHostName();
            IPHostEntry ipadrlist = Dns.GetHostEntry(hostname);
            IPAddress localaddr = null;
            foreach (var obj in ipadrlist.AddressList)
            {
                localaddr = obj;
            }
            //localip = localaddr.ToString();
            string localip = EnvironmentVariables.GbServiceLocalIp ?? localaddr.ToString();
            //logger.Debug("Gb Service Local Ip: " + localip);
            return localip;
        }

        public static IPAddress GetRawIP() =>  IPAddress.Parse(GetIPAddress());
        

    }
}
