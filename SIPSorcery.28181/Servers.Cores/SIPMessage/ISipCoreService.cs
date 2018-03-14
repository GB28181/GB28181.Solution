using SIPSorcery.GB28181.SIP;
using System;
using System.Collections.Generic;
using System.Text;

namespace SIPSorcery.GB28181.Servers.SIPMessage
{
    public interface ISipCoreService
    {
        void Start();
        void Stop();
        void AddMessageRequest(SIPEndPoint localEP, SIPEndPoint remoteEP, SIPRequest request);

        void AddMessageResponse(SIPEndPoint localEP, SIPEndPoint remoteEP, SIPResponse response);
    }
}
