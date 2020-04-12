using System;
using System.Reflection;
using System.Text;
using System.Threading;

namespace GB28181.Logger4Net.DebugEx
{
	public static class _DebugEx
	{
		private static string _appName = "";

		private static Action<UnhandledExceptionEventArgs> _unhandledExceptionCallBack = null;

		private static bool _alertExceptionMsg = false;


		public static string Version = Assembly.GetExecutingAssembly().GetName().Version.ToString();

		public static event Action<string> EvtTrace;


		public static string[] LogFlagSwitch = new string[] { };
		
		public static void Trace(Exception e)
		{
			Trace("Error", e.ToString());
		}

		public static void Trace(string flag, string msg)
		{
			string text = $"{flag}___{msg}";
			if (EvtTrace != null)
			{
				EvtTrace?.Invoke(text);
			}
		}

		public static void RegisterUnHandldException(string appName, Action<UnhandledExceptionEventArgs> unhandledExceptionCallBack, bool alertExceptionMsg = false)
		{
			_appName = appName;
			_alertExceptionMsg = alertExceptionMsg;
			_unhandledExceptionCallBack = unhandledExceptionCallBack;
		//	Application.ThreadException += Application_ThreadException;
			AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
		}

		private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			OnFatalException(e.ExceptionObject as Exception);
			if (e.IsTerminating && _unhandledExceptionCallBack != null)
			{
				_unhandledExceptionCallBack(e);
			}
		}

		private static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
		{
			OnFatalException(e.Exception);
		}

		public static void OnFatalException(Exception ex, bool isEnd = false)
		{
			var stringBuilder = new StringBuilder();
			string text = "";
			string version = Version;
			text = $"{_appName} {version} {DateTime.Now}";
			if (isEnd)
			{
				text = $"Fatal error \r\n{_appName} {1}";
			}
			stringBuilder.AppendLine(text);
			stringBuilder.AppendLine(GetErrorString(ex));

			var stackMsg = $"{text} \r\nDetails：{ex.Message + ex.StackTrace}\r\nTime：{DateTime.Now}";
			stringBuilder.AppendLine(stackMsg);
			WriteLog(stringBuilder.ToString());
			
		}

		public static string GetLog(Exception ex, bool isEnd = false)
		{
			var stringBuilder = new StringBuilder();
			string text = "";
			string arg = Assembly.GetExecutingAssembly().GetName().Version.ToString();
			text = $"{_appName} {arg} {DateTime.Now}";
			if (isEnd)
			{
				text = $"Fatal error: {_appName}";
			}
			stringBuilder.AppendLine(text);
			stringBuilder.AppendLine(GetErrorString(ex));
			return stringBuilder.ToString();
		}

		public static void WriteLog(string msg)
		{

		}

		private static string GetErrorString(Exception ex, string pleft = "")
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine(pleft + ex.Message);
			stringBuilder.AppendLine(pleft + ex.GetType().Name);
			stringBuilder.AppendLine(pleft + ex.StackTrace);
			if (ex.InnerException != null)
			{
				stringBuilder.AppendLine(GetErrorString(ex.InnerException, pleft + "  "));
			}
			return stringBuilder.ToString();
		}
	}
}
