// ============================================================================
// FileName: SIPMonitorConsoleEvent.cs
//
// Description:
// Describes the types of events that can be sent by the different SIP Servers to SIP
// Monitor clients.
//
// Author(s):
// Aaron Clauson
//
// History:
// 15 Nov 2008	Aaron Clauson	Created.
//
// License: 
// BSD 3-Clause "New" or "Revised" License, see included LICENSE.md file.
// 30 May 2020	Edward Chen     Updated.
// License: 
// BSD 3-Clause "New" or "Revised" License, see included LICENSE.md file.
//

using System;
using System.Diagnostics;
using System.Globalization;
using SIPSorcery.SIP;
using SIPSorcery.Sys;

#if UNITTEST
using NUnit.Framework;
#endif

namespace GB28181.App
{
    /// <summary>
    /// Describes the types of events that can be sent by the different SIP Servers to SIP
    /// Monitor clients.
    /// </summary>
    public class SIPMonitorConsoleEvent : SIPMonitorEvent
    {
        public const string SERIALISATION_PREFIX = "1";     // Prefix appended to the front of a serialised event to identify the type.
        private const string CALLDIRECTION_IN_STRING = "<-";
        private const string CALLDIRECTION_OUT_STRING = "->";

        private const string m_topLevelAdminID = "*";

        public SIPMonitorServerTypesEnum ServerType { get; set; }
        public SIPEndPoint DestinationEndPoint { get; set; }
        public SIPEndPoint ServerEndPoint { get; set; }             // Socket the request was received on by the server.
        public SIPMonitorEventTypesEnum EventType { get; set; }

        private SIPMonitorConsoleEvent()
        {
            m_serialisationPrefix = SERIALISATION_PREFIX;
            ClientType = SIPMonitorClientTypesEnum.Console;
#if !SILVERLIGHT
            ProcessID = Process.GetCurrentProcess().Id;
#endif
        }

        public SIPMonitorConsoleEvent(SIPMonitorServerTypesEnum serverType, SIPMonitorEventTypesEnum eventType, string message, string username)
        {
            m_serialisationPrefix = SERIALISATION_PREFIX;
            ClientType = SIPMonitorClientTypesEnum.Console;

            ServerType = serverType;
            EventType = eventType;
            Message = message;
            Username = username;
            Created = DateTimeOffset.UtcNow;
#if !SILVERLIGHT
            ProcessID = Process.GetCurrentProcess().Id;
#endif
        }

        public SIPMonitorConsoleEvent(SIPMonitorServerTypesEnum serverType, SIPMonitorEventTypesEnum eventType, string message, string username, SIPEndPoint localEndPoint, SIPEndPoint remoteEndPoint)
        {
            m_serialisationPrefix = SERIALISATION_PREFIX;
            ClientType = SIPMonitorClientTypesEnum.Console;

            ServerType = serverType;
            EventType = eventType;
            Message = message;
            Username = username;
            Created = DateTimeOffset.UtcNow;
            ServerEndPoint = localEndPoint;
            RemoteEndPoint = remoteEndPoint;
#if !SILVERLIGHT
            ProcessID = Process.GetCurrentProcess().Id;
#endif
        }

        public SIPMonitorConsoleEvent(SIPMonitorServerTypesEnum serverType, SIPMonitorEventTypesEnum eventType, string message, SIPEndPoint serverSocket, SIPEndPoint fromSocket, SIPEndPoint toSocket)
        {
            m_serialisationPrefix = SERIALISATION_PREFIX;
            ClientType = SIPMonitorClientTypesEnum.Console;

            ServerType = serverType;
            EventType = eventType;
            Message = message;
            ServerEndPoint = serverSocket;
            RemoteEndPoint = fromSocket;
            DestinationEndPoint = toSocket;
            Created = DateTimeOffset.UtcNow;
#if !SILVERLIGHT
            ProcessID = Process.GetCurrentProcess().Id;
#endif
        }

        public SIPMonitorConsoleEvent(SIPMonitorServerTypesEnum serverType, SIPMonitorEventTypesEnum eventType, string message, SIPRequest sipRequest, SIPResponse sipResponse, SIPEndPoint localEndPoint, SIPEndPoint remoteEndPoint, SIPCallDirection callDirection)
        {
            m_serialisationPrefix = SERIALISATION_PREFIX;
            ClientType = SIPMonitorClientTypesEnum.Console;

            ServerType = serverType;
            EventType = eventType;
            Message = message;
            RemoteEndPoint = remoteEndPoint;
            ServerEndPoint = localEndPoint;
            Created = DateTimeOffset.UtcNow;
#if !SILVERLIGHT
            ProcessID = Process.GetCurrentProcess().Id;
#endif

            string dirn = (callDirection == SIPCallDirection.In) ? CALLDIRECTION_IN_STRING : CALLDIRECTION_OUT_STRING;
            if (sipRequest != null)
            {
                Message = $"REQUEST ({Created:HH:mm:ss:fff}): {localEndPoint}{dirn}{remoteEndPoint}\r\n{sipRequest}";
            }
            else if (sipResponse != null)
            {
                Message = $"RESPONSE ({Created:HH:mm:ss:fff}): {localEndPoint}{dirn}{remoteEndPoint}\r\n{sipResponse}";
            }
        }

