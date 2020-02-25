using System;

namespace GLib.AXLib.Utility
{
	public class Loop
	{
		private int _interval = 0;

		private Action _callback = null;

		private Action<Exception> _error = null;

		private bool _isRuning = false;

		public Guid Key
		{
			get;
			private set;
		}

		public int Interval => _interval;

		public Loop(int interval, Action callback)
			: this(interval, callback, null)
		{
		}

		public Loop(int interval, Action callback, Action<Exception> error)
		{
			_interval = interval;
			_callback = callback;
			_error = error;
		}

		public void Start()
		{
			if (!_isRuning)
			{
				_isRuning = true;
				Key = ThreadEx.NewTimer(_interval, _callback, _error);
			}
		}

		public void Stop()
		{
			if (_isRuning)
			{
				_isRuning = false;
				ThreadEx.FreeTimer(Key);
			}
		}
	}
}
