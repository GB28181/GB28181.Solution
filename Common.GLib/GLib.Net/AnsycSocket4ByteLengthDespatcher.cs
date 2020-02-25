using System;
using System.Net.Sockets;

namespace GLib.Net
{
	public class AnsycSocket4ByteLengthDespatcher
	{
		private AnsycSocketDespatcher _despatcher = null;

		private bool _isBeging = false;

		private bool _needTSLen = true;

		private bool _needTSData = true;

		private int _needTSDataLen = 0;

		private byte[] _bsLen = new byte[4];

		private byte[] _bsData = null;

		private object _lock = new object();

		private Action _DisoconnectHandle;

		private Action<byte[]> _Completed;

		private bool _isReceiveMode = false;

		private bool _isDisposed = false;

		public AnsycSocket4ByteLengthDespatcher(Socket socket, bool isReceiveMode, Action disoconnectHandle, Action<byte[]> _completed)
		{
			_isReceiveMode = isReceiveMode;
			_DisoconnectHandle = disoconnectHandle;
			_Completed = _completed;
			_despatcher = new AnsycSocketDespatcher(socket, isReceiveMode, _despatcher_Disoconnect, _despatcher_Completed);
		}

		public void BeginSend(byte[] buffer)
		{
			if (!_isDisposed && !_isBeging)
			{
				_needTSLen = true;
				_needTSData = true;
				_bsLen = BitConverter.GetBytes(buffer.Length);
				_bsData = buffer;
				_despatcher.Begin(_bsLen);
			}
		}

		public void EndSend()
		{
			if (_isBeging)
			{
				_isBeging = false;
				_needTSLen = true;
				_needTSData = true;
				_bsData = null;
				_despatcher.End();
			}
		}

		public void BeginReceive()
		{
			if (!_isDisposed && !_isBeging)
			{
				_needTSLen = true;
				_needTSData = true;
				_bsData = null;
				_despatcher.Begin(_bsLen);
			}
		}

		public void EndReceive()
		{
			if (_isBeging)
			{
				_isBeging = false;
				_needTSLen = true;
				_needTSData = true;
				_bsData = null;
				_despatcher.End();
			}
		}

		private void OnCompleted(byte[] bytes)
		{
			if (!_isDisposed)
			{
				_isBeging = false;
				if (_Completed != null)
				{
					_Completed(_bsData);
				}
			}
		}

		private void _despatcher_Disoconnect()
		{
			if (_DisoconnectHandle != null)
			{
				_DisoconnectHandle();
			}
		}

		private void _despatcher_Completed(AnsycSocketDespatcher.DespatcheStateModel model)
		{
			if (_isDisposed)
			{
				return;
			}
			if (!_isReceiveMode)
			{
				if (_needTSLen)
				{
					_needTSLen = false;
				}
				if (_needTSData)
				{
					_needTSData = false;
					_despatcher.Begin(_bsData);
				}
				else
				{
					OnCompleted(model.Buffer);
				}
				return;
			}
			if (_needTSLen)
			{
				_needTSLen = false;
				_needTSDataLen = BitConverter.ToInt32(model.Buffer, 0);
				_bsData = new byte[_needTSDataLen];
			}
			if (_needTSData)
			{
				_needTSData = false;
				_despatcher.Begin(_bsData);
				if (_bsData.Length != _needTSDataLen)
				{
					throw new Exception();
				}
			}
			else
			{
				OnCompleted(model.Buffer);
			}
		}

		private void Log(string msg)
		{
		}

		public void Dispose()
		{
			_isDisposed = false;
			_despatcher.Dispose();
			_DisoconnectHandle = null;
			_Completed = null;
		}
	}
}
