using System;
using System.Data;
using System.IO;
using System.Text;

namespace GLib.Utilities.IO
{
	public class TxtHelper
	{
		public static DataTable ReadFile(string filename, string[] split)
		{
			try
			{
				StreamReader streamReader = new StreamReader(filename, Encoding.Default);
				DataTable dataTable = new DataTable();
				int num = 0;
				string text;
				while ((text = streamReader.ReadLine()) != null)
				{
					string[] array = text.Split(split, StringSplitOptions.RemoveEmptyEntries);
					if (num == 0)
					{
						for (int i = 0; i < array.Length; i++)
						{
							dataTable.Columns.Add(array[i]);
						}
					}
					else
					{
						DataRow dataRow = dataTable.NewRow();
						for (int i = 0; i < array.Length; i++)
						{
							dataRow[i] = array[i];
						}
						dataTable.Rows.Add(dataRow);
					}
					num++;
				}
				return dataTable;
			}
			catch (Exception)
			{
			}
			return null;
		}
	}
}
