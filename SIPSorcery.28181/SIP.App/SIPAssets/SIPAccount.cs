// ============================================================================
// FileName: SIPAccount.cs
//
// Description:
// Represents a SIP account that holds authentication information and additional settings
// for SIP accounts.
//
// Author(s):
// Aaron Clauson
//
// History:
// 10 May 2008  Aaron Clauson   Created.
//
// License: 
// This software is licensed under the BSD License http://www.opensource.org/licenses/bsd-license.php
//
// Copyright (c) 2008 Aaron Clauson (aaron@sipsorcery.com), SIP Sorcery PTY LTD, Hobart, Australia (www.sipsorcery.com)
// All rights reserved.
//
// Redistribution and use in source and binary forms, with or without modification, are permitted provided that 
// the following conditions are met:
//
// Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer. 
// Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following 
// disclaimer in the documentation and/or other materials provided with the distribution. Neither the name of SIP Sorcery PTY LTD. 
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

using Logger4Net;
using SIPSorcery.GB28181.Net.RTP;
using SIPSorcery.GB28181.Sys;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using System.Xml;

namespace SIPSorcery.GB28181.SIP.App
{
    /// <remarks>
    /// SIP account usernames can be treated by some SIP Sorcery server agents as domain name like structures where a username of
    /// "x.username" will match the "username" account for receiving calls. To facilitate this SIP accounts with a '.' character in them
    /// can only be created where the suffix "username" portion matches the Owner field. This allows users to create SIP accounts with '.'
    /// in them but will prevent a different user from being able to hijack an "x.username" account and caue unexpected behaviour.
    /// </remarks>
   // [Table(Name = "sipaccounts")]
    [DataContract]
    public class SIPAccount : INotifyPropertyChanged, ISIPAsset
    {
        public const string XML_DOCUMENT_ELEMENT_NAME = "sipaccounts";
        public const string XML_ELEMENT_NAME = "sipaccount";
        public const int PASSWORD_MIN_LENGTH = 6;
        public const int PASSWORD_MAX_LENGTH = 15;
        public const int USERNAME_MIN_LENGTH = 5;
        private const string BANNED_SIPACCOUNT_NAMES = "dispatcher";

        //public static readonly string SelectQuery = "select * from sipaccounts where sipusername = ?1 and sipdomain = ?2";
        // Only non-printable non-alphanumeric ASCII characters missing are ; \ and space. The semi-colon isn't accepted by 
        // Netgears and the space has the potential to create too much confusion with the users and \ with the system.
        public static readonly char[] NONAPLPHANUM_ALLOWED_PASSWORD_CHARS = new char[] { '!', '"', '$', '%', '&', '(', ')', '*', '+', ',', '.', '/', ':', '<', '=', '>', '?', '@', '[', ']', '^', '_', '`', '{', '|', '}', '~' };
        public static readonly string USERNAME_ALLOWED_CHARS = @"a-zA-Z0-9_\-\.";

        private static ILog logger = AppState.logger;
        private static string m_newLine = AppState.NewLine;

        public static int TimeZoneOffsetMinutes;
        //  // [Column(Name = "id", DbType = "varchar(36)", IsPrimaryKey = true, CanBeNull = false, UpdateCheck = UpdateCheck.Never)]
        [DataMember]
        public Guid Id { get; set; }

        private string m_owner;                 // The username of the account that owns this SIP account.
                                                //   // [Column(Name = "owner", DbType = "varchar(32)", CanBeNull = false, UpdateCheck = UpdateCheck.Never)]
        [DataMember]
        public string Owner
        {
            get { return m_owner; }
            set
            {
                m_owner = value;
                NotifyPropertyChanged("Owner");
            }
        }

        #region 国标属性
        private IPAddress m_localIP;
        private ushort m_localPort;
        private bool m_authentication;
        private string m_sipUsername;
        private string m_sipPassword;
        private ProtocolType _msgProtocol;
        private ProtocolType _streamProtocol;
        private TcpConnectMode _tcpMode;
        private bool m_packetOutOrder;
        private ushort m_keepaliveInterval;
        private byte m_keepaliveNumber;
        private IPAddress m_OutputIP;

