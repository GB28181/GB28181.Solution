using Common.Generic;
using System;
using System.Net.Sockets;

namespace Common.Networks
{
	public class AnsycSocketDespatcher : IDisposable
	{
		public class DespatcheStateModel
		{
			public byte[] Buffer;

			public int Offset;

			public int Size;

			public bool IsCompleted;
		}

		private bool _isBeging = false;

		private object _lockObj = new object();

		private IAsyncResult _lasyAsyncResult = null;

		private Socket _socket = null;

		private bool _CompletedHandle_Invokeing = false;

		private AQueue<DespatcheStateModel> _queueCompletedHandleInvoke = null;

		private Action _DisoconnectHandle;

		private Action<DespatcheStateModel> _CompletedHandle;

		private bool _isDisposed = false;

		public bool IsReceiveMode
		{
			get;
			private set;
		}

		public AnsycSocketDespatcher(Socket socket, bool isReceiveMode, Action disoconnectHandle, Action<DespatcheStateModel> completedHandle)
		{
			_socket = socket;
			IsReceiveMode = isReceiveMode;
			_DisoconnectHandle = disoconnectHandle;
			_CompletedHandle = completedHandle;
		}

		public void Begin(byte[] buffer)
		{
			Begin(buffer, 0, buffer.Length);
		}

		public void Begin(byte[] buffer, int offset, int size)
		{
			DespatcheStateModel despatcheStateModel = new DespatcheStateModel();
			despatcheStateModel.Size = size;
			despatcheStateModel.Buffer = buffer;
			despatcheStateModel.Offset = offset;
			despatcheStateModel.IsCompleted = false;
			DespatcheStateModel so = despatcheStateModel;
			Begin(so);
		}

		public void Begin(DespatcheStateModel so)
		{
			if (!_isDisposed && !_isBeging)
			{
				_isBeging = true;
				try
				{
					lock (_lockObj)
					{
						if (IsReceiveMode)
						{
							_lasyAsyncResult = _socket.BeginReceive(so.Buffer, so.Offset, so.Size, SocketFlags.None, ReadCallback, so);
						}
						else
						{
							_lasyAsyncResult = _socket.BeginSend(so.Buffer, so.Offset, so.Size, SocketFlags.None, ReadCallback, so);
						}
					}
				}
				catch (Exception e)
				{
					OnDisoconnect(e);
				}
			}
		}

		public void End()
		{
			if (!_isDisposed && _isBeging)
			{
				_isBeging = false;
				try
				{
					if (IsReceiveMode)
					{
						if (_lasyAsyncResult != null && !_lasyAsyncResult.IsCompleted)
						{
							_socket.EndReceive(_lasyAsyncResult);
						}
					}
					else if (_lasyAsyncResult != null && !_lasyAsyncResult.IsCompleted)
					{
						_socket.EndSend(_lasyAsyncResult);
					}
				}
				catch (Exception e)
				{
					if (!_isDisposed)
					{
						OnDisoconnect(e);
					}
				}
				_lasyAsyncResult = null;
			}
		}

		private void Completed(DespatcheStateModel model)
		{
			if (!_isDisposed)
			{
				_isBeging = false;
				_lasyAsyncResult = null;
				lock (_lockObj)
				{
					if (_queueCompletedHandleInvoke == null)
					{
						_queueCompletedHandleInvoke = new AQueue<DespatcheStateModel>();
					}
					_queueCompletedHandleInvoke.Enqueue(model);
					if (!_CompletedHandle_Invokeing)
					{
						model = _queueCompletedHandleInvoke.Dequeue();
						_CompletedHandle_Invokeing = true;
						_CompletedHandle.BeginInvoke(model, _CompletedHandle_AsyncCallback, null);
					}
				}
			}
		}

		private void _CompletedHandle_AsyncCallback(IAsyncResult ar)
		{
			if (!_isDisposed)
			{
				lock (_lockObj)
				{
					_CompletedHandle.EndInvoke(ar);
					if (_queueCompletedHandleInvoke.Count > 0)
					{
						DespatcheStateModel obj = _queueCompletedHandleInvoke.Dequeue();
						_CompletedHandle.BeginInvoke(obj, _CompletedHandle_AsyncCallback, null);
					}
					else
					{
						_CompletedHandle_Invokeing = false;
					}
				}
			}
		}

		private void ReadCallback(IAsyncResult ar)
		{
			if (!_isDisposed)
			{
				DespatcheStateModel despatcheStateModel = null;
				despatcheStateModel = (DespatcheStateModel)ar.AsyncState;
				try
				{
					int num = _socket.EndReceive(ar);
					despatcheStateModel.Offset += num;
					despatcheStateModel.Size -= num;
					if (num == 0)
					{
						_ = _socket.Connected;
						//bool flag = 1 == 0;
						if (_DisoconnectHandle != null)
						{
							_DisoconnectHandle.BeginInvoke(null, null);
						}
					}
					else if (despatcheStateModel.Size == 0)
					{
						despatcheStateModel.IsCompleted = true;
						Completed(despatcheStateModel);
					}
					else if (IsReceiveMode)
					{
						_lasyAsyncResult = _socket.BeginReceive(despatcheStateModel.Buffer, despatcheStateModel.Offset, despatcheStateModel.Size, SocketFlags.None, ReadCallback, despatcheStateModel);
					}
					else
					{
						_lasyAsyncResult = _socket.BeginSend(despatcheStateModel.Buffer, despatcheStateModel.Offset, despatcheStateModel.Size, SocketFlags.None, ReadCallback, despatcheStateModel);
					}
				}
				catch (Exception e)
				{
					OnDisoconnect(e);
				}
			}
		}

		protected void OnDisoconnect(Exception e)
		{
			if (!_isDisposed && _DisoconnectHandle != null)
			{
				_DisoconnectHandle.BeginInvoke(null, null);
			}
		}

		protected void OnDisoconnect()
		{
			if (!_isDisposed && _DisoconnectHandle != null)
			{
				_DisoconnectHandle.BeginInvoke(null, null);
			}
		}

		public void Dispose()
		{
			_isDisposed = true;
			End();
			_DisoconnectHandle = null;
			_CompletedHandle = null;
		}
	}
}
