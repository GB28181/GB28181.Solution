//-----------------------------------------------------------------------------
// Filename: IceCandidate.cs
//
// Description: Represents a candidate used in the Interactive Connectivity Establishment (ICE) 
// negotiation to set up a usable network connection between two peers as 
// per RFC5245 https://tools.ietf.org/html/rfc5245.
//
// History:
// 26 Feb 2016	Aaron Clauson	Created.
//
// License: 
// BSD 3-Clause "New" or "Revised" License, see included LICENSE.md file.
//


using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using GB28181.Sys;
using SIPSorcery.Net;
using SIPSorcery.Sys;

namespace GB28181.Net
{
    public enum IceCandidateTypesEnum
    {
        Unknown = 0,
        host = 1,
        srflx = 2,
        relay = 3
    }

    public class IceCandidate
    {
        public const string m_CRLF = "\r\n";
        public const string REMOTE_ADDRESS_KEY = "raddr";
        public const string REMOTE_PORT_KEY = "rport";

        public Socket LocalRtpSocket;
        public Socket LocalControlSocket;
        public IPAddress LocalAddress;
        public Task RtpListenerTask;
        public TurnServer TurnServer;
        public bool IsGatheringComplete;
        public int TurnAllocateAttempts;
        public IPEndPoint StunRflxIPEndPoint;
        public IPEndPoint TurnRelayIPEndPoint;
        public IPEndPoint RemoteRtpEndPoint;
        public bool IsDisconnected;
        public string DisconnectionMessage;
        public DateTime LastSTUNSendAt;
        public DateTime LastStunRequestReceivedAt;
        public DateTime LastStunResponseReceivedAt;
        public bool IsStunLocalExchangeComplete;      // This is the authenticated STUN request sent by us to the remote WebRTC peer.
        public bool IsStunRemoteExchangeComplete;     // This is the authenticated STUN request sent by the remote WebRTC peer to us.
        public int StunConnectionRequestAttempts = 0;
        public DateTime LastCommunicationAt;

        public string Transport;
        public string NetworkAddress;
        public int Port;
        public IceCandidateTypesEnum CandidateType;
        public string RemoteAddress;
        public int RemotePort;
        public string RawString;

        public bool IsConnected
        {
            get { return IsStunLocalExchangeComplete == true && IsStunRemoteExchangeComplete && !IsDisconnected; }
        }

        public static IceCandidate Parse(string candidateLine)
        {
            var candidateFields = candidateLine.Trim().Split(' ');
            IceCandidate candidate = new IceCandidate
            {
                RawString = candidateLine,
                Transport = candidateFields[2],
                NetworkAddress = candidateFields[4],
                Port = Convert.ToInt32(candidateFields[5])
            };
            Enum.TryParse(candidateFields[7], out candidate.CandidateType);

            if (candidateFields.Length > 8 && candidateFields[8] == REMOTE_ADDRESS_KEY)
            {
                candidate.RemoteAddress = candidateFields[9];
            }

            if (candidateFields.Length > 10 && candidateFields[10] == REMOTE_PORT_KEY)
            {
                candidate.RemotePort = Convert.ToInt32(candidateFields[11]);
            }

            return candidate;
        }

#if !SILVERLIGHT
        public override string ToString()
        {
            //  var candidateStr = string.Format("a=candidate:{0} {1} udp {2} {3} {4} typ host generation 0\r\n", Crypto.GetRandomInt(10).ToString(), "1", Crypto.GetRandomInt(10).ToString(), LocalAddress.ToString(), (LocalRtpSocket.LocalEndPoint as IPEndPoint).Port);

            var candidateStr = $"a=candidate:{Crypto.GetRandomInt(10)} 1 udp {Crypto.GetRandomInt(10)} {LocalAddress} {(LocalRtpSocket.LocalEndPoint as IPEndPoint).Port} typ host generation 0\r\n";

            if (StunRflxIPEndPoint != null)
            {
                candidateStr += string.Format("a=candidate:{0} {1} udp {2} {3} {4} typ srflx raddr {5} rport {6} generation 0\r\n", Crypto.GetRandomInt(10).ToString(), "1", Crypto.GetRandomInt(10).ToString(), StunRflxIPEndPoint.Address, StunRflxIPEndPoint.Port, LocalAddress.ToString(), (LocalRtpSocket.LocalEndPoint as IPEndPoint).Port);
                //logger.Debug(" " + srflxCandidateStr);
                //iceCandidateString += srflxCandidateStr;
            }

            return candidateStr;
        }
#endif
    }
}
