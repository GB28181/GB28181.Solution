using System;
using System.IO;

namespace GLib.Utilities.IO
{
	public class DirectoryHelper
	{
		public static string CreateMultiFolder(params string[] names)
		{
			string text = null;
			if (names.Length > 0)
			{
				text = names[0];
				if (!Directory.Exists(text))
				{
					Directory.CreateDirectory(text);
				}
			}
			if (text != null)
			{
				for (int i = 1; i < names.Length; i++)
				{
					text = text + "\\" + names[i];
					if (!Directory.Exists(text))
					{
						Directory.CreateDirectory(text);
					}
				}
				return text + "\\";
			}
			return null;
		}

		public static string CreateMultiFolder(string path)
		{
			string[] names = path.Replace("\\\\", "\\").Split(new string[1]
			{
				"\\"
			}, StringSplitOptions.RemoveEmptyEntries);
			return CreateMultiFolder(names);
		}
	}
}
