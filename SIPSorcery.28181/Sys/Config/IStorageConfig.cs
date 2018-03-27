using SIPSorcery.GB28181.SIP.App;
using System.Collections.Generic;
using System.Net.Sockets;

namespace SIPSorcery.GB28181.Sys.Config
{
    public interface ISipAccountStorage
    {

        void Read();

        void Save(SIPAccount account);

        List<SIPAccount> Accounts { get; }


        //Get Local Default SipDomain Info
        SIPAccount GetLocalSipAccout();

    }
}
