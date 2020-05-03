using GB28181.SIP.App;
using System.Collections.Generic;

namespace GB28181.Config
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
