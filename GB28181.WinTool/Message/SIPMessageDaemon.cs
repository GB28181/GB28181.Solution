// ============================================================================
// FileName: SIPRegistrarDaemon.cs
//
// Description:
// A daemon to configure and start a SIP Registration Agent.
//
// Author(s):
// Aaron Clauson
//
// History:
// 29 Mar 2009	Aaron Clauson	Created.
//
// License: 
// BSD 3-Clause "New" or "Revised" License, see included LICENSE.md file.
//


using GB28181.Logger4Net;
using GB28181.Servers;
using GB28181.Servers.SIPMessage;
using GB28181;
using GB28181.App;
using GB28181.Sys;
using GB28181.Config;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GB28181.WinTool.Message
{
    public class SIPMessageDaemon
    {
        private ILog logger = AppState.logger;

        private SIPTransport m_sipTransport;

     //   private SIPAssetGetDelegate<SIPAccount> GetSIPAccount_External;
        private SIPAuthenticateRequestDelegate SIPAuthenticateRequest_External;
        private Dictionary<string, PlatformConfig> _platformList;
        private SIPAccount _account;
        public SIPMessageCore MessageCore;

        public SIPMessageDaemon(
            SIPAccount account,
            SIPAuthenticateRequestDelegate sipRequestAuthenticator,
            Dictionary<string, PlatformConfig> platformList)
        {
            _account = account;
            SIPAuthenticateRequest_External = sipRequestAuthenticator;
            _platformList = platformList;
        }

        public void Start()
        {
            try
            {
                logger.Debug("SIP Registrar daemon starting...");


                // Configure the SIP transport layer.
                m_sipTransport = new SIPTransport(new SIPTransactionEngine(), false);
                m_sipTransport.PerformanceMonitorPrefix = SIPSorceryPerformanceMonitor.REGISTRAR_PREFIX;
                SIPAccount account = SipStorage.Instance.Accounts.FirstOrDefault();
                var sipChannels = SIPTransportConfig.ParseSIPChannelsNode(account.LocalIP, account.LocalPort);
                m_sipTransport.AddSIPChannel(sipChannels);


                MessageCore = new SIPMessageCore(m_sipTransport, SIPConstants.SIP_SERVER_STRING);
                MessageCore.Initialize(SIPAuthenticateRequest_External, _platformList, _account);
                GB28181Catalog.Instance.MessageCore = MessageCore;
                m_sipTransport.SIPTransportRequestReceived += MessageCore.AddMessageRequest;
                m_sipTransport.SIPTransportResponseReceived += MessageCore.AddMessageResponse;

                Console.ForegroundColor = ConsoleColor.Green;
                logger.Debug("SIP Registrar successfully started.");
                Console.ForegroundColor = ConsoleColor.White;
            }
            catch (Exception excp)
            {
                logger.Error("Exception SIPRegistrarDaemon Start. " + excp.Message);
            }
        }
        public void Stop()
        {
            try
            {
                logger.Debug("SIP Registrar daemon stopping...");

                logger.Debug("Shutting down SIP Transport.");
                m_sipTransport.Shutdown();

                logger.Debug("sip message service stopped.");
                MessageCore.Stop();

                logger.Debug("SIP Registrar daemon stopped.");
            }
            catch (Exception excp)
            {
                logger.Error("Exception SIPRegistrarDaemon Stop. " + excp.Message);
            }
        }
    }
}
