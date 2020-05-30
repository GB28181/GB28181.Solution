// ============================================================================
// FileName: SIPRegistrarState.cs
//
// Description:
// Application configuration for a SIP Registrar Server.
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
using System;
using System.Xml;

namespace GB28181.WinTool.Message
{
    /// <summary>
    /// Retrieves application conifguration settings from App.Config.
    /// </summary>
    public class SIPMessageState 
    {
        private const string LOGGER_NAME = "siprergistrar";

        public const string SIPREGISTRAR_CONFIGNODE_NAME = "sipServer";

        private const string SIPSOCKETS_CONFIGNODE_NAME = "sipsockets";
        private const string USERAGENTS_CONFIGNODE_NAME = "useragentconfigs";
        private const string MONITOR_LOOPBACK_PORT_KEY = "MonitorLoopbackPort";
        private const string MAXIMUM_ACCOUNT_BINDINGS_KEY = "MaximumAccountBindings";
        private const string NATKEEPALIVE_RELAY_SOCKET = "NATKeepAliveRelaySocket";
        //private const string SWITCHBOARD_CERTIFICATE_NAME_KEY = "SwitchboardCertificateName";
        private const string SWITCHBOARD_USERAGNET_PREFIX_KEY = "SwitchboardUserAgentPrefix";
        private const string THREAD_COUNT_KEY = "ThreadCount";

        public static ILog logger;

        private static readonly XmlNode m_sipRegistrarNode;
        public static readonly XmlNode SIPRegistrarSocketsNode;
        public static readonly XmlNode UserAgentsConfigNode;

        static SIPMessageState()
        {
            try
            {
                #region Configure logging.

                try
                {
                  //  log4net.Config.XmlConfigurator.Configure();
                    logger = LogManager.GetLogger(LOGGER_NAME);
                }
                catch (Exception logExcp)
                {
                    Console.WriteLine("Exception SIPMessageState Configure Logging. " + logExcp.Message);
                }

                #endregion

                //if (AppState.GetSection(SIPREGISTRAR_CONFIGNODE_NAME) != null)
                //{
                //    m_sipRegistrarNode = (XmlNode)AppState.GetSection(SIPREGISTRAR_CONFIGNODE_NAME);
                //}

                XmlDocument doc = new XmlDocument();
                string xml = AppDomain.CurrentDomain.BaseDirectory + "Config\\SipSocket.xml";
                doc.Load(xml);
                m_sipRegistrarNode = doc.SelectNodes("sipServer")[0];

                if (m_sipRegistrarNode == null)
                {
                    //throw new ApplicationException("The SIP Registrar could not be started, no " + SIPREGISTRAR_CONFIGNODE_NAME + " config node available.");
                    logger.Warn("The SIP Registrar " + SIPREGISTRAR_CONFIGNODE_NAME + " config node was not available, the agent will not be able to start.");
                }
                else
                {
                    SIPRegistrarSocketsNode = m_sipRegistrarNode.SelectSingleNode(SIPSOCKETS_CONFIGNODE_NAME);
                    if (SIPRegistrarSocketsNode == null)
                    {
                        throw new ApplicationException("The SIP Registrar could not be started, no " + SIPSOCKETS_CONFIGNODE_NAME + " node could be found.");
                    }

                    UserAgentsConfigNode = m_sipRegistrarNode.SelectSingleNode(USERAGENTS_CONFIGNODE_NAME);
                }
            }
            catch (Exception excp)
            {
                logger.Error("Exception SIPRegistrarState. " + excp.Message);
                throw;
            }
        }
    }
}
