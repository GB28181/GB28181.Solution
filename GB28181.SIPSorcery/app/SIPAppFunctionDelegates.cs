// ============================================================================
// FileName: SIPFunctionDelegates.cs
//
// Description:
// A list of function delegates that are used by the SIP Server Agents.
//
// Author(s):
// Aaron Clauson
//
// History:
// 14 Nov 2008	Aaron Clauson	Created.
// 30 May 2020	Edward Chen     Updated.
// License: 
// BSD 3-Clause "New" or "Revised" License, see included LICENSE.md file.
//

using System.Net;
using SIPSorcery.SIP;

namespace GB28181.App
{
    public delegate void SIPMonitorLogDelegate(SIPMonitorEvent monitorEvent);
    public delegate void SIPMonitorMachineLogDelegate(SIPMonitorMachineEvent machineEvent);
    public delegate bool SIPMonitorAuthenticationDelegate(string username, string password);    // Delegate to authenticate connections to the SIP Monitor Server.
    public delegate void DialogueBridgeCreatedDelegate(SIPDialogue clientDialogue, SIPDialogue forwardedDialogue, string owner);
    public delegate void DialogueBridgeClosedDelegate(string dialogueId, string owner);
    public delegate void IPAddressChangedDelegate(IPAddress newIPAddress);
    public delegate void QueueNewCallDelegate(ISIPServerUserAgent uas);
    public delegate void BlindTransferDelegate(SIPDialogue deadDialogue, SIPDialogue orphanedDialogue, SIPDialogue answeredDialogue);

    // SIP User Agent Delegates.
    public delegate void SIPCallResponseDelegate(ISIPClientUserAgent uac, SIPResponse sipResponse);
    public delegate void SIPCallFailedDelegate(ISIPClientUserAgent uac, string errorMessage);
    public delegate void SIPUASStateChangedDelegate(ISIPServerUserAgent uas, SIPResponseStatusCodesEnum statusCode, string reasonPhrase);

    // Authorisation delegates.
    public delegate SIPRequestAuthenticationResult SIPAuthenticateRequestDelegate(SIPEndPoint localSIPEndPoint, SIPEndPoint remoteEndPoint, SIPRequest sipRequest, SIPAccount sipAccount, SIPMonitorLogDelegate log);
}
