using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
//using System.Windows.Forms;

public static class Watchdog
{
	private static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
	{
		OnFatalException(e.Exception);
	}

	private static int CheckActive(string appName)
	{
		int num = 0;
		Process[] processes = Process.GetProcesses();
		foreach (Process process in processes)
		{
			if (process.ProcessName == appName)
			{
				num++;
			}
		}
		return num;
	}

	private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
	{
		OnFatalException(e.ExceptionObject as Exception);
	}

	[DllImport("user32.dll", CharSet = CharSet.Unicode)]
	public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

	[DllImport("User32.dll", CharSet = CharSet.Unicode)]
	public static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string strClassName, string strWindowName);

	[DllImport("user32.dll")]
	private static extern int GetWindowThreadProcessId(IntPtr hWnd, out int ProcessId);

	private static void HideConsoleWindow()
	{
		//Console.Title = Application.ProductName;
		//IntPtr intPtr = FindWindow("ConsoleWindowClass", Application.ProductName);
		//if (intPtr != IntPtr.Zero)
		//{
		//	ShowWindow(intPtr, 0u);
		//}
	}

	private static void KillDApp(string appName)
	{
		List<string> list = Enumerable.ToList(Enumerable.Select(new string[6]
		{
			"WerFault.exe",
			"dwwin.exe",
			"dw20.exe",
			"WerFault",
			"dwwin",
			"dw20"
		}, (string p) => p.ToUpper()));
		Process[] processes = Process.GetProcesses();
		Process[] array = processes;
		foreach (Process process in array)
		{
			try
			{
				if (list.Contains(process.ProcessName.ToUpper().Replace(" *32", "")))
				{
					Log($"异常进程:{process.ProcessName}");
					process.Kill();
				}
			}
			catch (Exception)
			{
			}
		}
		try
		{
			IntPtr intPtr = FindWindowEx(IntPtr.Zero, IntPtr.Zero, null, $"{appName}.exe - 应用程序错误");
			if (intPtr != IntPtr.Zero)
			{
				int ProcessId = 0;
				GetWindowThreadProcessId(intPtr, out ProcessId);
				if (ProcessId != 0)
				{
					Process.GetProcessById(ProcessId).Kill();
					Log($"异常窗口 {appName}.exe - 应用程序错误");
				}
			}
			intPtr = FindWindowEx(IntPtr.Zero, IntPtr.Zero, null, "Microsoft Visual C++ Runtime Library");
			if (intPtr != IntPtr.Zero)
			{
				int ProcessId2 = 0;
				GetWindowThreadProcessId(intPtr, out ProcessId2);
				if (ProcessId2 != 0)
				{
					Process.GetProcessById(ProcessId2).Kill();
					Log($"异常窗口 {appName}.exe - Microsoft Visual C++ Runtime Library");
				}
			}
		}
		catch (Exception)
		{
		}
	}

	private static void KillOtherDog()
	{
		Process currentProcess = Process.GetCurrentProcess();
		Process[] processes = Process.GetProcesses();
		foreach (Process process in processes)
		{
			if (process.ProcessName.ToUpper() == currentProcess.ProcessName.ToUpper())
			{
				try
				{
					if (currentProcess.Id != process.Id)
					{
						process.Kill();
					}
				}
				catch (Exception)
				{
				}
			}
		}
	}

	private static void Log(string msg)
	{
		//msg = $"{DateTime.Now} {msg}";
		//Console.WriteLine(msg);
		//string text = Application.StartupPath + "\\Watchdog";
		//if (!Directory.Exists(text))
		//{
		//	Directory.CreateDirectory(text);
		//}
		//File.AppendAllText(string.Format("{0}\\{1}.txt", text, DateTime.Now.ToString("yyyy_MM")), msg + "\r\n");
	}

	private static void Main(string[] args)
	{
		try
		{
			RegisterUnHandldException();
			HideConsoleWindow();
			string text = args[0];
			string text2 = args[1];
			string text3 = args[2];
			int num = int.Parse(args[3]);
			string text4 = args[4];
			string text5 = args[5];
			string text6 = (args.Length > 6) ? args[6] : null;
			string text7 = (args.Length > 7) ? args[7] : null;
			Console.WriteLine(string.Format("{0} {1} {2} {3} {4} {5} {6}", text, text2, text3, num, text4, text5, text6, text7));
			if (!(text == "0"))
			{
				RegistryHelper.ConfigAutoStart(text4 == "1", args);
				KillOtherDog();
				DateTime now = DateTime.Now;
				while (true)
				{
					bool flag = true;
					KillDApp(text2);
					if (CheckActive(text2) > 0)
					{
						now = DateTime.Now;
					}
					if (now.AddSeconds(num) < DateTime.Now)
					{
						StartApp(text2, text3);
						now = DateTime.Now;
					}
					Thread.Sleep(1000);
				}
			}
			RegistryHelper.ConfigAutoStart(isAutoStart: false, args);
			KillOtherDog();
		}
		catch (Exception ex)
		{
			Console.WriteLine(ex.ToString());
			Console.Read();
		}
	}

	private static void OnFatalException(Exception ex)
	{
		StringBuilder stringBuilder = new StringBuilder();
		string value = string.Format("{0} {1}", "Watchdog", "");
		stringBuilder.AppendLine(value);
		stringBuilder.AppendLine(ex.Message);
		stringBuilder.AppendLine(ex.GetType().Name);
		stringBuilder.AppendLine(ex.StackTrace);
		Log(stringBuilder.ToString());
	}

	private static void RegisterUnHandldException()
	{
		//Application.ThreadException += Application_ThreadException;
		AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
	}

	[DllImport("user32.dll")]
	public static extern bool ShowWindow(IntPtr hWnd, uint nCmdShow);

	private static void StartApp(string appName, string exe)
	{
		try
		{
			int num = CheckActive(appName);
			Log($"尝试启动 {appName} 路径：{exe}");
			Directory.SetCurrentDirectory(new FileInfo(exe).Directory.ToString());
			Process.Start(exe);
			if (CheckActive(appName) > num)
			{
				Log($"启动成功 {appName} 路径：{exe}");
			}
			else
			{
				Log($"启动失败 {appName} 路径：{exe}");
			}
			Thread.Sleep(3000);
		}
		catch (Exception arg)
		{
			Log($"启动异常 {appName} 路径：{exe} \r\n异常信息 {arg}");
		}
	}
}
