using System;
using System.Data;

namespace GLib.Data.Entity
{
	public static class EntityHelper
	{
		public static DbType GetDbType(Type sysType)
		{
			DbType result = DbType.String;
			switch (sysType.Name)
			{
			case "String":
				result = DbType.String;
				break;
			case "Byte":
				result = DbType.Byte;
				break;
			case "Byte[]":
				result = DbType.Binary;
				break;
			case "Int16":
				result = DbType.Int16;
				break;
			case "Int32":
				result = DbType.Int32;
				break;
			case "Int64":
				result = DbType.Int64;
				break;
			case "DateTime":
				result = DbType.DateTime;
				break;
			case "Decimal":
				result = DbType.Decimal;
				break;
			case "Double":
				result = DbType.Double;
				break;
			case "Single":
				result = DbType.Single;
				break;
			case "Boolean":
				result = DbType.Boolean;
				break;
			case "Guid":
				result = DbType.Guid;
				break;
			}
			return result;
		}

		public static bool IsTypeMinValue(object obValue, DbType dbType)
		{
			bool result = false;
			switch (dbType)
			{
			case DbType.String:
				if (obValue == null)
				{
					result = true;
				}
				break;
			case DbType.Int16:
				if (Convert.ToInt16(obValue) == short.MinValue)
				{
					result = true;
				}
				break;
			case DbType.Int32:
				if (Convert.ToInt32(obValue) == int.MinValue)
				{
					result = true;
				}
				break;
			case DbType.Int64:
				if (Convert.ToInt64(obValue) == long.MinValue)
				{
					result = true;
				}
				break;
			case DbType.Decimal:
				if (Convert.ToDecimal(obValue) == decimal.MinValue)
				{
					result = true;
				}
				break;
			case DbType.Double:
				if (Convert.ToDouble(obValue) == double.MinValue)
				{
					result = true;
				}
				break;
			case DbType.Single:
				if (Convert.ToSingle(obValue) == float.MinValue)
				{
					result = true;
				}
				break;
			case DbType.DateTime:
				if (Convert.ToDateTime(obValue) == new DateTime(1900, 1, 1))
				{
					result = true;
				}
				break;
			}
			return result;
		}

		public static object GetTypeDefaultValue(object obValue, DbType dbType)
		{
			object result = obValue;
			switch (dbType)
			{
			case DbType.String:
				if (obValue == null || Convert.ToString(obValue) == string.Empty)
				{
					result = string.Empty;
				}
				break;
			case DbType.Int16:
				if (Convert.ToInt16(obValue) == short.MinValue)
				{
					result = 0;
				}
				break;
			case DbType.Int32:
				if (Convert.ToInt32(obValue) == int.MinValue)
				{
					result = 0;
				}
				break;
			case DbType.Int64:
				if (Convert.ToInt64(obValue) == long.MinValue)
				{
					result = 0;
				}
				break;
			case DbType.Byte:
				if (Convert.ToByte(obValue) == 0)
				{
					result = 0;
				}
				break;
			case DbType.Decimal:
				if (Convert.ToDecimal(obValue) == decimal.MinValue)
				{
					result = 0;
				}
				break;
			case DbType.Double:
				if (Convert.ToDouble(obValue) == double.MinValue)
				{
					result = 0;
				}
				break;
			case DbType.Single:
				if (Convert.ToSingle(obValue) == float.MinValue)
				{
					result = 0;
				}
				break;
			default:
				result = obValue;
				break;
			}
			return result;
		}
	}
}
