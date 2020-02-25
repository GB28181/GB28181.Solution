using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Text;

namespace GLib.Data
{
	public abstract class DBT_SqlHelper
	{
		public static string connectionString = string.Concat(ConfigurationSettings.AppSettings["con"]);

		public static int ExecteNonQuery(string connectionString, CommandType cmdType, string cmdText, params SqlParameter[] commandParameters)
		{
			SqlCommand sqlCommand = new SqlCommand();
			using (SqlConnection conn = new SqlConnection(connectionString))
			{
				PrepareCommand(sqlCommand, conn, null, cmdType, cmdText, commandParameters);
				int result = sqlCommand.ExecuteNonQuery();
				sqlCommand.Parameters.Clear();
				return result;
			}
		}

		public static int ExecteNonQueryProducts(string cmdText, params SqlParameter[] commandParameters)
		{
			return ExecteNonQuery(connectionString, CommandType.StoredProcedure, cmdText, commandParameters);
		}

		public static int ExecteNonQueryText(string cmdText, params SqlParameter[] commandParameters)
		{
			return ExecteNonQuery(connectionString, CommandType.Text, cmdText, commandParameters);
		}

		private static void PrepareCommand(SqlCommand cmd, SqlConnection conn, SqlTransaction trans, CommandType cmdType, string cmdText, SqlParameter[] cmdParms)
		{
			if (conn.State != ConnectionState.Open)
			{
				conn.Open();
			}
			cmd.Connection = conn;
			cmd.CommandText = cmdText;
			if (trans != null)
			{
				cmd.Transaction = trans;
			}
			cmd.CommandType = cmdType;
			if (cmdParms != null)
			{
				foreach (SqlParameter value in cmdParms)
				{
					cmd.Parameters.Add(value);
				}
			}
		}

		public static void CreateDatabase(string dbName, string dbFileName, string dbSize, string dbMaxSize, string dbFileGrowth, string logName, string logFileName, string logSize, string logMaxSize, string logFileGrowth, bool isDeletedb)
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append("USE master ");
			stringBuilder.Append("  GO");
			if (isDeletedb)
			{
				stringBuilder.Append("IF  EXISTS（SELECT * FROM  sysdatabases WHERE  name ='@dbName'）begin DROP DATABASE @dbName  end");
			}
			stringBuilder.Append("CREATE DATABASE @dbName ON  PRIMARY (");
			stringBuilder.Append("NAME='@ dbName_data',");
			stringBuilder.Append("FILENAME='@dbFileName', ");
			stringBuilder.Append("SIZE=@dbSize, ");
			stringBuilder.Append("MAXSIZE= @dbMaxSize,");
			stringBuilder.Append("FILEGROWTH=@dbFileGrowth)");
			stringBuilder.Append("LOG ON (");
			stringBuilder.Append("NAME='@logName_log',");
			stringBuilder.Append("FILENAME='@logFileName',");
			stringBuilder.Append("SIZE=@logSize,");
			stringBuilder.Append("MAXSIZE=@logMaxSize,");
			stringBuilder.Append("FILEGROWTH=@logFileGrowth ) GO");
			SqlParameter[] array = new SqlParameter[10]
			{
				new SqlParameter("@dbName", dbName),
				new SqlParameter("@dbFileName", dbFileName),
				new SqlParameter("@dbSize", dbSize),
				new SqlParameter("@dbMaxSize", dbMaxSize),
				new SqlParameter("@dbFileGrowth", dbFileGrowth),
				new SqlParameter("@logName", logName),
				new SqlParameter("@logFileName", logFileName),
				new SqlParameter("@logSize", logSize),
				new SqlParameter("@logMaxSize", logMaxSize),
				new SqlParameter("@logFileGrowth", logFileGrowth)
			};
			ExecteNonQueryText(stringBuilder.ToString().Trim(), (SqlParameter[])null);
		}

