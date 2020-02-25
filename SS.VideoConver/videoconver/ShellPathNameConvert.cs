using System.Runtime.InteropServices;
using System.Text;

namespace videoconver
{
	public class ShellPathNameConvert
	{
		[DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
		private static extern uint GetLongPathName(string shortname, StringBuilder longnamebuff, uint buffersize);

		[DllImport("kernel32.dll", CharSet = CharSet.Auto)]
		private static extern int GetShortPathName([MarshalAs(UnmanagedType.LPWStr)] string path, [MarshalAs(UnmanagedType.LPWStr)] StringBuilder shortPath, int shortPathLength);

		public static string ToLongPathName(string shortName)
		{
			StringBuilder stringBuilder = new StringBuilder(256);
			uint capacity = (uint)stringBuilder.Capacity;
			GetLongPathName(shortName, stringBuilder, capacity);
			return stringBuilder.ToString();
		}

		public static string ToShortPathName(string longName)
		{
			StringBuilder stringBuilder = new StringBuilder(256);
			int capacity = stringBuilder.Capacity;
			if (GetShortPathName(longName, stringBuilder, capacity) == 0)
			{
				return longName;
			}
			return stringBuilder.ToString();
		}
	}
}
