using SIPSorcery.GB28181.Persistence;
using SIPSorcery.GB28181.SIP.App;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIPSorcery.GB28181.Sys.Config
{
    public class SIPSqlite
    {
        private static readonly string m_storageTypeKey = SIPSorceryConfiguration.PERSISTENCE_STORAGETYPE_KEY;
        private static readonly string m_connStrKey = SIPSorceryConfiguration.PERSISTENCE_STORAGECONNSTR_KEY;
        private static readonly string m_XMLFilename = "gb28181.xml";

        private static StorageTypes m_storageType;
        private static string m_connStr;

        private static SIPSqlite _instance;

        private List<SIPAccount> _accounts;

        public List<SIPAccount> Accounts
        {
            get { return _accounts; }
            set { _accounts = value; }
        }
        private SIPAssetPersistor<SIPAccount> _sipAccount;

        public SIPAssetPersistor<SIPAccount> SipAccount
        {
            get { return _sipAccount; }
        }

        public static SIPSqlite Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new SIPSqlite();
                }
                return _instance;
            }
        }

        static SIPSqlite()
        {
            string path = AppDomain.CurrentDomain.BaseDirectory + "Config\\";
            m_storageType = (AppState.GetConfigSetting(m_storageTypeKey) != null) ? StorageTypesConverter.GetStorageType(AppState.GetConfigSetting(m_storageTypeKey)) : StorageTypes.Unknown;
            m_connStr = AppState.GetConfigSetting(m_connStrKey);
            if (m_storageType == StorageTypes.SQLite)
            {
                m_connStr = string.Format(m_connStr, path);

            }
            if (m_storageType == StorageTypes.Unknown || m_connStr.IsNullOrBlank())
            {
                throw new ApplicationException("The SIP Registrar cannot start with no persistence settings.");
            }
        }

        public void Read()
        {
            SIPAssetPersistor<SIPAccount> account = SIPAssetPersistorFactory<SIPAccount>.CreateSIPAssetPersistor(m_storageType, m_connStr, m_XMLFilename);
            _sipAccount = account;
            _accounts = account.Get();
        }

        public void Save(SIPAccount account)
        {
            if (_accounts.Any(d => d.SIPUsername == account.SIPUsername || d.SIPDomain == account.SIPDomain))
            {
                SipAccount.Update(account);
            }
            else
            {
                SipAccount.Add(account);
                _accounts.Add(account);
            }
        }
    }
}
