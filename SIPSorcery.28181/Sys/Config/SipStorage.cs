using SIPSorcery.GB28181.Persistence;
using SIPSorcery.GB28181.SIP.App;
using System;
using System.Collections.Generic;
using System.Linq;
/// <summary>
/// read configuraton and config the data storage
/// </summary>
namespace SIPSorcery.GB28181.Sys.Config
{
    public class SipStorage
    {
        private static readonly string m_storageTypeKey = SIPSorceryConfiguration.PERSISTENCE_STORAGETYPE_KEY;
        private static readonly string m_connStrKey = SIPSorceryConfiguration.PERSISTENCE_STORAGECONNSTR_KEY;
        private static readonly string m_XMLFilename = "gb28181.xml"; //default storage filename

        private static StorageTypes m_storageType;
        private static string m_connStr;

        private static SipStorage _instance;

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

        public static SipStorage Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new SipStorage();
                }
                return _instance;
            }
        }


        // here init the gb28181.xml file setting from app.config
        static SipStorage()
        {
            m_storageType = (AppState.GetConfigSetting(m_storageTypeKey) != null) ? StorageTypesConverter.GetStorageType(AppState.GetConfigSetting(m_storageTypeKey)) : StorageTypes.Unknown;
            m_connStr = AppState.GetConfigSetting(m_connStrKey);
            if (m_storageType == StorageTypes.SQLite)
            {
                var path = AppDomain.CurrentDomain.BaseDirectory + "Config\\";
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

            account.Added += Account_Added;

            _sipAccount = account;
            _accounts = account.Get();
        }

        private void Account_Added(SIPAccount asset)
        {
            throw new NotImplementedException();
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
