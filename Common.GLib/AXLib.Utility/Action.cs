using System;

namespace AXLib.Utility
{
	public class Action<T> : IAction<T>
	{
		private System.Action<T> _action = null;

		public Action(System.Action<T> action)
		{
			_action = action;
		}

		public void invoke(T t)
		{
			if (_action != null)
			{
				_action(t);
			}
		}
	}
}
