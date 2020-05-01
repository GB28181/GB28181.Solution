using GB28181.SIPSorcery.SIP.App;
using System.Collections.Generic;

namespace GB28181.SIPSorcery.Config
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
