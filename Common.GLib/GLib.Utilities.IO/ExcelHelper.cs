using System.Data;
using System.Data.OleDb;

namespace GLib.Utilities.IO
{
	public class ExcelHelper
	{
		public static DataTable ReadExcel(string fileName, string Sheet)
		{
			DataSet dataSet = new DataSet();
			string connectionString = "Provider=Microsoft.Jet.Oledb.4.0;Data Source=" + fileName + ";Extended Properties=Excel 8.0";
			OleDbConnection oleDbConnection = new OleDbConnection(connectionString);
			try
			{
				oleDbConnection.Open();
				DataTable oleDbSchemaTable = oleDbConnection.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, null);
				string[] array = new string[oleDbSchemaTable.Rows.Count];
				int num = 0;
				foreach (DataRow row in oleDbSchemaTable.Rows)
				{
					array[num] = row[2].ToString();
					num++;
				}
				for (int i = 0; i < array.Length; i++)
				{
					OleDbDataAdapter oleDbDataAdapter = new OleDbDataAdapter("SELECT * FROM [" + array[i] + "]", oleDbConnection);
					oleDbDataAdapter.Fill(dataSet, "TEMPTABLE");
				}
			}
			catch
			{
			}
			oleDbConnection.Close();
			return dataSet.Tables["TEMPTABLE"];
		}
	}
}
