
using System.Collections.Generic;

namespace SIPSorcery.GB28181.SIP
{
    public interface ISIPTransport
    {
        event SIPTransportRequestDelegate SIPTransportRequestReceived;
        event SIPTransportResponseDelegate SIPTransportResponseReceived;

        void Shutdown();
        void AddSIPChannel(List<SIPChannel> sipChannels);

        SIPRequest GetRequest(SIPMethodsEnum method, SIPURI uri);

        void SendRequest(SIPEndPoint dstEndPoint, SIPRequest sipRequest);

        void SendResponse(SIPResponse sipResponse);
        SIPNonInviteTransaction CreateNonInviteTransaction(SIPRequest sipRequest, SIPEndPoint dstEndPoint, SIPEndPoint localSIPEndPoint, SIPEndPoint outboundProxy);
        UASInviteTransaction CreateUASTransaction(SIPRequest sipRequest, SIPEndPoint dstEndPoint, SIPEndPoint localSIPEndPoint, SIPEndPoint outboundProxy, bool noCDR);
    }
}
