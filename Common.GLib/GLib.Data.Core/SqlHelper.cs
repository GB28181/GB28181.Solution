using GLib.Data.Entity;
using GLib.Logging;
using MySql.Data.MySqlClient;
using System;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Data.OleDb;
using System.Data.OracleClient;
using System.Data.SqlClient;

namespace GLib.Data.Core
{
	public class SqlHelper
	{
		public static string GetConnectionString(string dbName)
		{
			if (string.IsNullOrEmpty(dbName))
			{
				throw new ArgumentNullException("ConnectionString is null");
			}
			return ConfigurationManager.ConnectionStrings[dbName].ConnectionString;
		}

		public static DataBaseType GetDBType()
		{
			switch (ConfigurationManager.AppSettings["DBType"])
			{
			case "SqlServer":
				return DataBaseType.SqlServer;
			case "Access":
				return DataBaseType.Access;
			case "MySql":
				return DataBaseType.MySql;
			case "Oracle":
				return DataBaseType.Oracle;
			default:
				return DataBaseType.SqlServer;
			}
		}

		public static DbProviderFactory GetDBFactory(DataBaseType dbType)
		{
			switch (dbType)
			{
			case DataBaseType.SqlServer:
				return SqlClientFactory.Instance;
			case DataBaseType.Access:
				return OleDbFactory.Instance;
			case DataBaseType.MySql:
				return (DbProviderFactory)(object)MySqlClientFactory.Instance;
			//case DataBaseType.Oracle:
			//	return OracleClientFactory.Instance;
			default:
				return SqlClientFactory.Instance;
			}
		}

		public static string ParseSelector(string selector, params DBParam[] values)
		{
			string text = selector;
			if (values != null)
			{
				for (int i = 0; i < values.Length; i++)
				{
					if (string.IsNullOrEmpty(values[i].ParamName))
					{
						text = text.Replace($"@{i}", $"@param{i}");
						values[i].ParamName = $"param{i}";
					}
					else
					{
						text = text.Replace($"@{i}", "@" + values[i].ParamName);
					}
				}
			}
			return text.Replace("==", " = ").Replace("&&", " and ").Replace("||", " or ");
		}

		public static DbCommand GetCommand(string dbName, string strCommand)
		{
			return GetCommand(dbName, strCommand, 30);
		}

		public static DbCommand GetCommand(string dbName, string strCommand, int timeOut)
		{
			DbProviderFactory dBFactory = GetDBFactory(GetDBType());
			DbCommand dbCommand = dBFactory.CreateCommand();
			string text = GetConnectionString(dbName);
			if (GetDBType() == DataBaseType.MySql)
			{
				strCommand = strCommand.Replace("[", "").Replace("]", "");
			}
			if (GetDBType() == DataBaseType.Access)
			{
				text = text.Replace("@Path", AppDomain.CurrentDomain.BaseDirectory);
			}
			dbCommand.CommandText = strCommand;
			DbConnection dbConnection = dBFactory.CreateConnection();
			dbConnection.ConnectionString = text;
			dbConnection.Open();
			dbCommand.Connection = dbConnection;
			dbCommand.CommandTimeout = timeOut;
			return dbCommand;
		}

		public static IDataReader ExecuteDataReader(string dbName, string strCommand, CommandType commandType, DBParam[] listParam)
		{
			return ExecuteDataReader(dbName, strCommand, commandType, listParam, 30);
		}

		public static IDataReader ExecuteDataReader(string dbName, string strCommand, CommandType commandType, DBParam[] listParam, int timeOut)
		{
			strCommand = ParseSelector(strCommand, listParam);
			DbCommand command = GetCommand(dbName, strCommand, timeOut);
			command.CommandType = commandType;
			if (listParam != null)
			{
				DbParameterCollection parameters = command.Parameters;
				foreach (DBParam dBParam in listParam)
				{
					DbParameter dbParameter = command.CreateParameter();
					dbParameter.ParameterName = dBParam.ParamName;
					dbParameter.DbType = dBParam.ParamDbType;
					dbParameter.Value = dBParam.ParamValue;
					if (!parameters.Contains(dbParameter))
					{
						parameters.Add(dbParameter);
					}
				}
			}
			try
			{
				IDataReader result = command.ExecuteReader(CommandBehavior.CloseConnection);
				command.Parameters.Clear();
				return result;
			}
			catch (Exception ex)
			{
				if (command.Connection.State == ConnectionState.Open)
				{
					command.Connection.Close();
					command.Connection.Dispose();
				}
				Log log = new Log();
				log.Write(new LogMessage
				{
					Content = "GLib.Data.Core.SqlHelper.ExecuteDataReader：\r\n" + ex.Message,
					Type = MessageType.Error
				});
				return null;
			}
		}

