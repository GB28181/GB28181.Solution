using GB28181.App;
using System.Collections.Generic;

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
