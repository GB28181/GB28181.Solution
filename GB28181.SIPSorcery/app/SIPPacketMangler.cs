﻿// ============================================================================
// FileName: SIPPacketMangler.cs
//
// Description:
// A class containing functionality to mangle the Contact header and SDP payloads
// of SIP messages.
//
// History:
// 14 Sep 2008	    Aaron Clauson	    Created (most methods extractd from StatefulProxyCore).
// 30 May 2020	Edward Chen     Updated.
// License: 
// BSD 3-Clause "New" or "Revised" License, see included LICENSE.md file.
//

using System;
using System.Net;
using System.Text.RegularExpressions;
using GB28181.Logger4Net;
using GB28181.Net;
using GB28181.Sys;
using SIPSorcery.SIP;
using SIPSorcery.Sys;

namespace GB28181.App
{
    public class SIPPacketMangler
    {
        private static ILog logger = AppState.logger;

        public static string MangleSDP(string sdpBody, string publicIPAddress, out bool wasMangled)
        {
            wasMangled = false;

            try
            {
                if (sdpBody != null && publicIPAddress != null)
                {
                    string sdpAddress = SDP.GetSDPRTPEndPoint(sdpBody).Address.ToString();

                    // Only mangle if there is something to change. For example the server could be on the same private subnet in which case it can't help.
                    if (IPSocket.IsPrivateAddress(sdpAddress) && publicIPAddress != sdpAddress)
                    {
                        //logger.Debug("MangleSDP replacing private " + sdpAddress + " with " + publicIPAddress + ".");
                        string mangledSDP = Regex.Replace(sdpBody, @"c=IN IP4 (?<ipaddress>(\d+\.){3}\d+)", "c=IN IP4 " + publicIPAddress, RegexOptions.Singleline);
                        wasMangled = true;

                        return mangledSDP;
                    }
                    // else {
                    //    logger.Debug("MangleSDP did not replace " + sdpAddress + " with " + publicIPAddress + ".");
                    //}
                }
                else
                {
                    logger.Warn("Mangle SDP was called with an empty body or public IP address.");
                }

                return sdpBody;
            }
            catch (Exception excp)
            {
                logger.Error("Exception MangleSDP. " + excp.Message);
                return sdpBody;
            }
        }

        /// <summary>
        /// Mangles private IP addresses in a SIP request replacing them with the IP address the packet was received on. 
        /// </summary>
        /// <param name="sipResponse">The unmangled SIP request.</param>
        /// <returns>The mangled SIP request</returns>
        public static void MangleSIPRequest(SIPMonitorServerTypesEnum server, SIPRequest sipRequest, string username, SIPMonitorLogDelegate logDelegate)
        {
            try
            {
                string bottomViaIPAddress = sipRequest.Header.Vias.BottomViaHeader.ReceivedFromIPAddress;

                if (sipRequest.Header.Contact != null && sipRequest.Header.Contact.Count == 1 && bottomViaIPAddress != null)
                {
                    string contactHost = sipRequest.Header.Contact[0].ContactURI.Host;

                    // Only mangle if the host is a private IP address and there is something to change. 
                    // For example the server could be on the same private subnet in which case it can't help.
                    if (IPSocket.IsPrivateAddress(contactHost) && contactHost != bottomViaIPAddress)
                    {
                        string origContact = sipRequest.Header.Contact[0].ContactURI.Host;
                        sipRequest.Header.Contact[0].ContactURI.Host = sipRequest.Header.Vias.BottomViaHeader.ReceivedFromAddress;

                        //logger.Debug("Contact URI identified as containing private address for " + sipRequest.Method + " " + origContact + " adjusting to use bottom via " + bottomViaHost + ".");
                        //FireProxyLogEvent(new SIPMonitorConsoleEvent(SIPMonitorServerTypesEnum.Registrar, SIPMonitorServerTypesEnum.ContactRegisterInProgress, "Contact on " + sipRequest.Method + " " + origContact + " had private address adjusted to " + bottomViaHost + ".", username));
                    }
                }

                if (sipRequest.Body != null && bottomViaIPAddress != null)
                {
                    string mangledSDP = MangleSDP(sipRequest.Body, bottomViaIPAddress, out bool wasMangled);

                    if (wasMangled)
                    {
                        sipRequest.Body = mangledSDP;
                        sipRequest.Header.ContentLength = sipRequest.Body.Length;

                        if (logDelegate != null)
                        {
                            logDelegate(new SIPMonitorConsoleEvent(server, SIPMonitorEventTypesEnum.DialPlan, "SDP mangled for INVITE request from " + sipRequest.RemoteSIPEndPoint.ToString() + ", adjusted address " + bottomViaIPAddress + ".", username));
                        }
                    }
                }
            }
            catch (Exception excp)
            {
                logger.Error("Exception MangleSIPRequest. " + excp.Message);
            }
        }