		public static void DropDatabase(string dbName)
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine("USE master ");
			stringBuilder.AppendLine("DROP DATABASE " + dbName);
			SqlParameter[] array = new SqlParameter[1]
			{
				new SqlParameter("@dbName", dbName)
			};
			ExecteNonQueryText(stringBuilder.ToString().Trim());
		}

		public static void BackupDatabase(string dbName, string dbFileName)
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append("USE master ");
			stringBuilder.Append("  GO  ");
			stringBuilder.Append("BACKUP DATABASE @dbName TO DISK ='@dbFileName'");
			SqlParameter[] commandParameters = new SqlParameter[2]
			{
				new SqlParameter("@dbName", dbName),
				new SqlParameter("@dbFileName", dbFileName)
			};
			ExecteNonQueryText(stringBuilder.ToString().Trim(), commandParameters);
		}

		public static void RestoreDatabase(string dbName, string dbFileName)
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append("USE master ");
			stringBuilder.Append("  GO  ");
			stringBuilder.Append("restore database @dbName from disk='@dbFileName'  WITH REPLACE,RECOVERY");
			SqlParameter[] array = new SqlParameter[2]
			{
				new SqlParameter("@dbName", dbName),
				new SqlParameter("@dbFileName", dbFileName)
			};
			ExecteNonQueryText(stringBuilder.ToString().Trim(), (SqlParameter[])null);
		}

		public static void OnlineDatabase(string newDbName, string dbFileName, string logFileName)
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append("USE master ");
			stringBuilder.Append("  GO  ");
			stringBuilder.Append("EXEC sp_attach_db @ newDbName,'@dbFileName','@logFileName'");
			SqlParameter[] commandParameters = new SqlParameter[2]
			{
				new SqlParameter("@dbFileName", dbFileName),
				new SqlParameter("@logFileName", logFileName)
			};
			ExecteNonQueryText(stringBuilder.ToString().Trim(), commandParameters);
		}

		public static void OfflineDatabase(string dbName)
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append("USE master ");
			stringBuilder.Append("  GO  ");
			stringBuilder.Append(" exec  sp_detach_db '@dbName' ");
			SqlParameter[] array = new SqlParameter[1]
			{
				new SqlParameter("@dbName", dbName)
			};
			ExecteNonQueryText(stringBuilder.ToString().Trim(), (SqlParameter[])null);
		}

		public static void ResetPassword(string newPassword, string userName)
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append("USE master ");
			stringBuilder.Append("  GO  ");
			stringBuilder.Append("EXEC   sp_password null,'@newPassword','@userName'");
			SqlParameter[] array = new SqlParameter[2]
			{
				new SqlParameter("@newPassword", newPassword),
				new SqlParameter("@userName", userName)
			};
			ExecteNonQueryText(stringBuilder.ToString().Trim(), (SqlParameter[])null);
		}

		public static void CreateDbUser(string dbName, string userName, string passWord)
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append("USE  " + dbName);
			stringBuilder.Append("  GO  ");
			stringBuilder.Append("EXEC sp_addlogin N'@userName','@passWord'");
			stringBuilder.Append("EXEC sp_grantdbaccess N'@userName'");
			SqlParameter[] array = new SqlParameter[3]
			{
				new SqlParameter("@dbName", userName),
				new SqlParameter("@userName", userName),
				new SqlParameter("@passWord", passWord)
			};
			ExecteNonQueryText(stringBuilder.ToString().Trim(), (SqlParameter[])null);
		}

		public static void AddRoleToDbUser(string dbName, string userName)
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append("USE " + dbName);
			stringBuilder.Append("GO ");
			stringBuilder.Append("EXEC sp_addrolemember N'@dbName', N'@userName'");
			SqlParameter[] array = new SqlParameter[2]
			{
				new SqlParameter("@dbName", userName),
				new SqlParameter("@userName", userName)
			};
			ExecteNonQueryText(stringBuilder.ToString().Trim(), (SqlParameter[])null);
		}

		public static void ExecuteSQLFile(string sqlFileName)
		{
			SqlConnection sqlConnection = null;
			try
			{
				sqlConnection = new SqlConnection(connectionString);
				SqlCommand sqlCommand = sqlConnection.CreateCommand();
				sqlConnection.Open();
				using (FileStream fileStream = new FileStream(sqlFileName, FileMode.Open, FileAccess.ReadWrite))
				{
					StreamReader streamReader = new StreamReader(fileStream, Encoding.Default);
					StringBuilder stringBuilder = new StringBuilder();
					string text = "";
					while ((text = streamReader.ReadLine()) != null)
					{
						if (text.Trim().ToUpper() != "GO")
						{
							stringBuilder.AppendLine(text);
						}
						else
						{
							sqlCommand.CommandText = stringBuilder.ToString();
							sqlCommand.ExecuteNonQuery();
							stringBuilder.Remove(0, stringBuilder.Length);
						}
					}
					fileStream.Dispose();
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
			}
			finally
			{
				if (sqlConnection != null && sqlConnection.State != 0)
				{
					sqlConnection.Close();
				}
			}
		}

		public static void Test()
		{
			connectionString = "server=.;uid=sa;password=123456;database=master";
			DropDatabase("db");
		}
	}
}
