using Consul;
using GB28181.Logger4Net;
using GB28181.Sys;
using System;
using System.Net;
using GB28181.Server.Utils;

namespace GB28181.Server.Main
{
    public class ServicesRegistry
    {
        private static readonly ILog logger = AppState.GetLogger("ServicesRegistry");

        private AgentServiceRegistration _AgentServiceRegistration;

        /// <summary>
        /// Consul Register
        /// </summary>
        /// <param name="client"></param>
        public void ServiceRegister()
        {
            try
            {
                var clients = new ConsulClient(ConfigurationOverview);
                _AgentServiceRegistration = new AgentServiceRegistration()
                {
                    Address = HostsEnv.GetIPAddress(),
                    ID = "gbdeviceservice",//"gb28181" + Dns.GetHostName(),
                    Name = "gbdeviceservice",
                    Port = EnvironmentVariables.GBServerGrpcPort,
                    Tags = new[] { "gb28181" }
                };
                var result = clients.Agent.ServiceRegister(_AgentServiceRegistration).Result;
            }
            catch (Exception ex)
            {
                logger.Error("Consul Register: " + ex.Message);
                throw ex;
            }
        }
        private void ConfigurationOverview(ConsulClientConfiguration obj)
        {
            obj.Address = new Uri("http://" + (EnvironmentVariables.MicroRegistryAddress ?? HostsEnv.GetIPAddress() + ":8500"));
            logger.Debug("Consul Client: " + obj.Address);
            obj.Datacenter = "dc1";
        }
    }
}
