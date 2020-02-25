using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GLib.Extension
{
	public static class StringEx
	{
		public static string _Format(this string str, string format)
		{
			return string.Format(format, str);
		}

		public static string CutString(this string inputString, int len)
		{
			ASCIIEncoding aSCIIEncoding = new ASCIIEncoding();
			int num = 0;
			string text = "";
			byte[] bytes = aSCIIEncoding.GetBytes(inputString);
			for (int i = 0; i < bytes.Length; i++)
			{
				num = ((bytes[i] != 63) ? (num + 1) : (num + 2));
				try
				{
					text += inputString.Substring(i, 1);
				}
				catch
				{
					break;
				}
				if (num > len)
				{
					break;
				}
			}
			byte[] bytes2 = Encoding.Default.GetBytes(inputString);
			if (bytes2.Length > len)
			{
				text += "...";
			}
			return text;
		}

		public static string CutString(this object inputString, int len)
		{
			return string.Concat(inputString).CutString(len);
		}

		public static string ConstituteString(this IEnumerable items, string splitKey)
		{
			string text = "";
			foreach (object item in items)
			{
				if (item != null)
				{
					text = string.Concat(text, (text == "") ? "" : splitKey, string.Concat(item));
				}
			}
			return text;
		}

		public static string ConstituteString(this IEnumerable items)
		{
			return items.ConstituteString(",");
		}

		public static bool EqIgnoreCase(this string source, string value)
		{
			if (source == value)
			{
				return true;
			}
			return source?.Equals(value, StringComparison.OrdinalIgnoreCase) ?? false;
		}

		public static T[] Split<T>(this string source, string splitKey = ",")
		{
			if (string.IsNullOrEmpty(source))
			{
				return new T[0];
			}
			return Enumerable.ToArray(Enumerable.Select(source.Split(new string[1]
			{
				splitKey
			}, StringSplitOptions.RemoveEmptyEntries), (string p) => p.Convert<T>()));
		}

		public static string UrlFilter(this string url, string filter)
		{
			return new Uri(url).UrlFilter(filter);
		}

		public static string UrlFilter(this Uri url, string filter)
		{
			string text = "";
			string[] array = url.Query.Replace("?", "").Split<string>("&");
			string[] array2 = array;
			foreach (string text2 in array2)
			{
				string[] array3 = text2.Split<string>("=");
				string source = "";
				string text3 = "";
				if (array3.Length > 0)
				{
					source = array3[0];
				}
				if (array3.Length > 1)
				{
					text3 = array3[1];
				}
				if (!source.EqIgnoreCase(filter))
				{
					text = text + ((text == "") ? "" : "&") + text2;
				}
			}
			if (text == "")
			{
				text = "1=1";
			}
			string result = url.AbsoluteUri + "?" + text;
			if (url.Query != "")
			{
				result = url.AbsoluteUri.Replace(url.Query, "") + "?" + text;
			}
			return result;
		}

		public static string To16Str(this byte b)
		{
			return Convert.ToString(b, 16).PadLeft(2, '0');
		}

		public static byte To16Byte(this string s)
		{
			return Convert.ToByte(s, 16);
		}

		public static string To16Strs(this byte[] bs)
		{
			return bs.To16Strs(0, bs.Length);
		}

		public static string To16Strs(this byte[] bs, int offset, int len)
		{
			StringBuilder stringBuilder = new StringBuilder();
			foreach (byte item in Enumerable.Take(Enumerable.Skip(bs, offset), len))
			{
				stringBuilder.Append(item.To16Str() + " ");
			}
			return stringBuilder.ToString();
		}

		public static byte[] To16Bytes(this string s)
		{
			List<byte> list = new List<byte>();
			string[] array = s.Split(new string[1]
			{
				" "
			}, StringSplitOptions.RemoveEmptyEntries);
			string[] array2 = array;
			foreach (string s2 in array2)
			{
				list.Add(s2.To16Byte());
			}
			return list.ToArray();
		}

		public static string ToSystemPath(this string path)
		{
			if (Environment.OSVersion.Platform == PlatformID.Unix)
			{
				return path.Replace("\\", "/");
			}
			return path.Replace("/", "\\");
		}

		public static string JsonConvert(this object o)
		{
			IsoDateTimeConverter isoDateTimeConverter = new IsoDateTimeConverter();
			isoDateTimeConverter.DateTimeFormat = "yyyy-MM-dd HH:mm:ss.fff";
			return Newtonsoft.Json.JsonConvert.SerializeObject(o, Formatting.Indented, isoDateTimeConverter);
		}

		public static string JsonConvert(this object o, bool isIndented)
		{
			return Newtonsoft.Json.JsonConvert.SerializeObject(o, isIndented ? Formatting.Indented : Formatting.None);
		}

		public static T JsonConvert<T>(this string str)
		{
			return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(str);
		}

		public static object JsonConvert(this string str, Type type)
		{
			return Newtonsoft.Json.JsonConvert.DeserializeObject(str, type);
		}

		public static T JsonConvert<T>(this string str, T obj)
		{
			return Newtonsoft.Json.JsonConvert.DeserializeAnonymousType(str, obj);
		}

		public static string StringToBase64String(this string value)
		{
			byte[] bytes = new UnicodeEncoding().GetBytes(value);
			int num = (int)Math.Ceiling((double)bytes.Length / 3.0) * 4;
			char[] array = new char[num];
			Convert.ToBase64CharArray(bytes, 0, bytes.Length, array, 0);
			return new string(array);
		}

		public static string Base64StringToString(this string base64)
		{
			char[] array = base64.ToCharArray();
			byte[] bytes = Convert.FromBase64CharArray(array, 0, array.Length);
			return new UnicodeEncoding().GetString(bytes);
		}

		public static string ToBase64(this byte[] binBuffer)
		{
			return binBuffer.ToBase64(0, binBuffer.Length);
		}

		public static string ToBase64(this byte[] binBuffer, int offset, int count)
		{
			int num = (int)Math.Ceiling((double)count / 3.0) * 4;
			char[] array = new char[num];
			Convert.ToBase64CharArray(binBuffer, offset, count, array, 0);
			return new string(array);
		}

		public static byte[] Base64ToBytes(this string base64)
		{
			char[] array = base64.ToCharArray();
			return Convert.FromBase64CharArray(array, 0, array.Length);
		}

		public static byte[] ToUTF8Bytes(this string value)
		{
			return Encoding.UTF8.GetBytes(value);
		}

		public static string ToUTF8String(this byte[] bytes)
		{
			return Encoding.UTF8.GetString(bytes);
		}

		public static string ToUTF8String(this byte[] bytes, int offset, int count)
		{
			return Encoding.UTF8.GetString(bytes, offset, count);
		}

		public static string EmptyString(string str1, string str2)
		{
			return str1.EmptyString(str2);
		}
	}
}
