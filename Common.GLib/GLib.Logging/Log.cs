using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace GLib.Logging
{
	public class Log : IDisposable
	{
		private static Queue<LogMessage> logMessages;

		private static string path;

		private static bool state;

		private static LogType logtype;

		private static DateTime time;

		private static StreamWriter writer;

		public Log()
			: this(AppDomain.CurrentDomain.BaseDirectory, LogType.Daily)
		{
		}

		public Log(LogType t)
			: this(AppDomain.CurrentDomain.BaseDirectory, t)
		{
		}

		public Log(string filepath, LogType t)
		{
			if (logMessages == null)
			{
				state = true;
				path = filepath;
				logtype = t;
				FileOpen();
				logMessages = new Queue<LogMessage>();
				Thread thread = new Thread(Work);
				thread.Start();
			}
		}

		private void Work()
		{
			while (true)
			{
				bool flag = true;
				if (logMessages.Count > 0)
				{
					LogMessage logMessage = null;
					lock (logMessages)
					{
						logMessage = logMessages.Dequeue();
					}
					if (logMessage != null)
					{
						WriteLogMessage(logMessage);
					}
				}
				else if (state)
				{
					Thread.Sleep(1);
				}
				else
				{
					FileClose();
				}
			}
		}

		private void WriteLogMessage(LogMessage message)
		{
			try
			{
				if (writer == null)
				{
					FileOpen();
				}
				else
				{
					if (DateTime.Now >= time)
					{
						FileClose();
						FileOpen();
					}
					writer.Write(message.Time);
					writer.Write("\t");
					writer.Write(message.Type);
					writer.Write("\t\r\n");
					writer.Write(message.Content);
					writer.Write("\r\n\r\n");
					writer.Flush();
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
			}
			FileClose();
		}

		private string GetFileName()
		{
			DateTime now = DateTime.Now;
			string format = "";
			switch (logtype)
			{
			case LogType.Daily:
				time = new DateTime(now.Year, now.Month, now.Day);
				time = time.AddDays(1.0);
				format = "yyyyMMdd'.log'";
				break;
			case LogType.Weekly:
				time = new DateTime(now.Year, now.Month, now.Day);
				time = time.AddDays(7.0);
				format = "yyyyMMdd'.log'";
				break;
			case LogType.Monthly:
				time = new DateTime(now.Year, now.Month, 1);
				time = time.AddMonths(1);
				format = "yyyyMM'.log'";
				break;
			case LogType.Annually:
				time = new DateTime(now.Year, 1, 1);
				time = time.AddYears(1);
				format = "yyyy'.log'";
				break;
			}
			return now.ToString(format);
		}

		public void Write(LogMessage message)
		{
			if (logMessages != null)
			{
				lock (logMessages)
				{
					logMessages.Enqueue(message);
				}
			}
		}

		public void Write(string text, MessageType type)
		{
			Write(new LogMessage(text, type));
		}

		public void Write(DateTime now, string text, MessageType type)
		{
			Write(new LogMessage(now, text, type));
		}

		public void Write(Exception e, MessageType type)
		{
			Write(new LogMessage(e.Message, type));
		}

		private void FileOpen()
		{
			writer = new StreamWriter(path + GetFileName(), append: true, Encoding.Default);
		}

		private void FileClose()
		{
			if (writer != null)
			{
				writer.Flush();
				writer.Close();
				writer.Dispose();
				writer = null;
			}
		}

		public void Dispose()
		{
			state = false;
			GC.SuppressFinalize(this);
		}
	}
}
