using System.IO;

namespace GLib.Utilities.IO
{
	public class FileTypeHelper
	{
		public static FileType GetFileType(string filename)
		{
			filename = filename.ToLower();
			switch (Path.GetExtension(filename))
			{
			case ".txt":
				return FileType.Text;
			case ".xls":
				return FileType.Excel;
			default:
				return FileType.UnKnow;
			}
		}
	}
}
