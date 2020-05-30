using GB28181.Server.Utils;
using GB28181.Service.Protos.AsClient.SystemConfig;
using GB28181.Sys;
using Grpc.Net.Client;
using System;
using System.Collections.Generic;
using GB28181.App;

namespace GB28181.Server.Main
{
    public partial class MainProcess
    {
        private List<SIPAccount> SipAccountStorage_RPCGBServerConfigReceived()
        {
            try
            {
                string SystemConfigurationServiceAddress = EnvironmentVariables.SystemConfigurationServiceAddress ?? "systemconfigurationservice:8080";
                logger.Debug("System Configuration Service Address: " + SystemConfigurationServiceAddress);
                //var channel = new  Channel(SystemConfigurationServiceAddress, ChannelCredentials.Insecure);
                var channel = GrpcChannel.ForAddress(SystemConfigurationServiceAddress);
                var client = new ServiceConfig.ServiceConfigClient(channel);
                GetIntegratedPlatformConfigRequest req = new GetIntegratedPlatformConfigRequest();
                GetIntegratedPlatformConfigResponse rep = client.GetIntegratedPlatformConfig(req);
                logger.Debug("GetIntegratedPlatformConfigResponse: " + rep.Config.ToString());
                GBPlatformConfig item = rep.Config;
                List<GB28181.App.SIPAccount> _lstSIPAccount = new List<GB28181.App.SIPAccount>();
                GB28181.App.SIPAccount obj = new GB28181.App.SIPAccount();
                obj.Id = Guid.NewGuid();
                //obj.Owner = item.Name;
                obj.GbVersion = string.IsNullOrEmpty(item.GbVersion) ? "GB-2016" : item.GbVersion;
                obj.LocalID = string.IsNullOrEmpty(item.LocalID) ? "34020000002000000001" : item.LocalID;
                obj.LocalIP = HostsEnv.GetRawIP();
                obj.LocalPort = string.IsNullOrEmpty(item.LocalPort) ? Convert.ToUInt16(5061) : Convert.ToUInt16(item.LocalPort);
                obj.RemotePort = string.IsNullOrEmpty(item.RemotePort) ? Convert.ToUInt16(5060) : Convert.ToUInt16(item.RemotePort);
                obj.Authentication = string.IsNullOrEmpty(item.Authentication) ? false : bool.Parse(item.Authentication);
                obj.SIPUsername = string.IsNullOrEmpty(item.SIPUsername) ? "admin" : item.SIPUsername;
                obj.SIPPassword = string.IsNullOrEmpty(item.SIPPassword) ? "123456" : item.SIPPassword;
                obj.MsgProtocol = System.Net.Sockets.ProtocolType.Udp;
                obj.StreamProtocol = System.Net.Sockets.ProtocolType.Udp;
                obj.TcpMode = GB28181.Net.RTP.TcpConnectMode.passive;
                obj.MsgEncode = string.IsNullOrEmpty(item.MsgEncode) ? "GB2312" : item.MsgEncode;
                obj.PacketOutOrder = string.IsNullOrEmpty(item.PacketOutOrder) ? true : bool.Parse(item.PacketOutOrder);
                obj.KeepaliveInterval = string.IsNullOrEmpty(item.KeepaliveInterval) ? Convert.ToUInt16(5000) : Convert.ToUInt16(item.KeepaliveInterval);
                obj.KeepaliveNumber = string.IsNullOrEmpty(item.KeepaliveNumber) ? Convert.ToByte(3) : Convert.ToByte(item.KeepaliveNumber);
                _lstSIPAccount.Add(obj);
                logger.Debug("GetIntegratedPlatformConfigResponse SIPAccount: {LocalID:" + obj.LocalID + ", LocalIP:" + obj.LocalIP + ", LocalPort:" + obj.LocalPort + ", RemotePort:"
                    + obj.RemotePort + ", SIPUsername:" + obj.SIPUsername + ", SIPPassword:" + obj.SIPPassword + ", KeepaliveInterval:" + obj.KeepaliveInterval + "}");
                return _lstSIPAccount;
            }
            catch (Exception ex)
            {
                logger.Warn("GetIntegratedPlatformConfigRequest: " + ex.Message);
                //logger.Debug("Can't get gb info from device-mgr, it will get gb info from config.");
                return null;
            }
        }
    }
}