        /// <summary>
        /// GB Version
        /// </summary>
        public string GbVersion { get; set; }

        /// <summary>
        /// 本地国标编码
        /// </summary>
        public string LocalID { get; set; }

        /// <summary>
        /// 本地IP地址
        /// </summary>
        public IPAddress LocalIP
        {
            get { return m_localIP; }
            set { m_localIP = value; }
        }

        /// <summary>
        /// 本地端口号
        /// </summary>
        public ushort LocalPort
        {
            get { return m_localPort; }
            set { m_localPort = value; }
        }

        /// <summary>
        /// 下级平台国标编码
        /// </summary>
        public string RemoteID { get; set; }

        /// <summary>
        /// 下级平台国标IP地址
        /// </summary>
        public IPAddress RemoteIP { get; set; }

        /// <summary>
        /// 下级平台国标端口号
        /// </summary>
        public ushort RemotePort { get; set; }

        /// <summary>
        /// 鉴权启用
        /// </summary>
        public bool Authentication
        {
            get { return m_authentication; }
            set { m_authentication = value; }
        }

        /// <summary>
        /// 用户名
        /// </summary>
        public string SIPUsername
        {
            get { return m_sipUsername; }
            set
            {
                m_sipUsername = value;
                NotifyPropertyChanged("SIPUsername");
            }
        }

        /// <summary>
        /// 密码
        /// </summary>
        public string SIPPassword
        {
            get { return m_sipPassword; }
            set
            {
                m_sipPassword = value;
                NotifyPropertyChanged("SIPPassword");
            }
        }

        /// <summary>
        /// 消息信令协议(TCP/UDP)
        /// </summary>
        public ProtocolType MsgProtocol
        {
            get { return _msgProtocol; }
            set { _msgProtocol = value; }
        }

        /// <summary>
        /// 媒体流协议(TCP/UDP)
        /// </summary>
        public ProtocolType StreamProtocol
        {
            get { return _streamProtocol; }
            set { _streamProtocol = value; }
        }

        /// <summary>
        /// tcp模式(active/passive)
        /// </summary>
        public TcpConnectMode TcpMode
        {
            get { return _tcpMode; }
            set { _tcpMode = value; }
        }

        /// <summary>
        /// 消息信令字符编码
        /// </summary>
        public string MsgEncode { get; set; }

        /// <summary>
        /// RTP包乱序处理
        /// </summary>
        public bool PacketOutOrder
        {
            get { return m_packetOutOrder; }
            set { m_packetOutOrder = value; }
        }

        /// <summary>
        /// 平台之间心跳周期(单位秒)
        /// </summary>
        public ushort KeepaliveInterval
        {
            get { return m_keepaliveInterval; }
            set { m_keepaliveInterval = value; }
        }

        /// <summary>
        /// 心跳过期次数
        /// </summary>
        public byte KeepaliveNumber
        {
            get { return m_keepaliveNumber; }
            set { m_keepaliveNumber = value; }
        }

        /// <summary>
        /// 对外输出IP地址
        /// </summary>
        public IPAddress OutputIP { get => m_OutputIP; set => m_OutputIP = value; }

        #endregion


        // // [Column(Name = "adminmemberid", DbType = "varchar(32)", CanBeNull = true, UpdateCheck = UpdateCheck.Never)]
        public string AdminMemberId { get; set; }    // If set it designates this asset as a belonging to a user with the matching adminid.

        private string m_sipDomain;
        // // [Column(Name = "sipdomain", DbType = "varchar(128)", CanBeNull = false, UpdateCheck = UpdateCheck.Never)]
        [DataMember]
        public string SIPDomain
        {
            get { return m_sipDomain; }
            set
            {
                m_sipDomain = value;
                NotifyPropertyChanged("SIPDomain");
            }
        }

