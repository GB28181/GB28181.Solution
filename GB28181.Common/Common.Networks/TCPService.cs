using Common.Generic;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Common.Networks
{
	public class TCPService
	{
		private int _maxConnect = 256;

		private int _port = -1;

		private Socket _sock = null;

		private bool _working = false;

		private Thread _thread = null;

		public int Port => _port;

		public event EventHandler<EventArgsEx<Socket>> Accepted;

		public TCPService(int port, int macConnect = 256)
		{
			_port = port;
			_maxConnect = macConnect;
		}

		public void Start()
		{
			lock (this)
			{
				if (!_working)
				{
					_working = true;
					_thread = new Thread(AcceptThread);
					_thread.Start();
				}
			}
		}

		public void Stop()
		{
			lock (this)
			{
				if (_working)
				{
					_working = false;
					if (_thread != null)
					{
						_thread.Abort();
						_thread.Join(1000);
					}
					_thread = null;
					try
					{
						if (_sock != null)
						{
							_sock.Close();
							_sock.Dispose();
						}
					}
					catch (Exception)
					{
					}
				}
			}
		}

		private void AcceptThread()
		{
			_sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			_sock.Bind(new IPEndPoint(IPAddress.Any, _port));
			_sock.Listen(_maxConnect);
			while (true)
			{
				Socket state = _sock.Accept();
				ThreadPool.QueueUserWorkItem(OnAccept, state);
			}
		}

		private void OnAccept(object obj)
		{
			Socket socket = (Socket)obj;
			try
			{
				if (this.Accepted != null)
				{
					this.Accepted(this, new EventArgsEx<Socket>(socket));
				}
			}
			catch (Exception value)
			{
				try
				{
					if (socket != null)
					{
						socket.Close();
						socket.Dispose();
					}
				}
				catch
				{
				}
				Console.WriteLine(value);
			}
		}
	}
}
