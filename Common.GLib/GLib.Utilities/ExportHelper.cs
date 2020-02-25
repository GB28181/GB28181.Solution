using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using System.Web;
using System.Xml;
using System.Xml.Xsl;

namespace GLib.Utilities
{
	public class ExportHelper
	{
		public static void Export(DataTable dt, ExportFormat exportFormat, string fileName, Encoding encoding)
		{
			DataSet dataSet = new DataSet("Export");
			DataTable dataTable = dt.Copy();
			dataTable.TableName = "Values";
			dataSet.Tables.Add(dataTable);
			string[] array = new string[dataTable.Columns.Count];
			string[] array2 = new string[dataTable.Columns.Count];
			for (int i = 0; i < dataTable.Columns.Count; i++)
			{
				array[i] = dataTable.Columns[i].ColumnName;
				array2[i] = ReplaceSpecialChars(dataTable.Columns[i].ColumnName);
			}
			Export(dataSet, array, array2, exportFormat, fileName, encoding, null);
		}

		public static void Export(DataTable dt, int[] columnIndexList, ExportFormat exportFormat, string fileName, Encoding encoding)
		{
			DataSet dataSet = new DataSet("Export");
			DataTable dataTable = dt.Copy();
			dataTable.TableName = "Values";
			dataSet.Tables.Add(dataTable);
			string[] array = new string[columnIndexList.Length];
			string[] array2 = new string[columnIndexList.Length];
			for (int i = 0; i < columnIndexList.Length; i++)
			{
				array[i] = dataTable.Columns[columnIndexList[i]].ColumnName;
				array2[i] = ReplaceSpecialChars(dataTable.Columns[columnIndexList[i]].ColumnName);
			}
			Export(dataSet, array, array2, exportFormat, fileName, encoding, null);
		}

		public static void Export(DataTable dt, string[] columnNameList, ExportFormat exportFormat, string fileName, Encoding encoding)
		{
			List<int> list = new List<int>();
			DataColumnCollection columns = dt.Columns;
			foreach (string columnName in columnNameList)
			{
				list.Add(GetColumnIndexByColumnName(columns, columnName));
			}
			Export(dt, list.ToArray(), exportFormat, fileName, encoding);
		}

		public static void Export(DataTable dt, int[] columnIndexList, string[] headers, ExportFormat exportFormat, string fileName, Encoding encoding)
		{
			DataSet dataSet = new DataSet("Export");
			DataTable dataTable = dt.Copy();
			dataTable.TableName = "Values";
			dataSet.Tables.Add(dataTable);
			string[] array = new string[columnIndexList.Length];
			for (int i = 0; i < columnIndexList.Length; i++)
			{
				array[i] = ReplaceSpecialChars(dataTable.Columns[columnIndexList[i]].ColumnName);
			}
			Export(dataSet, headers, array, exportFormat, fileName, encoding, null);
		}

		public static void Export(DataTable dt, int[] columnIndexList, string[] headers, ExportFormat exportFormat, string fileName, Encoding encoding, Dictionary<string, string> function)
		{
			DataSet dataSet = new DataSet("Export");
			DataTable dataTable = dt.Copy();
			dataTable.TableName = "Values";
			dataSet.Tables.Add(dataTable);
			string[] array = new string[columnIndexList.Length];
			for (int i = 0; i < columnIndexList.Length; i++)
			{
				array[i] = ReplaceSpecialChars(dataTable.Columns[columnIndexList[i]].ColumnName);
			}
			Export(dataSet, headers, array, exportFormat, fileName, encoding, function);
		}

		public static void Export(DataTable dt, string[] columnNameList, string[] headers, ExportFormat exportFormat, string fileName, Encoding encoding)
		{
			List<int> list = new List<int>();
			DataColumnCollection columns = dt.Columns;
			foreach (string columnName in columnNameList)
			{
				list.Add(GetColumnIndexByColumnName(columns, columnName));
			}
			Export(dt, list.ToArray(), headers, exportFormat, fileName, encoding);
		}

		public static void Export(DataTable dt, string[] columnNameList, string[] headers, ExportFormat exportFormat, string fileName, Encoding encoding, Dictionary<string, string> function)
		{
			List<int> list = new List<int>();
			DataColumnCollection columns = dt.Columns;
			foreach (string columnName in columnNameList)
			{
				list.Add(GetColumnIndexByColumnName(columns, columnName));
			}
			Export(dt, list.ToArray(), headers, exportFormat, fileName, encoding, function);
		}

