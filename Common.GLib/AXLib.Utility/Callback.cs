using System;

namespace AXLib.Utility
{
	public class Callback : ICallback
	{
		private Action _action = null;

		public Callback(Action action)
		{
			_action = action;
		}

		public void invoke()
		{
			if (_action != null)
			{
				_action();
			}
		}
	}
}
