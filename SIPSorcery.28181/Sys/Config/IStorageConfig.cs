using SIPSorcery.GB28181.SIP.App;
using System.Collections.Generic;

namespace SIPSorcery.GB28181.Sys.Config
{
    public interface IStorageConfig
    {
        void Read();

        void Save(SIPAccount account);

        List<SIPAccount> Accounts { get; }
    }
}
