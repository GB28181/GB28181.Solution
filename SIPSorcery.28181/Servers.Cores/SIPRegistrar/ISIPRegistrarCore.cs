using SIPSorcery.GB28181.SIP;

namespace SIPSorcery.GB28181.Servers
{
    public interface ISIPRegistrarCore
    {

        bool IsNeedAuthentication { get; }
        void ProcessRegisterRequest();

        void AddRegisterRequest(SIPEndPoint localSIPEndPoint, SIPEndPoint remoteEndPoint, SIPRequest registerRequest);

        /// <summary>
        /// 设备注册到DMS
        /// </summary>
        event RPCDmsRegisterDelegate RPCDmsRegisterReceived;
        event DeviceAlarmSubscribeDelegate DeviceAlarmSubscribe;
    }
}
