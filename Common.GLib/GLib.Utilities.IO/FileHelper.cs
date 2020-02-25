using System;
using System.IO;
using System.Text;

namespace GLib.Utilities.IO
{
	public static class FileHelper
	{
		public static string CreateFileName()
		{
			Random random = new Random();
			return DateTime.Now.ToString("yyyyMMddHHmmss") + random.Next(1000, 9999);
		}

		public static void WriteFile(string content)
		{
			WriteFile(string.Empty, string.Empty, content);
		}

		public static void WriteFile(string fileName, string content)
		{
			WriteFile(string.Empty, fileName, content);
		}

		public static void WriteFile(string path, string fileName, string content)
		{
			if (path == string.Empty)
			{
				path = AppDomain.CurrentDomain.BaseDirectory;
			}
			if (fileName == string.Empty)
			{
				fileName = CreateFileName() + ".log";
			}
			if (!Directory.Exists(path))
			{
				Directory.CreateDirectory(path);
			}
			path += fileName;
			try
			{
				if (!File.Exists(path))
				{
					FileStream fileStream = File.Create(path);
					fileStream.Close();
				}
				StreamWriter streamWriter = new StreamWriter(path, append: true, Encoding.Default);
				streamWriter.Write(content);
				streamWriter.Write("\r\n");
				streamWriter.Close();
				streamWriter.Dispose();
			}
			catch (Exception)
			{
			}
		}
	}
}
