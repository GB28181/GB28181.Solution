//-----------------------------------------------------------------------------
// Filename: NetTestDescription.cs
//
// Description: Descriptive fields for a network test
//
// NetTestDescription Payload:
// 0                   1                   2                   3
// 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
// |                Client Socket String                           |
// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
// |                Server Socket String                           |
// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
// 
// History:
// 09 Jan 2007	Aaron Clauson	Created.
//
// License: 
// BSD 3-Clause "New" or "Revised" License, see included LICENSE.md file.
//


using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using GB28181.Logger4Net;

namespace GB28181.Net
{
    public class NetTestDescription
    {
        private const char DELIMITER_CHARACTER = '\a';

        public static NetTestDescription Empty = new NetTestDescription(Guid.Empty, null, null, null, null, null, null);

        public Guid TestId;
        public string ClientSocket;
        public string ClientISP;
        public string ServerSocket;
        public string ServerISP;
        public string Username;
        public string Comment;

        public NetTestDescription()
        { }

        public NetTestDescription(Guid testId, string clientSocket, string clientISP, string serverSocket, string serverISP, string username, string comment)
        {
            TestId = testId;
            ClientSocket = clientSocket;
            ClientISP = clientISP;
            ServerSocket = serverSocket;
            ServerISP = serverISP;
            Username = username;
            Comment = comment;
        }

        public NetTestDescription(byte[] bytes)
        {
            string netTestString = Encoding.ASCII.GetString(bytes);

            string[] netTestFields = netTestString.Split(DELIMITER_CHARACTER);

            TestId = new Guid(netTestFields[0]);
            ClientSocket = netTestFields[1];
            ServerSocket = netTestFields[2];
            ClientISP = netTestFields[3];
            ServerISP = netTestFields[4];
            Username = netTestFields[5];
            Comment = netTestFields[6];
        }

        public byte[] GetBytes()
        {
            string netTestString = TestId.ToString() + DELIMITER_CHARACTER + ClientSocket + DELIMITER_CHARACTER + ServerSocket + DELIMITER_CHARACTER + 
                ClientISP + DELIMITER_CHARACTER + ServerISP + DELIMITER_CHARACTER + Username + DELIMITER_CHARACTER + Comment;

            return Encoding.ASCII.GetBytes(netTestString);
        }
    }
}
