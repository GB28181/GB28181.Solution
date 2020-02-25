using System;
using System.Collections.Specialized;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace GLib.Utilities
{
	public class IniFile
	{
		public string FileName;

		[DllImport("kernel32")]
		private static extern bool WritePrivateProfileString(string section, string key, string val, string filePath);

		[DllImport("kernel32")]
		private static extern int GetPrivateProfileString(string section, string key, string def, byte[] retVal, int size, string filePath);

		public IniFile(string IniFileName, bool ForceCreate)
		{
			FileInfo fileInfo = new FileInfo(IniFileName);
			if (!fileInfo.Exists)
			{
				if (!ForceCreate)
				{
					throw new ApplicationException("Ini文件不存在");
				}
				fileInfo.Directory.Create();
				fileInfo.Create();
			}
			FileName = fileInfo.FullName;
		}

		public void WriteString(string Section, string Ident, string Value)
		{
			if (!WritePrivateProfileString(Section, Ident, Value, FileName))
			{
				throw new ApplicationException("写Ini文件出错");
			}
		}

		public string ReadString(string Section, string Ident, string Default)
		{
			byte[] array = new byte[65535];
			int privateProfileString = GetPrivateProfileString(Section, Ident, Default, array, array.GetUpperBound(0), FileName);
			string @string = Encoding.GetEncoding(0).GetString(array);
			@string = @string.Substring(0, privateProfileString);
			return @string.Trim();
		}

		public int ReadInteger(string Section, string Ident, int Default)
		{
			string value = ReadString(Section, Ident, Default.ToString());
			try
			{
				return Convert.ToInt32(value);
			}
			catch (Exception)
			{
				return Default;
			}
		}

		public void WriteInteger(string Section, string Ident, int Value)
		{
			WriteString(Section, Ident, Value.ToString());
		}

		public bool ReadBool(string Section, string Ident, bool Default)
		{
			try
			{
				return Convert.ToBoolean(ReadString(Section, Ident, Default.ToString()));
			}
			catch (Exception)
			{
				return Default;
			}
		}

		public void WriteBool(string Section, string Ident, bool Value)
		{
			WriteString(Section, Ident, Value.ToString());
		}

		public void WriteDateTime(string Section, string Ident, DateTime Value)
		{
			WriteString(Section, Ident, Value.ToString());
		}

		public DateTime ReadDateTime(string Section, string Ident, DateTime Default)
		{
			try
			{
				return Convert.ToDateTime(ReadString(Section, Ident, Default.ToString()));
			}
			catch (Exception)
			{
				return Default;
			}
		}

		public void WriteDouble(string Section, string Ident, double Value)
		{
			WriteString(Section, Ident, Value.ToString());
		}

		public double ReadFloat(string Section, string Ident, double Default)
		{
			try
			{
				return Convert.ToDouble(ReadString(Section, Ident, Default.ToString()));
			}
			catch (Exception)
			{
				return Default;
			}
		}

		public StringCollection ReadSection(string Section)
		{
			byte[] array = new byte[16384];
			StringCollection stringCollection = new StringCollection();
			int privateProfileString = GetPrivateProfileString(Section, null, null, array, array.GetUpperBound(0), FileName);
			GetStringsFromBuffer(array, privateProfileString, stringCollection);
			return stringCollection;
		}

		private void GetStringsFromBuffer(byte[] Buffer, int bufLen, StringCollection Strings)
		{
			Strings.Clear();
			if (bufLen == 0)
			{
				return;
			}
			int num = 0;
			for (int i = 0; i < bufLen; i++)
			{
				if (Buffer[i] == 0 && i - num > 0)
				{
					string @string = Encoding.GetEncoding(0).GetString(Buffer, num, i - num);
					Strings.Add(@string);
					num = i + 1;
				}
			}
		}

		public StringCollection ReadSections()
		{
			StringCollection stringCollection = new StringCollection();
			byte[] array = new byte[65535];
			int num = 0;
			num = GetPrivateProfileString(null, null, null, array, array.GetUpperBound(0), FileName);
			GetStringsFromBuffer(array, num, stringCollection);
			return stringCollection;
		}

		public NameValueCollection ReadSectionValues(string Section)
		{
			NameValueCollection nameValueCollection = new NameValueCollection();
			StringCollection stringCollection = ReadSection(Section);
			nameValueCollection.Clear();
			StringEnumerator enumerator = stringCollection.GetEnumerator();
			try
			{
				while (enumerator.MoveNext())
				{
					string current = enumerator.Current;
					nameValueCollection.Add(current, ReadString(Section, current, ""));
				}
			}
			finally
			{
				(enumerator as IDisposable)?.Dispose();
			}
			return nameValueCollection;
		}

		public void EraseSection(string Section)
		{
			if (!WritePrivateProfileString(Section, null, null, FileName))
			{
				throw new ApplicationException("无法清除Ini文件中的Section");
			}
		}

		public void DeleteKey(string Section, string Ident)
		{
			WritePrivateProfileString(Section, Ident, null, FileName);
		}

		public void UpdateFile()
		{
			WritePrivateProfileString(null, null, null, FileName);
		}

		public bool SectionExists(string Section)
		{
			StringCollection stringCollection = ReadSections();
			return stringCollection.IndexOf(Section) > -1;
		}

		public bool ValueExists(string Section, string Ident)
		{
			StringCollection stringCollection = ReadSection(Section);
			return stringCollection.IndexOf(Ident) > -1;
		}

		~IniFile()
		{
			UpdateFile();
		}
	}
}
