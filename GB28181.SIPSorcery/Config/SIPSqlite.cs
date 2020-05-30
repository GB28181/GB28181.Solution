using System;
using System.Collections.Generic;
using System.Linq;
using GB28181.Logger4Net;
using GB28181.Persistence;
using GB28181.App;
using SIPSorcery.Sys;
using GB28181.Sys;


namespace GB28181.Config
{
    public class SIPSqlite
    {
        private static readonly string m_storageTypeKey = SIPSorceryConfiguration.PERSISTENCE_STORAGETYPE_KEY;
        private static readonly string m_connStrKey = SIPSorceryConfiguration.PERSISTENCE_STORAGECONNSTR_KEY;
        private static readonly string m_XMLFilename = "gb28181.xml";
        private static readonly ILog logger = AppState.logger;
        private static Sys.StorageTypes m_storageType;
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
            m_storageType = (AppState.GetConfigSetting(m_storageTypeKey) != null) ? Sys.StorageTypesConverter.GetStorageType(AppState.GetConfigSetting(m_storageTypeKey)) : Sys.StorageTypes.Unknown;
            m_connStr = AppState.GetConfigSetting(m_connStrKey);
            if (m_storageType == Sys.StorageTypes.SQLite)
            {
                m_connStr = string.Format(m_connStr, path);

            }
            if (m_storageType == Sys.StorageTypes.Unknown || m_connStr.IsNullOrBlank())
            {
                logger.Error($"The SIP Registrar cannot start with no persistence settings:m_storageType: {m_storageType},m_connStr :{m_connStr}.");
              //  throw new ApplicationException("The SIP Registrar cannot start with no persistence settings.");
            }
        }

        public void Read()
        {
            var account = SIPAssetPersistorFactory<SIPAccount>.CreateSIPAssetPersistor(m_storageType, m_connStr, m_XMLFilename);
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
