using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace GLib
{
	public static class DebugEx
	{
		private static string _appName = "";

		private static Action<UnhandledExceptionEventArgs> _unhandledExceptionCallBack = null;

		private static bool _alertExceptionMsg = false;

		public static string LogPath =  Application.StartupPath + "\\log\\";

		public static string Version = Assembly.GetExecutingAssembly().GetName().Version.ToString();

		public static event Action<string> EvtTrace;

		public static void Trace(string flag, string msg)
		{
			string text = $"{flag}___{msg}";
			Console.WriteLine(text);
			if (EvtTrace != null)
			{
				EvtTrace(text);
			}
		}

		public static void RegisterUnHandldException()
		{
			RegisterUnHandldException(Application.ProductName, null, alertExceptionMsg: true);
		}

		public static void RegisterUnHandldException(string appName, Action<UnhandledExceptionEventArgs> unhandledExceptionCallBack, bool alertExceptionMsg = false)
		{
			_appName = appName;
			_alertExceptionMsg = alertExceptionMsg;
			_unhandledExceptionCallBack = unhandledExceptionCallBack;
			Application.ThreadException += Application_ThreadException;
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
			StringBuilder stringBuilder = new StringBuilder();
			string text = "";
			string version = Version;
			text = $"{_appName} {version} {DateTime.Now}";
			if (isEnd)
			{
				text = string.Format("=================致命错误==================\r\n{0} {1}", _appName, "");
			}
			stringBuilder.AppendLine(text);
			stringBuilder.AppendLine(GetErrorString(ex));
			WriteLog(stringBuilder.ToString());
			string text2 = $"{text}出现一个未处理的异常\r\n请将程序安装目录下的日志反馈给软件提供商。\r\n详细信息：{ex.Message + ex.StackTrace}\r\n发生时间：{DateTime.Now}";
			if (!_alertExceptionMsg)
			{
			}
		}

		public static string GetLog(Exception ex, bool isEnd = false)
		{
			StringBuilder stringBuilder = new StringBuilder();
			string text = "";
			string arg = Assembly.GetExecutingAssembly().GetName().Version.ToString();
			text = $"{_appName} {arg} {DateTime.Now}";
			if (isEnd)
			{
				text = string.Format("=================致命错误==================\r\n{0} {1}", _appName, "");
			}
			stringBuilder.AppendLine(text);
			stringBuilder.AppendLine(GetErrorString(ex));
			return stringBuilder.ToString();
		}

		public static void WriteLog(string msg)
		{
			string text = LogPath.ToSystemPath();
			if (!Directory.Exists(text))
			{
				Directory.CreateDirectory(text);
			}
			string path = string.Format("{0}\\Log_{1}.txt", text, DateTime.Now.ToString("yyyy-MM-dd")).ToSystemPath();
			File.AppendAllText(path, msg + "\r\n", Encoding.UTF8);
		}

		private static string GetErrorString(Exception ex, string pleft = "")
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine(pleft + ex.Message);
			stringBuilder.AppendLine(pleft + ex.GetType().Name);
			stringBuilder.AppendLine(pleft + ex.StackTrace);
			if (ex.InnerException != null)
			{
				stringBuilder.AppendLine("---------------------------------------------->");
				stringBuilder.AppendLine(GetErrorString(ex.InnerException, pleft + "  "));
			}
			return stringBuilder.ToString();
		}

		public static string ToSystemPath(this string path)
		{
			if (Environment.OSVersion.Platform == PlatformID.Unix)
			{
				return path.Replace("\\", "/");
			}
			return path.Replace("/", "\\");
		}
	}

}
