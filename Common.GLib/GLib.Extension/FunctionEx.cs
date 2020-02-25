using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace GLib.Extension
{
	public static class FunctionEx
	{
		private static byte[] ArrayCRCHigh = new byte[256]
		{
			0,
			193,
			129,
			64,
			1,
			192,
			128,
			65,
			1,
			192,
			128,
			65,
			0,
			193,
			129,
			64,
			1,
			192,
			128,
			65,
			0,
			193,
			129,
			64,
			0,
			193,
			129,
			64,
			1,
			192,
			128,
			65,
			1,
			192,
			128,
			65,
			0,
			193,
			129,
			64,
			0,
			193,
			129,
			64,
			1,
			192,
			128,
			65,
			0,
			193,
			129,
			64,
			1,
			192,
			128,
			65,
			1,
			192,
			128,
			65,
			0,
			193,
			129,
			64,
			1,
			192,
			128,
			65,
			0,
			193,
			129,
			64,
			0,
			193,
			129,
			64,
			1,
			192,
			128,
			65,
			0,
			193,
			129,
			64,
			1,
			192,
			128,
			65,
			1,
			192,
			128,
			65,
			0,
			193,
			129,
			64,
			0,
			193,
			129,
			64,
			1,
			192,
			128,
			65,
			1,
			192,
			128,
			65,
			0,
			193,
			129,
			64,
			1,
			192,
			128,
			65,
			0,
			193,
			129,
			64,
			0,
			193,
			129,
			64,
			1,
			192,
			128,
			65,
			1,
			192,
			128,
			65,
			0,
			193,
			129,
			64,
			0,
			193,
			129,
			64,
			1,
			192,
			128,
			65,
			0,
			193,
			129,
			64,
			1,
			192,
			128,
			65,
			1,
			192,
			128,
			65,
			0,
			193,
			129,
			64,
			0,
			193,
			129,
			64,
			1,
			192,
			128,
			65,
			1,
			192,
			128,
			65,
			0,
			193,
			129,
			64,
			1,
			192,
			128,
			65,
			0,
			193,
			129,
			64,
			0,
			193,
			129,
			64,
			1,
			192,
			128,
			65,
			0,
			193,
			129,
			64,
			1,
			192,
			128,
			65,
			1,
			192,
			128,
			65,
			0,
			193,
			129,
			64,
			1,
			192,
			128,
			65,
			0,
			193,
			129,
			64,
			0,
			193,
			129,
			64,
			1,
			192,
			128,
			65,
			1,
			192,
			128,
			65,
			0,
			193,
			129,
			64,
			0,
			193,
			129,
			64,
			1,
			192,
			128,
			65,
			0,
			193,
			129,
			64,
			1,
			192,
			128,
			65,
			1,
			192,
			128,
			65,
			0,
			193,
			129,
			64
		};

		private static byte[] checkCRCLow = new byte[256]
		{
			0,
			192,
			193,
			1,
			195,
			3,
			2,
			194,
			198,
			6,
			7,
			199,
			5,
			197,
			196,
			4,
			204,
			12,
			13,
			205,
			15,
			207,
			206,
			14,
			10,
			202,
			203,
			11,
			201,
			9,
			8,
			200,
			216,
			24,
			25,
			217,
			27,
			219,
			218,
			26,
			30,
			222,
			223,
			31,
			221,
			29,
			28,
			220,
			20,
			212,
			213,
			21,
			215,
			23,
			22,
			214,
			210,
			18,
			19,
			211,
			17,
			209,
			208,
			16,
			240,
			48,
			49,
			241,
			51,
			243,
			242,
			50,
			54,
			246,
			247,
			55,
			245,
			53,
			52,
			244,
			60,
			252,
			253,
			61,
			255,
			63,
			62,
			254,
			250,
			58,
			59,
			251,
			57,
			249,
			248,
			56,
			40,
			232,
			233,
			41,
			235,
			43,
			42,
			234,
			238,
			46,
			47,
			239,
			45,
			237,
			236,
			44,
			228,
			36,
			37,
			229,
			39,
			231,
			230,
			38,
			34,
			226,
			227,
			35,
			225,
			33,
			32,
			224,
			160,
			96,
			97,
			161,
			99,
			163,
			162,
			98,
			102,
			166,
			167,
			103,
			165,
			101,
			100,
			164,
			108,
			172,
			173,
			109,
			175,
			111,
			110,
			174,
			170,
			106,
			107,
			171,
			105,
			169,
			168,
			104,
			120,
			184,
			185,
			121,
			187,
			123,
			122,
			186,
			190,
			126,
			127,
			191,
			125,
			189,
			188,
			124,
			180,
			116,
			117,
			181,
			119,
			183,
			182,
			118,
			114,
			178,
			179,
			115,
			177,
			113,
			112,
			176,
			80,
			144,
			145,
			81,
			147,
			83,
			82,
			146,
			150,
			86,
			87,
			151,
			85,
			149,
			148,
			84,
			156,
			92,
			93,
			157,
			95,
			159,
			158,
			94,
			90,
			154,
			155,
			91,
			153,
			89,
			88,
			152,
			136,
			72,
			73,
			137,
			75,
			139,
			138,
			74,
			78,
			142,
			143,
			79,
			141,
			77,
			76,
			140,
			68,
			132,
			133,
			69,
			135,
			71,
			70,
			134,
			130,
			66,
			67,
			131,
			65,
			129,
			128,
			64
		};

		public static string EmptyString(this string value)
		{
			return value.EmptyString(null);
		}

		public static string EmptyString(this string value, string def)
		{
			if (value != null)
			{
				if (value.Trim() == "")
				{
					return def;
				}
				return value;
			}
			return def;
		}

		public static decimal EmptyDecimal(this string value, decimal def = 0m)
		{
			if (value.EmptyString() == null)
			{
				return def;
			}
			return value.Convert<decimal>();
		}

		public static int? EmptyInt(this string value)
		{
			if (value.EmptyString() == null)
			{
				return null;
			}
			return value.Convert<int>();
		}

		public static int EmptyInt(this string value, int def = 0)
		{
			if (value.EmptyString() == null)
			{
				return def;
			}
			return value.Convert<int>();
		}

		public static long? EmptyLong(this string value)
		{
			if (value.EmptyString() == null)
			{
				return null;
			}
			return value.Convert<long>();
		}

		public static long EmptyLong(this string value, long def = 0L)
		{
			if (value.EmptyString() == null)
			{
				return def;
			}
			return value.Convert<long>();
		}

		public static string EmptyString(this int? value, string def = "")
		{
			return value.HasValue ? string.Concat(value) : def;
		}

		public static string EmptyString(this decimal? value, string def = "")
		{
			if (value.HasValue)
			{
				if (value.Value == 0m)
				{
					return "0";
				}
				return value.Value.ToString("G0");
			}
			return def;
		}

		public static string EmptyString(this long? value, string def = "")
		{
			return value.HasValue ? string.Concat(value) : def;
		}

		public static string EmptyString(this DateTime? value, string def = "")
		{
			return value.HasValue ? value.ToDateTime() : def;
		}

		public static string EmptyString(this object value, string def = "")
		{
			if (value != null && (value is decimal || value is decimal))
			{
				if (value is decimal?)
				{
					return ((decimal?)value).EmptyString();
				}
				return EmptyString((decimal)value);
			}
			return string.Concat(value).EmptyString(def);
		}

		public static DateTime? EmptyDateTime(this string value)
		{
			if (value.EmptyString() == null)
			{
				return null;
			}
			return value.Convert<DateTime>();
		}

		public static DateTime EmptyDateTime(this string value, DateTime def)
		{
			if (value.EmptyString() == null)
			{
				return def;
			}
			return value.Convert<DateTime>();
		}

		public static T NullType<T>(T? value) where T : struct
		{
			return NullType(value, default(T));
		}

		public static T NullType<T>(T? value, T def) where T : struct
		{
			if (value.HasValue)
			{
				return value.Value;
			}
			return def;
		}

		public static int NullInt(this int? value)
		{
			return NullType(value, 0);
		}

		public static int NullInt(this int? value, int def)
		{
			return NullType(value, def);
		}

		public static int NullInt(this object value, int def = 0)
		{
			if (value is int)
			{
				return (int)value;
			}
			if (value is int?)
			{
				if (((int?)value).HasValue)
				{
					return ((int?)value).Value;
				}
				return def;
			}
			if (value is string)
			{
				return ((string)value).EmptyInt(def);
			}
			return def;
		}

		public static decimal NullDecimal(this decimal? value)
		{
			return NullType(value, 0m);
		}

		public static decimal NullDecimal(this decimal? value, decimal def)
		{
			return NullType(value, def);
		}

		public static decimal NullDecimal(this object value, long def = 0L)
		{
			if (value is decimal)
			{
				return (decimal)value;
			}
			if (value is decimal?)
			{
				if (((decimal?)value).HasValue)
				{
					return ((decimal?)value).Value;
				}
				return def;
			}
			if (value is string)
			{
				return ((string)value).EmptyDecimal(def);
			}
			return def;
		}

		public static long NullLong(this long? value)
		{
			return NullType(value, 0L);
		}

		public static long NullLong(this long? value, long def)
		{
			return NullType(value, def);
		}

		public static long NullLong(this object value, long def = 0L)
		{
			if (value is long)
			{
				return (long)value;
			}
			if (value is long?)
			{
				if (((long?)value).HasValue)
				{
					return ((long?)value).Value;
				}
				return def;
			}
			if (value is string)
			{
				return ((string)value).EmptyLong(def);
			}
			return def;
		}

		public static bool NullBool(this bool? value)
		{
			return value.NullBool(def: false);
		}

		public static bool NullBool(this bool? value, bool def = false)
		{
			if (value.HasValue)
			{
				return value.Value;
			}
			return def;
		}

		public static bool NullBool(this object value, bool def = false)
		{
			if (value is bool)
			{
				return (bool)value;
			}
			if (value is bool?)
			{
				if (((bool?)value).HasValue)
				{
					return ((bool?)value).Value;
				}
				return def;
			}
			return def;
		}

		public static T Convert<T>(this object value)
		{
			return value.Convert<T>(showError: true);
		}

		public static T Convert<T>(this object value, bool showError)
		{
			Exception ex = null;
			T outValue = default(T);
			if (!value.Convert(out outValue, out ex) && showError)
			{
				throw ex;
			}
			return outValue;
		}

		public static bool Convert<T>(this object value, out T outValue)
		{
			Exception ex = null;
			return value.Convert(out outValue, out ex);
		}

		public static T Convert<T>(this object value, string message)
		{
			T val = default(T);
			try
			{
				return value.Convert<T>();
			}
			catch
			{
				throw new ArgumentException(message);
			}
		}

		private static bool Convert<T>(this object value, out T outValue, out Exception ex)
		{
			bool result = false;
			ex = null;
			try
			{
				Type typeFromHandle = typeof(T);
				if (value is T)
				{
					outValue = (T)value;
				}
				else if (typeFromHandle.IsEnum)
				{
					outValue = (T)Enum.Parse(typeof(T), value.ToString(), ignoreCase: true);
				}
				else
				{
					outValue = (T)System.Convert.ChangeType(value, typeof(T));
				}
				result = true;
			}
			catch (Exception ex2)
			{
				Exception ex3 = ex = ex2;
				outValue = default(T);
			}
			return result;
		}

		public static T Convert<T>(this string value)
		{
			return ((object)value).Convert<T>();
		}

		public static T[] Convert<T>(this Array Collection)
		{
			List<T> list = new List<T>();
			foreach (object item in Collection)
			{
				list.Add(item.Convert<T>());
			}
			return list.ToArray();
		}

		public static T Convert<T, U>(this IEnumerable<U> items) where T : ICollection<U>, new()
		{
			T result = new T();
			foreach (U item in items)
			{
				result.Add(item);
			}
			return result;
		}

		public static T BytesToStruct<T>(byte[] bytes, int startIndex, int length)
		{
			if (bytes == null)
			{
				return default(T);
			}
			if (bytes.Length <= 0)
			{
				return default(T);
			}
			IntPtr intPtr = Marshal.AllocHGlobal(length);
			try
			{
				Marshal.Copy(bytes, startIndex, intPtr, length);
				return (T)Marshal.PtrToStructure(intPtr, typeof(T));
			}
			catch (Exception ex)
			{
				throw new Exception("Error in BytesToStruct ! " + ex.Message);
			}
			finally
			{
				Marshal.FreeHGlobal(intPtr);
			}
		}

		public static T BytesToStruct<T>(byte[] bytes)
		{
			return BytesToStruct<T>(bytes, 0, bytes.Length);
		}

		public static byte[] StructToBytes(object structObj, int size)
		{
			IntPtr intPtr = Marshal.AllocHGlobal(size);
			try
			{
				Marshal.StructureToPtr(structObj, intPtr, fDeleteOld: false);
				byte[] array = new byte[size];
				Marshal.Copy(intPtr, array, 0, size);
				return array;
			}
			catch (Exception ex)
			{
				throw new Exception("Error in StructToBytes ! " + ex.Message);
			}
			finally
			{
				Marshal.FreeHGlobal(intPtr);
			}
		}

		public static byte[] StructToBytes(object structObj)
		{
			int size = Marshal.SizeOf(structObj);
			return StructToBytes(structObj, size);
		}

		public static IntPtr StructToIntPtr(object structObj)
		{
			byte[] buff = StructToBytes(structObj);
			return BytesToIntPtr(buff);
		}

		public static T IntPtrToStruct<T>(IntPtr intptr)
		{
			int index = 0;
			int length = Marshal.SizeOf(typeof(T));
			return IntPtrToStruct<T>(intptr, index, length);
		}

		public static T IntPtrToStruct<T>(IntPtr intptr, int index, int length)
		{
			byte[] array = new byte[length];
			Marshal.Copy(intptr, array, index, length);
			return BytesToStruct<T>(array, 0, array.Length);
		}

		public static IntPtr BytesToIntPtr(byte[] buff)
		{
			IntPtr intPtr = Marshal.AllocHGlobal(buff.Length);
			Marshal.Copy(buff, 0, intPtr, buff.Length);
			return intPtr;
		}

		public static byte[] IntPtrToBytes(IntPtr intptr, int index, int length)
		{
			byte[] array = new byte[length];
			Marshal.Copy(intptr, array, index, length);
			return array;
		}

		public static void IntPtrSetValue(IntPtr intptr, object structObj)
		{
			IntPtrSetValue(intptr, StructToBytes(structObj));
		}

		public static void IntPtrSetValue(IntPtr intptr, byte[] bytes)
		{
			Marshal.Copy(bytes, 0, intptr, bytes.Length);
		}

		public static long GetHashNum(this Guid guid)
		{
			return guid.ToString().Replace("-", "").GetHashNum();
		}

		public static long GetHashNum(this string szStr)
		{
			long num = 5381L;
			foreach (int num2 in szStr)
			{
				num = (((num << 5) + num) ^ num2);
			}
			return num;
		}

		public static long GenerateUniqueCode()
		{
			return Guid.NewGuid().ToString().Replace("-", "")
				.GetHashNum();
		}

		public static string ToDateTime(this DateTime? time)
		{
			return (!time.HasValue) ? "" : time.Value.ToString("yyyy-MM-dd");
		}

		public static string ToDisplayTime(this DateTime? time)
		{
			return (!time.HasValue) ? "" : time.Value.ToString("yyyy年MM月dd日");
		}

		public static string ToDisplayTime(this DateTime time)
		{
			return time.ToString("yyyy年MM月dd日");
		}

		public static string ToDateTime(this DateTime time)
		{
			return time.ToString("yyyy-MM-dd");
		}

		public static string ToDecimalString(this decimal? d)
		{
			string result = "";
			if (d.HasValue)
			{
				result = d.ToString();
			}
			return result;
		}

		public static string ToIntString(this int? n)
		{
			string result = "";
			if (n.HasValue)
			{
				result = n.ToString();
			}
			return result;
		}

		public static string ToSString(this string ss)
		{
			string result = "";
			if (!string.IsNullOrEmpty(ss))
			{
				result = ss;
			}
			return result;
		}

		public static string ToDateTime(this object time)
		{
			if (time is DateTime)
			{
				return ((DateTime)time).ToString("yyyy-MM-dd");
			}
			if (time is DateTime? && ((DateTime?)time).HasValue)
			{
				return ((DateTime?)time).Value.ToString("yyyy-MM-dd");
			}
			return null;
		}

		public static bool IsNotNull(this string s)
		{
			return !string.IsNullOrEmpty(s);
		}

		public static bool IsNullOrEmpty(this string s)
		{
			return string.IsNullOrEmpty(s);
		}

		public static byte[] ToByteArray(short[] src)
		{
			int num = src.Length * 2;
			IntPtr intPtr = Marshal.AllocHGlobal(num);
			Marshal.Copy(src, 0, intPtr, src.Length);
			byte[] array = new byte[num];
			Marshal.Copy(intPtr, array, 0, num);
			Marshal.FreeHGlobal(intPtr);
			return array;
		}

		public static byte[] ToByteArray(int[] src)
		{
			int num = src.Length * 4;
			IntPtr intPtr = Marshal.AllocHGlobal(num);
			Marshal.Copy(src, 0, intPtr, src.Length);
			byte[] array = new byte[num];
			Marshal.Copy(intPtr, array, 0, num);
			Marshal.FreeHGlobal(intPtr);
			return array;
		}

		public static short[] ToShortArray(byte[] src)
		{
			int num = (src.Length % 2 == 0) ? (src.Length / 2) : (src.Length / 2 + 1);
			IntPtr intPtr = Marshal.AllocHGlobal(src.Length);
			Marshal.Copy(src, 0, intPtr, src.Length);
			short[] array = new short[num];
			Marshal.Copy(intPtr, array, 0, array.Length);
			Marshal.FreeHGlobal(intPtr);
			return array;
		}

		public static int[] ToIntArray(byte[] src)
		{
			int num = (src.Length % 4 == 0) ? (src.Length / 4) : (src.Length / 4 + 1);
			IntPtr intPtr = Marshal.AllocHGlobal(src.Length);
			Marshal.Copy(src, 0, intPtr, src.Length);
			int[] array = new int[num];
			Marshal.Copy(intPtr, array, 0, array.Length);
			Marshal.FreeHGlobal(intPtr);
			return array;
		}

		public static short CRC16(this byte[] data)
		{
			return CRC16(data, data.Length);
		}

		public static short CRC16(byte[] data, int arrayLength)
		{
			byte b = byte.MaxValue;
			byte b2 = byte.MaxValue;
			int num = 0;
			while (arrayLength-- > 0)
			{
				byte b3 = (byte)(b ^ data[num++]);
				b = (byte)(b2 ^ ArrayCRCHigh[b3]);
				b2 = checkCRCLow[b3];
			}
			return (short)((b << 8) | b2);
		}
	}
}