        private bool m_sendNATKeepAlives;
        //  // [Column(Name = "sendnatkeepalives", DbType = "bit", CanBeNull = false, UpdateCheck = UpdateCheck.Never)]
        [DataMember]
        public bool SendNATKeepAlives
        {
            get { return m_sendNATKeepAlives; }
            set
            {
                m_sendNATKeepAlives = value;
                NotifyPropertyChanged("SendNATKeepAlives");
            }
        }

        private bool m_isIncomingOnly;          // For SIP accounts that can only be used to receive incoming calls.
                                                //  // [Column(Name = "isincomingonly", DbType = "bit", CanBeNull = false, UpdateCheck = UpdateCheck.Never)]
        [DataMember]
        public bool IsIncomingOnly
        {
            get { return m_isIncomingOnly; }
            set
            {
                m_isIncomingOnly = value;
                NotifyPropertyChanged("IsIncomingOnly");
            }
        }

        private string m_outDialPlanName;       // The dialplan that will be used for outgoing calls.
                                                // // [Column(Name = "outdialplanname", DbType = "varchar(64)", CanBeNull = true, UpdateCheck = UpdateCheck.Never)]
        [DataMember]
        public string OutDialPlanName
        {
            get { return m_outDialPlanName; }
            set
            {
                m_outDialPlanName = value;
                NotifyPropertyChanged("OutDialPlanName");
            }
        }

        private string m_inDialPlanName;        // The dialplan that will be used for incoming calls. If this field is empty incoming calls will be forwarded to the account's current bindings.
                                                // // [Column(Name = "indialplanname", DbType = "varchar(64)", CanBeNull = true, UpdateCheck = UpdateCheck.Never)]
        [DataMember]
        public string InDialPlanName
        {
            get { return m_inDialPlanName; }
            set
            {
                m_inDialPlanName = value;
                NotifyPropertyChanged("InDialPlanName");
            }
        }

        private bool m_isUserDisabled;              // Allows owning user disabling of accounts.
                                                    // // [Column(Name = "isuserdisabled", DbType = "bit", CanBeNull = false, UpdateCheck = UpdateCheck.Never)]
        [DataMember]
        public bool IsUserDisabled
        {
            get { return m_isUserDisabled; }
            set
            {
                m_isUserDisabled = value;
                NotifyPropertyChanged("IsUserDisabled");
                NotifyPropertyChanged("IsDisabled");
            }
        }

        private bool m_isAdminDisabled;              // Allows administrative disabling of accounts.
                                                     // // [Column(Name = "isadmindisabled", DbType = "bit", CanBeNull = false, UpdateCheck = UpdateCheck.Never)]
        [DataMember]
        public bool IsAdminDisabled
        {
            get { return m_isAdminDisabled; }
            set
            {
                m_isAdminDisabled = value;
                NotifyPropertyChanged("IsAdminDisabled");
                NotifyPropertyChanged("IsDisabled");
            }
        }

        private string m_adminDisabledReason;
        // // [Column(Name = "admindisabledreason", DbType = "varchar(256)", CanBeNull = true, UpdateCheck = UpdateCheck.Never)]
        [DataMember]
        public string AdminDisabledReason
        {
            get { return m_adminDisabledReason; }
            set
            {
                m_adminDisabledReason = value;
                NotifyPropertyChanged("AdminDisabledReason");
            }
        }

        private string m_networkId;                 // SIP accounts with the ame network id will not have their Contact headers or SDP mangled for private IP address.
                                                    // // [Column(Name = "networkid", DbType = "varchar(16)", CanBeNull = true, UpdateCheck = UpdateCheck.Never)]
        [DataMember]
        public string NetworkId
        {
            get { return m_networkId; }
            set
            {
                m_networkId = value;
                NotifyPropertyChanged("NetworkId");
            }
        }

