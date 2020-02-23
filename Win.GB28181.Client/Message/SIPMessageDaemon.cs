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
// This software is licensed under the BSD License http://www.opensource.org/licenses/bsd-license.php
//
// Copyright (c) 2009 Aaron Clauson (aaronc@blueface.ie), Blue Face Ltd, Dublin, Ireland (www.blueface.ie)
// All rights reserved.
//
// Redistribution and use in source and binary forms, with or without modification, are permitted provided that 
// the following conditions are met:
//
// Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer. 
// Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following 
// disclaimer in the documentation and/or other materials provided with the distribution. Neither the name of Blue Face Ltd. 
// nor the names of its contributors may be used to endorse or promote products derived from this software without specific 
// prior written permission. 
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, 
// BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. 
// IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, 
// OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, 
// OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, 
// OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE 
// POSSIBILITY OF SUCH DAMAGE.
// ============================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Xml;
using GB28181.SIPSorcery.Persistence;
using GB28181.SIPSorcery.SIP;
using GB28181.SIPSorcery.SIP.App;
using GB28181.SIPSorcery.Servers;
using GB28181.SIPSorcery.Sys;
using GB28181.Logger4Net;
using GB28181.SIPSorcery.Servers.SIPMessage;
using GB28181.SIPSorcery.Sys.Config;

namespace Win.GB28181.Client.Message
{
    public class SIPMessageDaemon
    {
        private ILog logger = AppState.logger;

        private SIPTransport m_sipTransport;

        private SIPAssetGetDelegate<SIPAccount> GetSIPAccount_External;
        private SIPAuthenticateRequestDelegate SIPAuthenticateRequest_External;
        private Dictionary<string, PlatformConfig> _platformList;
        private SIPAccount _account;
        public SIPMessageCoreService MessageCore;

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
                m_sipTransport = new SIPTransport(SIPDNSManager.ResolveSIPService, new SIPTransactionEngine(), false);
                m_sipTransport.PerformanceMonitorPrefix = SIPSorceryPerformanceMonitor.REGISTRAR_PREFIX;
                SIPAccount account = SipAccountStorage.Instance.Accounts.FirstOrDefault();

                var sipChannels = SIPTransportConfig.ParseSIPChannelsNode(account.LocalIP, account.LocalPort);
                m_sipTransport.AddSIPChannel(sipChannels);

                MessageCore = new SIPMessageCoreService(m_sipTransport, SIPConstants.SIP_SERVER_STRING);
               
                MessageCore.Initialize(SIPAuthenticateRequest_External, _platformList,_account);
                
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