        public static SIPMonitorConsoleEvent ParseClientControlEventCSV(string eventCSV)
        {
            try
            {
                var monitorEvent = new SIPMonitorConsoleEvent();

                if (eventCSV.IndexOf(END_MESSAGE_DELIMITER) != -1)
                {
                    eventCSV.Remove(eventCSV.Length - 2, 2);
                }

                string[] eventFields = eventCSV.Split(new char[] { '|' });

                monitorEvent.SessionID = eventFields[1];
                monitorEvent.MonitorServerID = eventFields[2];
                monitorEvent.ServerType = SIPMonitorServerTypes.GetProxyServerType(eventFields[3]);
                monitorEvent.EventType = SIPMonitorEventTypes.GetProxyEventType(eventFields[4]);
                monitorEvent.Created = DateTimeOffset.ParseExact(eventFields[5], SERIALISATION_DATETIME_FORMAT, CultureInfo.InvariantCulture);

                string serverEndPointStr = eventFields[6];
                if (serverEndPointStr != null && serverEndPointStr.Trim().Length > 0)
                {
                    monitorEvent.ServerEndPoint = SIPEndPoint.ParseSIPEndPoint(serverEndPointStr);
                }

                string remoteEndPointStr = eventFields[7];
                if (remoteEndPointStr != null && remoteEndPointStr.Trim().Length > 0)
                {
                    monitorEvent.RemoteEndPoint = SIPEndPoint.ParseSIPEndPoint(remoteEndPointStr);
                }

                string dstEndPointStr = eventFields[8];
                if (dstEndPointStr != null && dstEndPointStr.Trim().Length > 0)
                {
                    monitorEvent.DestinationEndPoint = SIPEndPoint.ParseSIPEndPoint(dstEndPointStr);
                }

                monitorEvent.Username = eventFields[9];

                _ = int.TryParse(eventFields[10], out int _prcessID);

                monitorEvent.ProcessID = _prcessID;

                monitorEvent.Message = eventFields[11].Trim('#');

                return monitorEvent;
            }
            catch (Exception excp)
            {
                logger.Error("Exception SIPMonitorConsoleEvent ParseEventCSV. " + excp.Message);
                return null;
            }
        }

        public override string ToCSV()
        {
            try
            {
                string serverEndPointValue = (ServerEndPoint != null) ? ServerEndPoint.ToString() : null;
                string remoteEndPointValue = (RemoteEndPoint != null) ? RemoteEndPoint.ToString() : null;
                string dstEndPointValue = (DestinationEndPoint != null) ? DestinationEndPoint.ToString() : null;

                string csvEvent =
                    SERIALISATION_PREFIX + "|" +
                    SessionID + "|" +
                    MonitorServerID + "|" +
                    ServerType + "|" +
                    EventType + "|" +
                    Created.ToString(SERIALISATION_DATETIME_FORMAT) + "|" +
                    serverEndPointValue + "|" +
                    remoteEndPointValue + "|" +
                    dstEndPointValue + "|" +
                    Username + "|" +
                    ProcessID + "|" +
                    Message + END_MESSAGE_DELIMITER;

                return csvEvent;
            }
            catch (Exception excp)
            {
                logger.Error("Exception SIPMonitorConsoleEvent ToCSV. " + excp.Message);
                return null;
            }
        }

        public string ToConsoleString(string adminId)
        {
            string consoleString = EventType.ToString() + " " + Created.ToString("HH:mm:ss:fff");

            if (!MonitorServerID.IsNullOrBlank())
            {
                consoleString += " " + MonitorServerID + "(" + ProcessID + ")";
            }

            // Special case for dialplan events and super user. Add the username of the event to the start of the monitor message.
            if (adminId == m_topLevelAdminID && !Username.IsNullOrBlank())
            {
                consoleString += " " + Username;
            }

            if (EventType == SIPMonitorEventTypesEnum.FullSIPTrace)
            {
                consoleString += ":\r\n" + Message + "\r\n";
            }
            else
            {
                consoleString += ": " + Message + "\r\n";
            }

            return consoleString;
        }

        #region Unit testing.

#if UNITTEST
	
		[TestFixture]
		public class SIPMonitorConsoleEventUnitTest
		{
			[TestFixtureSetUp]
			public void Init()
			{
				
			}

		
			[TestFixtureTearDown]
			public void Dispose()
			{			
				
			}
		}

#endif

        #endregion
    }
}