        private string m_ipAddressACL;              // A regular expression that acts as an IP address Access Control List for SIP request authorisation.
                                                    // // [Column(Name = "ipaddressacl", DbType = "varchar(256)", CanBeNull = true, UpdateCheck = UpdateCheck.Never)]
        [DataMember]
        public string IPAddressACL
        {
            get { return m_ipAddressACL; }
            set
            {
                m_ipAddressACL = value;
                NotifyPropertyChanged("IPAddressACL");
            }
        }

        private DateTimeOffset m_inserted;
        //  // [Column(Name = "inserted", DbType = "datetimeoffset", CanBeNull = false, UpdateCheck = UpdateCheck.Never)]
        [DataMember]
        public DateTimeOffset Inserted
        {
            get { return m_inserted; }
            set { m_inserted = value.ToUniversalTime(); }
        }

        private bool m_isSwitchboardEnabled = true;
        //// [Column(Name = "isswitchboardenabled", DbType = "bit", CanBeNull = false, UpdateCheck = UpdateCheck.Never)]
        [DataMember]
        public bool IsSwitchboardEnabled
        {
            get { return m_isSwitchboardEnabled; }
            set
            {
                m_isSwitchboardEnabled = value;
                NotifyPropertyChanged("IsSwitchboardEnabled");
            }
        }

        private bool m_dontMangleEnabled = false;
        // // [Column(Name = "dontmangleenabled", DbType = "bit", CanBeNull = false, UpdateCheck = UpdateCheck.Never)]
        [DataMember]
        public bool DontMangleEnabled
        {
            get { return m_dontMangleEnabled; }
            set
            {
                m_dontMangleEnabled = value;
                NotifyPropertyChanged("DontMangleEnabled");
            }
        }

        private string m_avatarURL;
        //  // [Column(Name = "avatarurl", DbType = "varchar(1024)", CanBeNull = true, UpdateCheck = UpdateCheck.Never)]
        [DataMember]
        public string AvatarURL
        {
            get { return m_avatarURL; }
            set
            {
                m_avatarURL = value;
                NotifyPropertyChanged("AvatarURL");
            }
        }

        private string m_accountCode;
        // // [Column(Name = "accountcode", DbType = "varchar(36)", CanBeNull = true, UpdateCheck = UpdateCheck.Never)]
        [DataMember]
        public string AccountCode
        {
            get { return m_accountCode; }
            set
            {
                m_accountCode = value;
                NotifyPropertyChanged("AccountCode");
            }
        }

        private string m_description;
        // // [Column(Name = "description", DbType = "varchar(1024)", CanBeNull = true, UpdateCheck = UpdateCheck.Never)]
        [DataMember]
        public string Description
        {
            get { return m_description; }
            set
            {
                m_description = value;
                NotifyPropertyChanged("Description");
            }
        }

        public DateTimeOffset InsertedLocal
        {
            get { return Inserted.AddMinutes(TimeZoneOffsetMinutes); }
        }

