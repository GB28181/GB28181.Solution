using GB28181.SIPSorcery.Persistence;
using GB28181.SIPSorcery.SIP.App;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using GB28181.SIPSorcery.SIP;
using GB28181.Logger4Net;

/// <summary>
/// read configuraton and config the data storage
/// </summary>
namespace GB28181.SIPSorcery.Sys.Config
{
    public class SipAccountStorage : ISipAccountStorage
    {
        private static ILog logger = AppState.logger;
        private const string m_storageTypeKey = SIPSorceryConfiguration.PERSISTENCE_STORAGETYPE_KEY;
        private const string m_connStrKey = SIPSorceryConfiguration.PERSISTENCE_STORAGECONNSTR_KEY;
        private const string m_XMLFilename = "gb28181.xml"; //default storage filename

        public static readonly SipAccountStorage Instance = new SipAccountStorage();
        //数据存储类型，比如xml,json,sqlite.postgresql
        private static StorageTypes m_storageType;
        //连接字符串
        private static string m_connStr;

        //   private static SipStorage _instance;

        private static List<SIPAccount> _sipAccountsCache = null;
        private static bool _haveGBConfig = false;
        /// <summary>
        /// 获取 GB Server Config
        /// </summary>
        public static event RPCGBServerConfigDelegate RPCGBServerConfigReceived;

        public List<SIPAccount> Accounts
        {
            get
            {
                if (RPCGBServerConfigReceived != null && !_haveGBConfig)
                {
                    List<SIPAccount> lstSIPAccount = RPCGBServerConfigReceived?.Invoke();
                    if (lstSIPAccount != null && lstSIPAccount.Count > 0)
                    {
                        _sipAccountsCache = lstSIPAccount;
                        _haveGBConfig = true;
                    }
                    else if (_sipAccountsCache == null)
                    {
                        logger.Debug("Get GB server config failed, but it's running with xml config.");
                    }
                }
                if (_sipAccountsCache == null)
                {
                    Read();
                }
                return _sipAccountsCache;
            }
        }

        public SIPAssetPersistor<SIPAccount> SipAccountDataStorage { get; private set; }

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

            if (Accounts == null)
            {
                throw new ApplicationException("Accounts is NULL,SIP not started");
            }
            //else
            //{
            //    throw new ApplicationException("Accounts is not NULL:" + Accounts.Count);
            //}

            var defaultAccount = Accounts.FirstOrDefault();

            if (defaultAccount == null)
            {
                throw new ApplicationException("Account Config NULL,SIP not started");
            }

            return defaultAccount;
        }


        public void Read()
        {
            SipAccountDataStorage = SIPAssetPersistorFactory<SIPAccount>.CreateSIPAssetPersistor(m_storageType, m_connStr, m_XMLFilename);
            SipAccountDataStorage.Added += Account_Added;
            _sipAccountsCache = SipAccountDataStorage.Get();
        }

        private void Account_Added(SIPAccount asset)
        {
            throw new NotImplementedException();
        }

        public void Save(SIPAccount account)
        {
            if (_sipAccountsCache.Any(d => d.SIPUsername == account.SIPUsername || d.SIPDomain == account.SIPDomain))
            {
                SipAccountDataStorage.Update(account);
            }
            else
            {
                SipAccountDataStorage.Add(account);
                _sipAccountsCache.Add(account);
            }
        }


    }
}
