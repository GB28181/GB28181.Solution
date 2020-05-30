// ============================================================================
// FileName: SIPDomainManager.cs
//
// Description:
// Maintains a list of domains and domain aliases that can be used by various
// SIP Server agents. For example allows a SIP Registrar or Proxy to check the 
// domain on an incoming request to see if it is serviced at this location.
//
// Author(s):
// Aaron Clauson
//
// History:
// 27 Jul 2008	Aaron Clauson	Created.
//
// License: 
// BSD 3-Clause "New" or "Revised" License, see included LICENSE.md file.
//


using GB28181.Logger4Net;
using GB28181.Persistence;
using GB28181.Sys;
using SIPSorcery.Sys;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic;
using System.Linq.Expressions;

#if UNITTEST
using NUnit.Framework;
#endif

namespace GB28181.App
{
    public delegate string GetCanonicalDomainDelegate(string host, bool wildCardOk);     // Used to get the canonical domain from a host portion of a SIP URI.

    /// <summary>
    /// This class maintains a list of domains that are being maintained by this process.
    /// </summary>
    public class SIPDomainManager
    {
        public const string WILDCARD_DOMAIN_ALIAS = "*";
        public const string DEFAULT_LOCAL_DOMAIN = "local";

        private ILog logger = AppState.logger;

        //    private static readonly string m_storageFileName = AssemblyState.XML_DOMAINS_FILENAME;

        private static Dictionary<string, SIPDomain> m_domains = new Dictionary<string, SIPDomain>();  // Records the domains that are being maintained.
        private SIPAssetPersistor<SIPDomain> m_sipDomainPersistor;
        private SIPDomain m_wildCardDomain;

        public SIPDomainManager()
        { }

        public SIPDomainManager(SIPAssetPersistor<SIPDomain> sipDomain)
        {
            m_sipDomainPersistor = sipDomain;
            //m_sipDomainPersistor = SIPAssetPersistorFactory<SIPDomain>.CreateSIPAssetPersistor(storageType, storageConnectionStr, m_storageFileName);
            //m_sipDomainPersistor.Added += new SIPAssetDelegate<SIPDomain>(d => { LoadSIPDomains(); });
            //m_sipDomainPersistor.Deleted += new SIPAssetDelegate<SIPDomain>(d => { LoadSIPDomains(); });
            //m_sipDomainPersistor.Updated += new SIPAssetDelegate<SIPDomain>(d => { LoadSIPDomains(); });
            //m_sipDomainPersistor.Modified += new SIPAssetsModifiedDelegate(() => { LoadSIPDomains(); });
            LoadSIPDomains();
        }

        private void LoadSIPDomains()
        {
            try
            {
                var sipDomainList = m_sipDomainPersistor.Get(null, null, 0, Int32.MaxValue);

                if (sipDomainList == null || sipDomainList.Count == 0)
                {
                    throw new ApplicationException("No SIP domains could be loaded from the persistence store. There needs to be at least one domain.");
                }
                else
                {
                    m_domains.Clear();
                    sipDomainList.ForEach(sipDomain => AddDomain(sipDomain));
                }
            }
            catch (Exception excp)
            {
                logger.Error("Exception LoadSIPDomains. " + excp.Message);
                throw;
            }
        }

        public void AddDomain(SIPDomain sipDomain)
        {
            if (sipDomain == null)
            {
                //throw new ArgumentException("The SIPDomainManager cannot add a null SIPDomain object.");
                logger.Error("The SIPDomainManager cannot add a null SIPDomain object.");
            }
            else
            {
                if (!m_domains.ContainsKey(sipDomain.Domain.ToLower()))
                {
                    logger.Debug(" SIPDomainManager added domain: " + sipDomain.Domain + ".");
                    sipDomain.Aliases.ForEach(a => logger.Debug(" added domain alias " + a + "."));
                    m_domains.Add(sipDomain.Domain.ToLower(), sipDomain);

                    sipDomain.Aliases.ForEach(aliasItem =>
                    {
                        if (aliasItem == WILDCARD_DOMAIN_ALIAS && m_wildCardDomain == null)
                        {
                            m_wildCardDomain = sipDomain;
                            logger.Debug(" SIPDomainManager wildcard domain set to " + sipDomain.Domain + ".");
                        }
                    });

                }
                else
                {
                    logger.Warn("SIPDomainManager ignoring duplicate domain entry for " + sipDomain.Domain.ToLower() + ".");
                }
            }
        }

        public void RemoveDomain(SIPDomain sipDomain)
        {
            if (sipDomain != null)
            {
                if (m_domains.ContainsKey(sipDomain.Domain.ToLower()))
                {
                    m_domains.Remove(sipDomain.Domain.ToLower());
                }
            }
        }