        public bool IsDisabled
        {
            get { return m_isUserDisabled || m_isAdminDisabled; }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public SIPAccount() { }


        public SIPAccount(DataRow sipAccountRow)
        {
            Load(sipAccountRow);
        }

        public DataTable GetTable()
        {
            DataTable table = new DataTable();
            table.Columns.Add(new DataColumn("id", typeof(String)));
            table.Columns.Add(new DataColumn("sipusername", typeof(String)));
            table.Columns.Add(new DataColumn("sippassword", typeof(String)));
            table.Columns.Add(new DataColumn("sipdomain", typeof(String)));
            table.Columns.Add(new DataColumn("owner", typeof(String)));
            table.Columns.Add(new DataColumn("adminmemberid", typeof(String)));
            table.Columns.Add(new DataColumn("sendnatkeepalives", typeof(Boolean)));
            table.Columns.Add(new DataColumn("isincomingonly", typeof(Boolean)));
            table.Columns.Add(new DataColumn("isuserdisabled", typeof(Boolean)));
            table.Columns.Add(new DataColumn("isadmindisabled", typeof(Boolean)));
            table.Columns.Add(new DataColumn("admindisabledreason", typeof(String)));
            table.Columns.Add(new DataColumn("outdialplanname", typeof(String)));
            table.Columns.Add(new DataColumn("indialplanname", typeof(String)));
            table.Columns.Add(new DataColumn("inserted", typeof(DateTimeOffset)));
            table.Columns.Add(new DataColumn("networkid", typeof(String)));
            table.Columns.Add(new DataColumn("ipaddressacl", typeof(String)));
            table.Columns.Add(new DataColumn("isswitchboardenabled", typeof(Boolean)));
            table.Columns.Add(new DataColumn("dontmangleenabled", typeof(Boolean)));
            table.Columns.Add(new DataColumn("avatarurl", typeof(String)));
            table.Columns.Add(new DataColumn("description", typeof(String)));
            table.Columns.Add(new DataColumn("accountcode", typeof(String)));
            return table;
        }

        public void Load(DataRow row)
        {
            try
            {
                Id = Guid.NewGuid();

                GbVersion = row["GbVersion"].ToString();
                LocalID = row["LocalID"].ToString();
                IPAddress.TryParse(row["LocalIP"].ToString(), out m_localIP);
                ushort.TryParse(row["LocalPort"].ToString(), out m_localPort);
                bool.TryParse(row["Authentication"].ToString(), out m_authentication);
                m_sipUsername = row["SIPUsername"].ToString();
                m_sipPassword = row["SIPPassword"].ToString();
                Enum.TryParse(GeFirstUpperString(row["MsgProtocol"].ToString()), out _msgProtocol);
                Enum.TryParse(GeFirstUpperString(row["StreamProtocol"].ToString()), out _streamProtocol);
                Enum.TryParse(GeFirstUpperString(row["TcpMode"].ToString()), out _tcpMode);
                bool.TryParse(row["PacketOutOrder"].ToString(), out m_packetOutOrder);
                ushort.TryParse(row["KeepaliveInterval"].ToString(), out m_keepaliveInterval);
                byte.TryParse(row["KeepaliveNumber"].ToString(), out m_keepaliveNumber);
                //IPAddress.TryParse(row["OutputIP"].ToString(), out m_OutputIP);
                MsgEncode = row["MsgEncode"].ToString();
            }
            catch (Exception excp)
            {
                logger.Error("Exception SIPAccount Load. " + excp);
                throw excp;
            }
        }

        public Dictionary<Guid, object> Load(XmlDocument dom)
        {
            return LoadAssetsFromXMLRecordSet(dom);
        }

        public Dictionary<Guid, object> LoadAssetsFromXMLRecordSet(XmlDocument dom)
        {
            try
            {
                var assets = new Dictionary<Guid, object>();
                var sipAssetSet = new DataSet();
                var xmlReader = new XmlTextReader(dom.OuterXml, XmlNodeType.Document, null);
                sipAssetSet.ReadXml(xmlReader);

                if (sipAssetSet.Tables != null && sipAssetSet.Tables.Count > 0)
                {
                    try
                    {
                        foreach (DataRow row in sipAssetSet.Tables[0].Rows)
                        {
                            var sipAsset = new SIPAccount();
                            logger.Debug("Load assets from xml with local ip&port is " + row["LocalIp"] + ":" + row["LocalPort"]);
                            row["LocalId"] = EnvironmentVariables.GbServiceLocalId ?? row["LocalId"];
                            row["LocalIp"] = EnvironmentVariables.GbServiceLocalIp ?? row["LocalIp"];
                            //row["LocalPort"] = EnvironmentVariables.GbServiceLocalPort;
                            sipAsset.Load(row);
                            assets.Add(sipAsset.Id, sipAsset);
                        }
                    }
                    catch (Exception excp)
                    {
                        logger.Error("Exception loading SIP asset record in LoadAssetsFromXMLRecordSet (" + (new SIPAccount()).GetType().ToString() + "). " + excp.Message);
                    }

                    logger.Debug(assets.Count + " " + (new SIPAccount()).GetType().ToString() + " assets loaded from XML record set.");
                }
                else
                {
                    //logger.Warn("The XML supplied to LoadAssetsFromXMLRecordSet for asset type " + (new T()).GetType().ToString() + " did not contain any assets.");
                    logger.Debug("No" + (new SIPAccount()).GetType().ToString() + " assets loaded from XML record set.");
                }

                xmlReader.Close();

                return assets;
            }
            catch (Exception excp)
            {
                logger.Error("Exception LoadAssetsFromXMLRecordSet. " + excp.Message);
                throw;
            }
        }


        public SIPAccount(string owner, string sipDomain, string sipUsername, string sipPassword, string outDialPlanName)
        {
            try
            {
                Id = Guid.NewGuid();
                m_owner = owner;
                m_sipDomain = sipDomain;
                m_sipUsername = sipUsername;
                m_sipPassword = sipPassword;
                m_outDialPlanName = (outDialPlanName != null && outDialPlanName.Trim().Length > 0) ? outDialPlanName.Trim() : null;
                m_inserted = DateTimeOffset.UtcNow;
            }
            catch (Exception excp)
            {
                logger.Error("Exception SIPAccount (ctor). " + excp);
                throw excp;
            }
        }

        public static string ValidateAndClean(SIPAccount sipAccount)
        {

            if (sipAccount.Owner.IsNullOrBlank())
            {
                return "The owner must be specified when creating a new SIP account.";
            }
            if (sipAccount.SIPUsername.IsNullOrBlank())
            {
                return "The username must be specified when creating a new SIP account.";
            }
            if (sipAccount.SIPDomain.IsNullOrBlank())
            {
                return "The domain must be specified when creating a new SIP account.";
            }
            else if (sipAccount.SIPUsername.Length < USERNAME_MIN_LENGTH)
            {
                return "The username must be at least " + USERNAME_MIN_LENGTH + " characters long.";
            }
            else if (Regex.Match(sipAccount.SIPUsername, BANNED_SIPACCOUNT_NAMES).Success)
            {
                return "The username you have requested is not permitted.";
            }
            else if (Regex.Match(sipAccount.SIPUsername, "[^" + USERNAME_ALLOWED_CHARS + "]").Success)
            {
                return "The username had an invalid character, characters permitted are alpha-numeric and .-_.";
            }
            else if (sipAccount.SIPUsername.Contains(".") &&
                (sipAccount.SIPUsername.Substring(sipAccount.SIPUsername.LastIndexOf(".") + 1).Trim().Length >= USERNAME_MIN_LENGTH &&
                sipAccount.SIPUsername.Substring(sipAccount.SIPUsername.LastIndexOf(".") + 1).Trim() != sipAccount.Owner))
            {
                return "You are not permitted to create this username. Only user " + sipAccount.SIPUsername.Substring(sipAccount.SIPUsername.LastIndexOf(".") + 1).Trim() + " can create SIP accounts ending in " + sipAccount.SIPUsername.Substring(sipAccount.SIPUsername.LastIndexOf(".")).Trim() + ".";
            }
            else if (!sipAccount.IsIncomingOnly || !sipAccount.SIPPassword.IsNullOrBlank())
            {
                if (sipAccount.SIPPassword.IsNullOrBlank())
                {
                    return "A password must be specified.";
                }
                else if (sipAccount.SIPPassword.Length < PASSWORD_MIN_LENGTH || sipAccount.SIPPassword.Length > PASSWORD_MAX_LENGTH)
                {
                    return "The password field must be at least " + PASSWORD_MIN_LENGTH + " characters and no more than " + PASSWORD_MAX_LENGTH + " characters.";
                }
                else
                {
                    #region Check the password illegal characters.

                    char[] passwordChars = sipAccount.SIPPassword.ToCharArray();

                    bool illegalCharFound = false;
                    char illegalChar = ' ';

                    foreach (char passwordChar in passwordChars)
                    {
                        if (Regex.Match(passwordChar.ToString(), "[a-zA-Z0-9]").Success)
                        {
                            continue;
                        }
                        else
                        {
                            bool validChar = false;
                            foreach (char allowedChar in NONAPLPHANUM_ALLOWED_PASSWORD_CHARS)
                            {
                                if (allowedChar == passwordChar)
                                {
                                    validChar = true;
                                    break;
                                }
                            }

                            if (validChar)
                            {
                                continue;
                            }
                            else
                            {
                                illegalCharFound = true;
                                illegalChar = passwordChar;
                                break;
                            }
                        }
                    }

                    #endregion

                    if (illegalCharFound)
                    {
                        return "Your password has an invalid character " + illegalChar + " it can only contain a to Z, 0 to 9 and characters in this set " + SafeXML.MakeSafeXML(new String(NONAPLPHANUM_ALLOWED_PASSWORD_CHARS)) + ".";
                    }
                }
            }

            sipAccount.Owner = sipAccount.Owner.Trim();
            sipAccount.SIPUsername = sipAccount.SIPUsername.Trim();
            sipAccount.SIPPassword = (sipAccount.SIPPassword.IsNullOrBlank()) ? null : sipAccount.SIPPassword.Trim();
            sipAccount.SIPDomain = sipAccount.SIPDomain.Trim();

            return null;
        }

        public string ToXML()
        {
            string sipAccountXML =
                " <" + XML_ELEMENT_NAME + ">" + m_newLine +
               ToXMLNoParent() + m_newLine +
                " </" + XML_ELEMENT_NAME + ">" + m_newLine;

            return sipAccountXML;
        }

        public string ToXMLNoParent()
        {
            string sipAccountXML =
                "  <id>" + Id + "</id>" + m_newLine +
                "  <owner>" + m_owner + "</owner>" + m_newLine +
                "  <sipusername>" + m_sipUsername + "</sipusername>" + m_newLine +
                "  <sippassword>" + m_sipPassword + "</sippassword>" + m_newLine +
                "  <sipdomain>" + m_sipDomain + "</sipdomain>" + m_newLine +
                "  <sendnatkeepalives>" + m_sendNATKeepAlives + "</sendnatkeepalives>" + m_newLine +
                "  <isincomingonly>" + m_isIncomingOnly + "</isincomingonly>" + m_newLine +
                "  <outdialplanname>" + m_outDialPlanName + "</outdialplanname>" + m_newLine +
                "  <indialplanname>" + m_inDialPlanName + "</indialplanname>" + m_newLine +
                "  <isuserdisabled>" + m_isUserDisabled + "</isuserdisabled>" + m_newLine +
                "  <isadmindisabled>" + m_isAdminDisabled + "</isadmindisabled>" + m_newLine +
                "  <disabledreason>" + m_adminDisabledReason + "</disabledreason>" + m_newLine +
                "  <networkid>" + m_networkId + "</networkid>" + m_newLine +
                "  <ipaddressacl>" + SafeXML.MakeSafeXML(m_ipAddressACL) + "</ipaddressacl>" + m_newLine +
                "  <inserted>" + m_inserted.ToString("o") + "</inserted>" + m_newLine +
                "  <isswitchboardenabled>" + m_isSwitchboardEnabled + "</isswitchboardenabled>" + m_newLine +
                "  <dontmangleenabled>" + m_dontMangleEnabled + "</dontmangleenabled>" + m_newLine +
                "  <avatarurl>" + m_avatarURL + "</avatarurl>";

            return sipAccountXML;
        }

        public string GetXMLElementName()
        {
            return XML_ELEMENT_NAME;
        }

        public string GetXMLDocumentElementName()
        {
            return XML_DOCUMENT_ELEMENT_NAME;
        }

        private void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private string GeFirstUpperString(string input)
        {
            if (!input.IsNullOrBlank() && input.Length > 1)
            {
                return char.ToUpper(input[0]) + input.Substring(1).ToLower();
            }
            else if (input.Length > 0)
            {
                return input.ToUpper();
            }
            return "";
        }

    }
}