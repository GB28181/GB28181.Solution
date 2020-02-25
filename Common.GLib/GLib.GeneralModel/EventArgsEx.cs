using System;

namespace GLib.GeneralModel
{
	public class EventArgsEx<T> : EventArgs
	{
		public T Arg
		{
			get;
			private set;
		}

		public EventArgsEx(T arg)
		{
			Arg = arg;
		}
	}
}
