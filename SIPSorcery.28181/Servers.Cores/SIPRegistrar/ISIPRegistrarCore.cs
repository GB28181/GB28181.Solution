using SIPSorcery.GB28181.SIP;

namespace SIPSorcery.GB28181.Servers
{
    public interface ISIPRegistrarCore
    {

        bool IsNeedAuthentication { get; }
        void ProcessRegisterRequest();

        void AddRegisterRequest(SIPEndPoint localSIPEndPoint, SIPEndPoint remoteEndPoint, SIPRequest registerRequest);
    }
}
