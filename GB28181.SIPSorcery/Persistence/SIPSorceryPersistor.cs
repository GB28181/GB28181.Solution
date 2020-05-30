// ============================================================================
// FileName: SIPSorceryPersistor.cs
//
// Description:
// Handles persistence for the SIP Sorcery Application Server.
//
// Author(s):
// Aaron Clauson
//
// History:
// 14 Sep 2008	Aaron Clauson	Created.
//
// License: 
// BSD 3-Clause "New" or "Revised" License, see included LICENSE.md file.
//


using System;
using System.Collections.Generic;
using System.IO;
using GB28181;
using GB28181.App;
using GB28181.Sys;
using GB28181.Logger4Net;

namespace GB28181.Persistence
{
    public class SIPSorceryPersistor
    {
        private const string WRITE_CDRS_THREAD_NAME = "sipappsvr-writecdrs";

        private ILog logger = AppState.logger;

        private static readonly string m_sipAccountsXMLFilename = AssemblyState.XML_SIPACCOUNTS_FILENAME;
        private static readonly string m_sipProvidersXMLFilename = AssemblyState.XML_SIPPROVIDERS_FILENAME;
        private static readonly string m_sipDialplansXMLFilename = AssemblyState.XML_DIALPLANS_FILENAME;
        private static readonly string m_sipRegistrarBindingsXMLFilename = AssemblyState.XML_REGISTRAR_BINDINGS_FILENAME;
        private static readonly string m_sipProviderBindingsXMLFilename = AssemblyState.XML_PROVIDER_BINDINGS_FILENAME;
        private static readonly string m_sipDialoguesXMLFilename = AssemblyState.XML_SIPDIALOGUES_FILENAME;
        private static readonly string m_sipCDRsXMLFilename = AssemblyState.XML_SIPCDRS_FILENAME;

        private SIPAssetPersistor<SIPAccount> m_sipAccountsPersistor;
        public SIPAssetPersistor<SIPAccount> SIPAccountsPersistor => m_sipAccountsPersistor;

        private SIPAssetPersistor<SIPDialPlan> m_dialPlanPersistor;
        public SIPAssetPersistor<SIPDialPlan> SIPDialPlanPersistor => m_dialPlanPersistor;

        private SIPAssetPersistor<SIPProvider> m_sipProvidersPersistor;
        public SIPAssetPersistor<SIPProvider> SIPProvidersPersistor => m_sipProvidersPersistor;

        private SIPAssetPersistor<SIPProviderBinding> m_sipProviderBindingsPersistor;
        public SIPAssetPersistor<SIPProviderBinding> SIPProviderBindingsPersistor => m_sipProviderBindingsPersistor;

        private SIPDomainManager m_sipDomainManager;
        public SIPDomainManager SIPDomainManager => m_sipDomainManager;

        private SIPAssetPersistor<SIPRegistrarBinding> m_sipRegistrarBindingPersistor;
        public SIPAssetPersistor<SIPRegistrarBinding> SIPRegistrarBindingPersistor => m_sipRegistrarBindingPersistor;

        private SIPAssetPersistor<SIPDialogueAsset> m_sipDialoguePersistor;
        public SIPAssetPersistor<SIPDialogueAsset> SIPDialoguePersistor => m_sipDialoguePersistor;

        private SIPAssetPersistor<SIPCDRAsset> m_sipCDRPersistor;
        public SIPAssetPersistor<SIPCDRAsset> SIPCDRPersistor => m_sipCDRPersistor;

        public bool StopCDRWrites;
        private Queue<SIPCDR> m_pendingCDRs = new Queue<SIPCDR>();