        /// <summary>
        /// Checks whether there the supplied hostname represents a serviced domain or alias.
        /// </summary>
        /// <param name="host">The hostname to check for a serviced domain for.</param>
        /// <returns>The canconical domain name for the host if found or null if not.</returns>
        public string GetDomain(string host, bool wilcardOk)
        {
            SIPDomain domain = GetSIPDomain(host, wilcardOk);
            return domain?.Domain.ToLower();
        }

        private SIPDomain GetSIPDomain(string host, bool wildcardOk)
        {
            //logger.Debug("SIPDomainManager GetDomain for " + host + ".");

            if (host == null)
            {
                return null;
            }
            else
            {
                if (m_domains.ContainsKey(host.ToLower()))
                {
                    return m_domains[host.ToLower()];
                }
                else
                {

                    #region comment
                    //foreach (SIPDomain sipDomain in m_domains.Values)
                    //{
                    //    if (sipDomain.Aliases != null)
                    //    {
                    //        foreach (string alias in sipDomain.Aliases)
                    //        {
                    //            if (alias.ToLower() == host.ToLower())
                    //            {
                    //                return sipDomain;
                    //            }
                    //        }
                    //    }
                    //} 
                    #endregion

                    var allAliases = m_domains.Values.Where(domainItem => domainItem.Aliases != null);

                    var targetDomain = allAliases?.FirstOrDefault(domainItem => domainItem.Aliases.Exists(aliasItem => aliasItem.ToLower() == host.ToLower()));

                    if (targetDomain  == null && wildcardOk)
                    {
                         targetDomain = m_wildCardDomain;
                    }
                    else
                    {
                        targetDomain = targetDomain ?? null;
                    }

                    return targetDomain;
                }
            }
        }

        /// <summary>
        /// Checks whether a host name is in the list of supported domains and aliases.
        /// </summary>
        /// <param name="host"></param>
        /// <returns>True if the host is present as a domain or an alias, false otherwise.</returns>
        public bool HasDomain(string host, bool wildcardOk)
        {
            return GetDomain(host, wildcardOk) != null;
        }

        public List<SIPDomain> Get(Expression<Func<SIPDomain, bool>> whereClause, int offset, int count)
        {
            try
            {
                List<SIPDomain> subList = null;
                if (whereClause == null)
                {
                    subList = m_domains.Values.ToList();
                }
                else
                {
                    subList = m_domains.Values.Where(a => whereClause.Compile()(a)).ToList();
                }

                if (subList != null)
                {
                    if (offset >= 0)
                    {
                        if (count == 0 || count == Int32.MaxValue)
                        {
                            return subList.OrderBy(x => x.Domain).Skip(offset).ToList();
                        }
                        else
                        {
                            return subList.OrderBy(x => x.Domain).Skip(offset).Take(count).ToList();
                        }
                    }
                    else
                    {
                        return subList.OrderBy(x => x.Domain).ToList(); ;
                    }
                }

                return subList;
            }
            catch (Exception excp)
            {
                logger.Error("Exception SIPDomainManager Get. " + excp.Message);
                return null;
            }
        }

        public void AddAlias(string domain, string alias)
        {
            try
            {
                if (domain.IsNullOrBlank() || alias.IsNullOrBlank())
                {
                    logger.Warn("AddAlias was passed a null alias or domain.");
                }
                else if (!HasDomain(alias.ToLower(), false) && HasDomain(domain.ToLower(), false))
                {
                    SIPDomain sipDomain = GetSIPDomain(domain.ToLower(), false);
                    if (alias == WILDCARD_DOMAIN_ALIAS)
                    {
                        if (m_wildCardDomain != null)
                        {
                            m_wildCardDomain = sipDomain;
                            logger.Debug(" SIPDomainManager wildcard domain set to " + sipDomain.Domain + ".");
                        }
                    }
                    else
                    {
                        sipDomain.Aliases.Add(alias.ToLower());
                        logger.Debug(" SIPDomainManager added alias to " + sipDomain.Domain + " of " + alias.ToLower() + ".");
                    }
                }
                else
                {
                    logger.Warn("Could not add alias " + alias + " to domain " + domain + ".");
                }
            }
            catch (Exception excp)
            {
                logger.Error("Exception SIPDomainManager AddAlias. " + excp.Message);
            }
        }

        public void RemoveAlias(string alias)
        {
            try
            {
                if (alias.IsNullOrBlank())
                {
                    logger.Warn("RemoveAlias was passed a null alias.");
                }
                else if (HasDomain(alias.ToLower(), false))
                {
                    SIPDomain sipDomain = GetSIPDomain(alias.ToLower(), false);
                    sipDomain.Aliases.Remove(alias.ToLower());
                }
                else
                {
                    logger.Warn("Could not remove alias " + alias + ".");
                }
            }
            catch (Exception excp)
            {
                logger.Error("Exception SIPDomainManager RemoveAlias. " + excp.Message);
            }
        }
    }
}
