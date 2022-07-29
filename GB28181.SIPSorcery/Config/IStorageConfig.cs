using System.Collections.Generic;
using GB28181.App;

namespace GB28181.Config
{
    public interface ISipStorage
    {

        void Read();

        void Save(SIPAccount account);

        List<SIPAccount> Accounts { get; }

        //Get Local Default SipDomain Info
        SIPAccount GetLocalSipAccout();

    }
}