        public SIPSorceryPersistor(StorageTypes storageType, string storageConnectionStr)
        {
            if (storageType == StorageTypes.XML)
            {
                if (!storageConnectionStr.Contains(":"))
                {
                    // Relative path.
                    storageConnectionStr = AppDomain.CurrentDomain.BaseDirectory + storageConnectionStr;
                }

                if (!storageConnectionStr.EndsWith(@"\"))
                {
                    storageConnectionStr += @"\";
                }

                if (!Directory.Exists(storageConnectionStr))
                {
                    throw new ApplicationException("Directory " + storageConnectionStr + " does not exist for XML persistor.");
                }
            }

            m_sipAccountsPersistor = SIPAssetPersistorFactory<SIPAccount>.CreateSIPAssetPersistor(storageType, storageConnectionStr, m_sipAccountsXMLFilename);
            m_dialPlanPersistor = SIPAssetPersistorFactory<SIPDialPlan>.CreateSIPAssetPersistor(storageType, storageConnectionStr, m_sipDialplansXMLFilename);
            m_sipProvidersPersistor = SIPAssetPersistorFactory<SIPProvider>.CreateSIPAssetPersistor(storageType, storageConnectionStr, m_sipProvidersXMLFilename);
            m_sipProviderBindingsPersistor = SIPAssetPersistorFactory<SIPProviderBinding>.CreateSIPAssetPersistor(storageType, storageConnectionStr, m_sipProviderBindingsXMLFilename);
            var sipDomainAssetPersistor = SIPAssetPersistorFactory<SIPDomain>.CreateSIPAssetPersistor(storageType, storageConnectionStr, m_sipProvidersXMLFilename);
            m_sipDomainManager = new SIPDomainManager(sipDomainAssetPersistor);
            m_sipRegistrarBindingPersistor = SIPAssetPersistorFactory<SIPRegistrarBinding>.CreateSIPAssetPersistor(storageType, storageConnectionStr, m_sipRegistrarBindingsXMLFilename);
            m_sipDialoguePersistor = SIPAssetPersistorFactory<SIPDialogueAsset>.CreateSIPAssetPersistor(storageType, storageConnectionStr, m_sipDialoguesXMLFilename);
            m_sipCDRPersistor = SIPAssetPersistorFactory<SIPCDRAsset>.CreateSIPAssetPersistor(storageType, storageConnectionStr, m_sipCDRsXMLFilename);

            //if (m_sipCDRPersistor != null)
            //{
            //    ThreadPool.QueueUserWorkItem(delegate { WriteCDRs(); });
            //}
        }

        public void WriteCDR(SIPCDR cdr)
        {
            try
            {
                //if (m_sipCDRPersistor != null && !StopCDRWrites && !m_pendingCDRs.Contains(cdr))
                //{
                //    m_pendingCDRs.Enqueue(cdr);
                //}

                SIPCDRAsset cdrAsset = new SIPCDRAsset(cdr);

                var existingCDR = m_sipCDRPersistor.Get(cdrAsset.Id);

                if (existingCDR == null)
                {
                    cdrAsset.Inserted = DateTimeOffset.UtcNow;
                    m_sipCDRPersistor.Add(cdrAsset);
                }
                else //if (existingCDR.ReconciliationResult == null)
                {
                    m_sipCDRPersistor.Update(cdrAsset);
                }
            }
            catch (Exception excp)
            {
                logger.Error("Exception QueueCDR. " + excp.Message);
            }
        }

        //    private void WriteCDRs()
        //    {
        //        try
        //        {
        //            Thread.CurrentThread.Name = WRITE_CDRS_THREAD_NAME;

        //            while (!StopCDRWrites || m_pendingCDRs.Count > 0)
        //            {
        //                try
        //                {
        //                    if (m_pendingCDRs.Count > 0)
        //                    {
        //                        SIPCDRAsset cdrAsset = new SIPCDRAsset(m_pendingCDRs.Dequeue());

        //                        // Check whether the CDR has been hungup already in which case no more updates are permitted.
        //                        var existingCDR = m_sipCDRPersistor.Get(cdrAsset.Id);

        //                        if (existingCDR == null)
        //                        {
        //                            cdrAsset.Inserted = DateTimeOffset.UtcNow;
        //                            m_sipCDRPersistor.Add(cdrAsset);
        //                        }
        //                        else //if (existingCDR.ReconciliationResult == null)
        //                        {
        //                            m_sipCDRPersistor.Update(cdrAsset);
        //                        }
        //                        //else
        //                        //{
        //                        //    logger.Warn("A CDR was not updated as the copy in the database had already been processed by the RTCC engine (" + existingCDR.Id + ").");
        //                        //}
        //                    }
        //                    else
        //                    {
        //                        Thread.Sleep(1000);
        //                    }
        //                }
        //                catch (Exception writeExcp)
        //                {
        //                    logger.Error("Exception WriteCDRs writing CDR. " + writeExcp.Message);
        //                }
        //            }
        //        }
        //        catch (Exception excp)
        //        {
        //            logger.Error("Exception WriteCDRs. " + excp.Message);
        //        }
        //    }
    }
}
