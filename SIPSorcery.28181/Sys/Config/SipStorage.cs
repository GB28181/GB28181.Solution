using SIPSorcery.GB28181.Persistence;
using SIPSorcery.GB28181.SIP.App;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
/// <summary>
/// read configuraton and config the data storage
/// </summary>
namespace SIPSorcery.GB28181.Sys.Config
{
    public class SipAccountStorage : ISipAccount
    {
        private static readonly string m_storageTypeKey = SIPSorceryConfiguration.PERSISTENCE_STORAGETYPE_KEY;
        private static readonly string m_connStrKey = SIPSorceryConfiguration.PERSISTENCE_STORAGECONNSTR_KEY;
        private static readonly string m_XMLFilename = "gb28181.xml"; //default storage filename

        //数据存储类型，比如xml,json,sqlite.postgresql
        private static StorageTypes m_storageType;
        //连接字符串
        private static string m_connStr;

        //   private static SipStorage _instance;

        private List<SIPAccount> _accountsCache = null;

        public List<SIPAccount> Accounts
        {
            get
            {
                if (_accountsCache == null)
                {
                    Read();
                }
                return _accountsCache;
            }
        }

        public SIPAssetPersistor<SIPAccount> SipAccountDataStorage { get; private set; }

        //public static SipStorage Instance
        //{
        //    get
        //    {
        //        if (_instance == null)
        //        {
        //            _instance = new SipStorage();
        //        }
        //        return _instance;
        //    }
        //}


        // here init the gb28181.xml file setting from app.config
        static SipAccountStorage()
        {
            m_storageType = (AppState.GetConfigSetting(m_storageTypeKey) != null) ? StorageTypesConverter.GetStorageType(AppState.GetConfigSetting(m_storageTypeKey)) : StorageTypes.Unknown;

            var rootPath = AppDomain.CurrentDomain.BaseDirectory;
            m_connStr = Path.Combine(rootPath, AppState.GetConfigSetting(m_connStrKey));
            if (m_storageType == StorageTypes.SQLite)
            {
                m_connStr = string.Format(m_connStr, rootPath);
            }
            if (m_storageType == StorageTypes.Unknown || m_connStr.IsNullOrBlank())
            {
                throw new ApplicationException("The SIP Registrar cannot start with no persistence settings.");
            }
        }

        public SIPAccount GetLocalSipAccout()
        {

            var defaultAccount = Accounts.First();

            if (defaultAccount == null)
            {
                throw new ApplicationException("Account Config NULL,SIP not started");
            }

            return defaultAccount;
        }


        public void Read()
        {
            var accounts = SIPAssetPersistorFactory<SIPAccount>.CreateSIPAssetPersistor(m_storageType, m_connStr, m_XMLFilename);

            accounts.Added += Account_Added;

            SipAccountDataStorage = accounts;
            _accountsCache = accounts.Get();
        }

        private void Account_Added(SIPAccount asset)
        {
            throw new NotImplementedException();
        }

        public void Save(SIPAccount account)
        {
            if (_accountsCache.Any(d => d.SIPUsername == account.SIPUsername || d.SIPDomain == account.SIPDomain))
            {
                SipAccountDataStorage.Update(account);
            }
            else
            {
                SipAccountDataStorage.Add(account);
                _accountsCache.Add(account);
            }
        }


    }
}
