using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GB28181.App;
using GB28181.Logger4Net;
using GB28181.Persistence;
using GB28181.Sys;
using Microsoft.Extensions.Configuration;
using SIPSorcery.Sys;
/// <summary>
/// read configuraton and config the data storage
/// </summary>
namespace GB28181.Config
{
    public class SipStorage : ISipStorage
    {
        private static readonly ILog logger = AppState.logger;
        private const string m_storageTypeKey = SIPSorceryConfiguration.PERSISTENCE_STORAGETYPE_KEY;
        private const string m_connStrKey = SIPSorceryConfiguration.PERSISTENCE_STORAGECONNSTR_KEY;
        private const string m_XMLFilename = "gb28181.xml"; //default storage filename

        public static readonly SipStorage Instance = new SipStorage();
        //数据存储类型，比如xml,json,sqlite.postgresql
        private static Sys.StorageTypes m_storageType;
        //连接字符串
        private static string m_connStr;

        //   private static SipStorage _instance;

        private static List<SIPAccount> _sipAccountsCache = null;
        private static bool _haveGBConfig = false;
        /// <summary>
        /// 读取本地的gb28181.xml获取 GB Server Config，作为默认
        /// 微服务架构中，往往需要从Config Server中通过GRPC获取，项目中已经增了通过GRPC获取配置的实现
        /// </summary>
        public static event RPCGBServerConfigDelegate RPCGBServerConfigReceived;

        private readonly IConfiguration _configuration;


        public SipStorage(IConfiguration configuration)
        {
            //依赖注入的方式，获取appsettings.json的配置
            _configuration = configuration;
        }

       public SipStorage() { }

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
       static SipStorage()
        {
            m_storageType = (AppState.GetConfigSetting(m_storageTypeKey) != null) ? Sys.StorageTypesConverter.GetStorageType(AppState.GetConfigSetting(m_storageTypeKey)) : Sys.StorageTypes.Unknown;

            var rootPath = AppDomain.CurrentDomain.BaseDirectory;
            m_connStr = Path.Combine(rootPath, AppState.GetConfigSetting(m_connStrKey));
            if (m_storageType == Sys.StorageTypes.SQLite)
            {
                m_connStr = string.Format(m_connStr, rootPath);
            }
            if (m_storageType == Sys.StorageTypes.Unknown || m_connStr.IsNullOrBlank())
            {
                logger.Error($"The SIP Registrar cannot start with no persistence settings:m_storageType: {m_storageType},m_connStr :{m_connStr}.");
                //throw new ApplicationException("The SIP Registrar cannot start with no persistence settings.");
            }
        }

        public SIPAccount GetLocalSipAccout()
        {

            if (Accounts == null)
            {
                throw new ApplicationException("Accounts is NULL,SIP not started");
            }
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
