using System;

namespace Common.Generic
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
