using System;
using System.Threading;

namespace Helpers
{
	public static class MyTask
	{
		private static ApartmentState ApartmentState = ApartmentState.MTA;

		public static TResult CallTask<TArg, TResult>(this Func<TArg, TResult> fun, TArg arg)
		{
			TResult result = default(TResult);
			Thread thread = new Thread(((Action)delegate
			{
				result = fun(arg);
			}).Invoke);
			thread.IsBackground = true;
			thread.Priority = ThreadPriority.Lowest;
			thread.SetApartmentState(ApartmentState);
			thread.Start();
			thread.Join();
			return result;
		}

		public static void CallTask<TArg>(this Action<TArg> act, TArg arg)
		{
			Thread thread = new Thread(((Action)delegate
			{
				act(arg);
			}).Invoke);
			thread.IsBackground = true;
			thread.Priority = ThreadPriority.Lowest;
			thread.SetApartmentState(ApartmentState);
			thread.Start();
			thread.Join();
		}

		public static void CallTask(this Action act)
		{
			Thread thread = new Thread(act.Invoke);
			thread.IsBackground = true;
			thread.Priority = ThreadPriority.Lowest;
			thread.SetApartmentState(ApartmentState);
			thread.Start();
			thread.Join();
		}
	}
}
