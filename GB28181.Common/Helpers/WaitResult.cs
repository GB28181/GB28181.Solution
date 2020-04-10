using System;
using System.Threading;

namespace Helpers
{
	public class WaitResult<T>
	{
		private bool _isFinish = false;

		private Thread _waitThread = null;

		private object _lockObject = new object();

		private Semaphore _sp = new Semaphore(0, 1);

		public T Result;

		public void Wait()
		{
			_waitThread = Thread.CurrentThread;
			try
			{
				lock (_lockObject)
				{
					_sp.WaitOne();
				}
			}
			catch (Exception ex)
			{
				throw ex;
			}
		}

		public void Wait(long timeout)
		{
			_waitThread = Thread.CurrentThread;
			try
			{
				lock (_lockObject)
				{
					_sp.WaitOne((int)timeout);
				}
			}
			catch (Exception ex)
			{
				throw ex;
			}
		}

		public void Finish(T result)
		{
			Result = result;
			_isFinish = true;
			if (_waitThread != null)
			{
				lock (_lockObject)
				{
					_sp.Release();
				}
			}
		}

		public bool GetIsFinish()
		{
			return _isFinish;
		}
	}
}
