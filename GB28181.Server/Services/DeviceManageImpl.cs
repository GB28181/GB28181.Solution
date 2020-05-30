using System;
using GB28181.Logger4Net;
using GB28181.Servers;
using GB28181;
using GB28181.Sys;
using Grpc.Net.Client;

namespace GB28181.Service.Protos.AsClient.DeviceManagement
{
    public class DeviceManageImpl : DevicesManager.DevicesManagerClient
    {
        private static ILog logger = LogManager.GetLogger("RpcServer");
        private ISIPRegistrarCore _sipRegistrarCore = null;

        public DeviceManageImpl(ISIPRegistrarCore sipRegistrarCore)
        {
            _sipRegistrarCore = sipRegistrarCore;
            //_sipRegistrarCore.RPCDmsRegisterReceived += _sipRegistrarCore_RPCDmsRegisterReceived;
        }

        private void _sipRegistrarCore_RPCDmsRegisterReceived(SIPTransaction sipTransaction, GB28181.App.SIPAccount sIPAccount)
        {
            try
            {
                Device _device = new Device();
                SIPRequest sipRequest = sipTransaction.TransactionRequest;
                _device.Guid = Guid.NewGuid().ToString();
                _device.IP = sipTransaction.TransactionRequest.RemoteSIPEndPoint.Address.ToString();//IPC
                _device.Name = "gb" + _device.IP;
                _device.LoginUser.Add(new LoginUser() { LoginName = sIPAccount.SIPUsername ?? "admin", LoginPwd = sIPAccount.SIPPassword ?? "123456" });
                _device.Port = Convert.ToUInt32(sipTransaction.TransactionRequest.RemoteSIPEndPoint.Port);//5060
                _device.GBID = sipTransaction.TransactionRequestFrom.URI.User;//42010000001180000184
                _device.PtzType = 0;
                _device.ProtocolType = 0;
                _device.ShapeType = ShapeType.Dome;
                //var options = new List<ChannelOption> { new ChannelOption(ChannelOptions.MaxMessageLength, int.MaxValue) };
                //  var channel = new Channel(EnvironmentVariables.DeviceManagementServiceAddress ?? "devicemanagementservice:8080", ChannelCredentials.Insecure);
                 var channel =  GrpcChannel.ForAddress(EnvironmentVariables.DeviceManagementServiceAddress ?? "devicemanagementservice:8080");

                logger.Debug("Device Management Service Address: " + (EnvironmentVariables.DeviceManagementServiceAddress ?? "devicemanagementservice:8080"));
                var client = new DevicesManager.DevicesManagerClient(channel);
                AddDeviceRequest _AddDeviceRequest = new AddDeviceRequest();
                _AddDeviceRequest.Device.Add(_device);
                _AddDeviceRequest.LoginRoleId = "XXXX";
                var reply = client.AddDevice(_AddDeviceRequest);
                if (reply.Status == OP_RESULT_STATUS.OpSuccess)
                {
                    logger.Debug("Device[" + sipTransaction.TransactionRequest.RemoteSIPEndPoint + "] have completed registering DMS service.");
                }
                else {
                    logger.Error("_sipRegistrarCore_RPCDmsRegisterReceived: " + reply.Status.ToString());
                }
            }
            catch (Exception ex)
            {
                logger.Error("Device[" + sipTransaction.TransactionRequest.RemoteSIPEndPoint + "] register DMS failed: " + ex.Message);
            }
        }
    }
}
