using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Common.Networks
{
	public static class TimeOutSocket
	{
		private static bool IsConnectionSuccessful = false;

		private static Exception socketexception;

		private static ManualResetEvent TimeoutObject = new ManualResetEvent(initialState: false);

		public static void Connect(Socket sock, string ip, int port, int timeoutMSec)
		{
			Connect(sock, new IPEndPoint(IPAddress.Parse(ip), port), timeoutMSec);
		}

		public static void Connect(Socket sock, IPEndPoint remoteEndPoint, int timeoutMSec)
		{
			sock.BeginConnect(remoteEndPoint, CallBackMethod, sock);
			if (TimeoutObject.WaitOne(timeoutMSec, exitContext: false))
			{
				if (IsConnectionSuccessful)
				{
					return;
				}
				throw socketexception;
			}
			sock.Close();
			throw new TimeoutException("TimeOut Exception");
		}

		private static void CallBackMethod(IAsyncResult asyncresult)
		{
			try
			{
				IsConnectionSuccessful = false;
				Socket socket = asyncresult.AsyncState as Socket;
				if (socket != null)
				{
					socket.EndConnect(asyncresult);
					IsConnectionSuccessful = true;
				}
			}
			catch (Exception ex)
			{
				IsConnectionSuccessful = false;
				socketexception = ex;
			}
			finally
			{
				TimeoutObject.Set();
			}
		}
	}
}
