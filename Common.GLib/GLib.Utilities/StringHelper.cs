using GLib.Data.Entity;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace GLib.Utilities
{
	public class StringHelper
	{
		public static string GetOrSelector(string col, string strid, out IList<DBParam> outlist)
		{
			string[] array = strid.Split(new char[1]
			{
				','
			}, StringSplitOptions.RemoveEmptyEntries);
			StringBuilder stringBuilder = new StringBuilder();
			outlist = new List<DBParam>();
			for (int i = 0; i < array.Length; i++)
			{
				if (i == 0)
				{
					stringBuilder.Append(" " + col + "=@" + i);
				}
				else
				{
					stringBuilder.Append("  or " + col + "=@" + i);
				}
				outlist.Add(new DBParam
				{
					ParamDbType = DbType.Int32,
					ParamValue = int.Parse(array[i])
				});
			}
			return stringBuilder.ToString();
		}

		public static string FormatSqlString(string strInput)
		{
			return strInput.Replace("'", "''");
		}

		public static string FormatDataTime(DateTime dt)
		{
			return dt.ToString("yyyy-MM-dd HH:mm");
		}
	}
}