		public static int ExecuteNonQuery(string dbName, string strCommand, CommandType commandType, DBParam[] listParam)
		{
			strCommand = ParseSelector(strCommand, listParam);
			int result = -1;
			using (DbCommand dbCommand = GetCommand(dbName, strCommand))
			{
				dbCommand.CommandType = commandType;
				if (listParam != null)
				{
					DbParameterCollection parameters = dbCommand.Parameters;
					foreach (DBParam dBParam in listParam)
					{
						DbParameter dbParameter = dbCommand.CreateParameter();
						dbParameter.ParameterName = dBParam.ParamName;
						dbParameter.DbType = dBParam.ParamDbType;
						dbParameter.Value = dBParam.ParamValue;
						if (!parameters.Contains(dbParameter))
						{
							parameters.Add(dbParameter);
						}
					}
				}
				result = dbCommand.ExecuteNonQuery();
				dbCommand.Connection.Close();
				dbCommand.Dispose();
			}
			return result;
		}

		public static object ExecuteScalar(string dbName, string strCommand, CommandType commandType, DBParam[] listParam)
		{
			strCommand = ParseSelector(strCommand, listParam);
			object result = null;
			using (DbCommand dbCommand = GetCommand(dbName, strCommand))
			{
				dbCommand.CommandType = commandType;
				if (listParam != null)
				{
					DbParameterCollection parameters = dbCommand.Parameters;
					foreach (DBParam dBParam in listParam)
					{
						DbParameter dbParameter = dbCommand.CreateParameter();
						dbParameter.ParameterName = dBParam.ParamName;
						dbParameter.DbType = dBParam.ParamDbType;
						dbParameter.Value = dBParam.ParamValue;
						if (!parameters.Contains(dbParameter))
						{
							parameters.Add(dbParameter);
						}
					}
				}
				result = dbCommand.ExecuteScalar();
				dbCommand.Connection.Close();
				dbCommand.Dispose();
			}
			return result;
		}

		public static DataTable ExecuteTable(string dbName, string strCommand, CommandType commandType, DBParam[] listParam)
		{
			string text = GetConnectionString(dbName);
			if (GetDBType() == DataBaseType.MySql)
			{
				strCommand = strCommand.Replace("[", "").Replace("]", "");
			}
			if (GetDBType() == DataBaseType.Access)
			{
				text = text.Replace("@Path", AppDomain.CurrentDomain.BaseDirectory);
			}
			DataTable dataTable = new DataTable();
			DbProviderFactory dBFactory = GetDBFactory(GetDBType());
			using (DbCommand dbCommand = dBFactory.CreateCommand())
			{
				DbConnection dbConnection = dBFactory.CreateConnection();
				dbConnection.ConnectionString = text;
				dbConnection.Open();
				dbCommand.Connection = dbConnection;
				dbCommand.CommandTimeout = 30;
				dbCommand.CommandType = commandType;
				dbCommand.CommandText = ParseSelector(strCommand, listParam);
				if (listParam != null)
				{
					DbParameterCollection parameters = dbCommand.Parameters;
					foreach (DBParam dBParam in listParam)
					{
						DbParameter dbParameter = dbCommand.CreateParameter();
						dbParameter.ParameterName = dBParam.ParamName;
						dbParameter.DbType = dBParam.ParamDbType;
						dbParameter.Value = dBParam.ParamValue;
						if (!parameters.Contains(dbParameter))
						{
							parameters.Add(dbParameter);
						}
					}
				}
				DbDataAdapter dbDataAdapter = dBFactory.CreateDataAdapter();
				dbDataAdapter.SelectCommand = dbCommand;
				dbDataAdapter.Fill(dataTable);
				dbCommand.Connection.Close();
				dbCommand.Dispose();
			}
			return dataTable;
		}
	}
}