		private static void Export(DataSet ds, string[] headers, string[] fields, ExportFormat exportFormat, string fileName, Encoding encoding, Dictionary<string, string> function)
		{
			//HttpContext.Current.Response.Clear();
			//HttpContext.Current.Response.Buffer = true;
			//HttpContext.Current.Response.ContentType = $"text/{exportFormat.ToString().ToLower()}";
			//HttpContext.Current.Response.AddHeader("content-disposition", $"attachment;filename={fileName}.{exportFormat.ToString().ToLower()}");
			//HttpContext.Current.Response.ContentEncoding = encoding;
			MemoryStream memoryStream = new MemoryStream();
			XmlTextWriter xmlTextWriter = new XmlTextWriter(memoryStream, encoding);
			CreateStylesheet(xmlTextWriter, headers, fields, exportFormat, function);
			xmlTextWriter.Flush();
			memoryStream.Seek(0L, SeekOrigin.Begin);
			//XmlDataDocument input = new XmlDataDocument(ds);
			XslCompiledTransform xslCompiledTransform = new XslCompiledTransform();
			xslCompiledTransform.Load(new XmlTextReader(memoryStream));
			StringWriter stringWriter = new StringWriter();
			//xslCompiledTransform.Transform(input, null, stringWriter);
		//	HttpContext.Current.Response.Write(stringWriter.ToString());
			stringWriter.Close();
			xmlTextWriter.Close();
			memoryStream.Close();
		//	HttpContext.Current.Response.End();
		}

		private static void CreateStylesheet(XmlTextWriter writer, string[] headers, string[] fields, ExportFormat exportFormat, Dictionary<string, string> function)
		{
			string ns = "http://www.w3.org/1999/XSL/Transform";
			writer.Formatting = Formatting.Indented;
			writer.WriteStartDocument();
			writer.WriteStartElement("xsl", "stylesheet", ns);
			writer.WriteAttributeString("version", "1.0");
			writer.WriteStartElement("xsl:output");
			writer.WriteAttributeString("method", "text");
			writer.WriteAttributeString("version", "4.0");
			writer.WriteEndElement();
			writer.WriteStartElement("xsl:template");
			writer.WriteAttributeString("match", "/");
			for (int i = 0; i < headers.Length; i++)
			{
				writer.WriteString("\"");
				writer.WriteStartElement("xsl:value-of");
				writer.WriteAttributeString("select", "'" + headers[i] + "'");
				writer.WriteEndElement();
				writer.WriteString("\"");
				if (i != fields.Length - 1)
				{
					writer.WriteString((exportFormat == ExportFormat.CSV) ? "," : "\t");
				}
			}
			writer.WriteStartElement("xsl:for-each");
			writer.WriteAttributeString("select", "Export/Values");
			writer.WriteString("\r\n");
			for (int i = 0; i < fields.Length; i++)
			{
				writer.WriteString("\"");
				writer.WriteStartElement("xsl:value-of");
				if (function != null && function.ContainsKey(fields[i]))
				{
					string value = "";
					function.TryGetValue(fields[i], out value);
					writer.WriteAttributeString("select", value);
				}
				else
				{
					writer.WriteAttributeString("select", fields[i]);
				}
				writer.WriteEndElement();
				writer.WriteString("\"");
				if (i != fields.Length - 1)
				{
					writer.WriteString((exportFormat == ExportFormat.CSV) ? "," : "\t");
				}
			}
			writer.WriteEndElement();
			writer.WriteEndElement();
			writer.WriteEndElement();
		}

		public static string ReplaceSpecialChars(string input)
		{
			input = input.Replace(" ", "_x0020_").Replace("%", "_x0025_").Replace("#", "_x0023_")
				.Replace("&", "_x0026_")
				.Replace("/", "_x002F_");
			return input;
		}

		public static int GetColumnIndexByColumnName(DataColumnCollection dcc, string columnName)
		{
			int result = -1;
			for (int i = 0; i < dcc.Count; i++)
			{
				if (dcc[i].ColumnName.ToLower() == columnName.ToLower())
				{
					result = i;
					break;
				}
			}
			return result;
		}
	}
}