        /// <summary>
        /// Mangles private IP addresses in a SIP response replacing them with the IP address the packet was received on. 
        /// </summary>
        /// <param name="sipResponse">The unmangled SIP response.</param>
        /// <returns>The mangled SIP response</returns>
        public static void MangleSIPResponse(SIPMonitorServerTypesEnum server, SIPResponse sipResponse, SIPEndPoint remoteEndPoint, string username, SIPMonitorLogDelegate logDelegate)
        {
            try
            {
                if (sipResponse.Header.Contact != null && sipResponse.Header.Contact.Count > 0)
                {
                    string contactHost = sipResponse.Header.Contact[0].ContactURI.Host;

                    // Only mangle if the host is a private IP address and there is something to change. 
                    // For example the server could be on the same private subnet in which case it can't help.
                    if (IPSocket.IsPrivateAddress(contactHost) && contactHost != remoteEndPoint.Address.ToString())
                    {
                        SIPURI origContact = sipResponse.Header.Contact[0].ContactURI;
                        sipResponse.Header.Contact[0].ContactURI = new SIPURI(origContact.Scheme, remoteEndPoint);

                        //logger.Debug("INVITE response Contact URI identified as containing private address, original " + origContact + " adjustied to " + remoteEndPoint.ToString() + ".");
                        //FireProxyLogEvent(new SIPMonitorConsoleEvent(SIPMonitorServerTypesEnum.Registrar, SIPMonitorServerTypesEnum.ContactRegisterInProgress, "INVITE Response contact adjusted from " + origContact + " to " + remoteEndPoint.ToString() + ".", username));
                    }
                }

                if (sipResponse.Body != null)
                {
                    string mangledSDP = MangleSDP(sipResponse.Body, remoteEndPoint.Address.ToString(), out bool wasMangled);

                    if (wasMangled)
                    {
                        sipResponse.Body = mangledSDP;
                        sipResponse.Header.ContentLength = sipResponse.Body.Length;

                        if (logDelegate != null)
                        {
                            logDelegate(new SIPMonitorConsoleEvent(server, SIPMonitorEventTypesEnum.DialPlan, "SDP mangled for INVITE response from " + sipResponse.RemoteSIPEndPoint.ToString() + ", adjusted address " + remoteEndPoint.Address.ToString() + ".", username));
                        }
                    }
                }
            }
            catch (Exception excp)
            {
                logger.Error("Exception MangleSIPResponse. " + excp.Message);
            }
        }

        public static IPAddress GetRequestIPAddress(SIPRequest sipRequest)
        {
            IPAddress requestIPAddress = null;
            string remoteUAStr = sipRequest.Header.ProxyReceivedFrom;
            if (!remoteUAStr.IsNullOrBlank())
            {
                requestIPAddress = SIPEndPoint.ParseSIPEndPoint(remoteUAStr).Address;
            }
            else if (sipRequest.RemoteSIPEndPoint != null)
            {
                requestIPAddress = sipRequest.RemoteSIPEndPoint.Address;
            }
            return requestIPAddress;
        }
    }
}
