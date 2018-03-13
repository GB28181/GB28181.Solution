using SIPSorcery.GB28181.SIP.App;

namespace SIPSorcery.GB28181.Sys.Config
{
    public interface IStorageConfig
    {
        void Read();

        void Save(SIPAccount account);
    }
}
