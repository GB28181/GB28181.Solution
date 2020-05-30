
using System.Collections.Generic;
using SIPSorcery.SIP;

namespace GB28181
{
    public interface ISIPTransport
    {
        event SIPTransportRequestDelegate SIPTransportRequestReceived;
        event SIPTransportResponseDelegate SIPTransportResponseReceived;

        string PerformanceMonitorPrefix { get; set; }                              // Allows an application to set the prefix for the performance monitor counter it wants to use for tracking the SIP transport metrics.
        string MsgEncode { get; set; }

        void Shutdown();
        void AddSIPChannel(List<SIPChannel> sipChannels);

        SIPRequest GetRequest(SIPMethodsEnum method, SIPURI uri);

        void SendRequest(SIPEndPoint dstEndPoint, SIPRequest sipRequest);

        void SendResponse(SIPResponse sipResponse);
        SIPNonInviteTransaction CreateNonInviteTransaction(SIPRequest sipRequest, SIPEndPoint dstEndPoint, SIPEndPoint localSIPEndPoint, SIPEndPoint outboundProxy);
        UASInviteTransaction CreateUASTransaction(SIPRequest sipRequest, SIPEndPoint dstEndPoint, SIPEndPoint localSIPEndPoint, SIPEndPoint outboundProxy, bool noCDR = false);
    }
}
