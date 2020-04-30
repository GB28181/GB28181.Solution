using System;

namespace Helpers
{
	public class RuntimeExceptionEx : Exception
	{
		//private bool _setMsged = false;

		public new string Message = null;

		public bool CanCancel = false;

		public string StackTraceString = "";

		public RuntimeExceptionEx(string msg)
			: base(msg)
		{
			SetStackTraceString();
		}

		public RuntimeExceptionEx(Exception e)
			: base(e.Message, e)
		{
			SetStackTraceString();
		}

		public RuntimeExceptionEx(string msg, Exception e)
			: base(msg, e)
		{
		}

		private void SetStackTraceString()
		{
			StackTraceString = ToString();
		}

		public static string GetStackTraceString(Exception e)
		{
			return GetStackTraceString(e, "");
		}

		public static string GetStackTraceString(Exception e, string span)
		{
			return e?.ToString();
		}

		public static void PrintException(Exception e)
		{
			Console.WriteLine("PrintException", GetStackTraceString(e));
		}

		public static RuntimeExceptionEx Create(Exception e)
		{
			return new RuntimeExceptionEx(e);
		}

		public static RuntimeExceptionEx Create(string msg)
		{
			return new RuntimeExceptionEx(msg);
		}
	}
}
