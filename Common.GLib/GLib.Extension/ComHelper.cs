using System;
using System.Data;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
//using System.Web.Caching;
//using System.Web.UI;
//using System.Web.UI.WebControls;

namespace GLib.Extension
{
	public class ComHelper
	{
		private static byte[] Keys = new byte[8]
		{
			16,
			48,
			80,
			115,
			151,
			171,
			205,
			239
		};

		public static string AddSucces => "添加成功";

		public static string AddSame => "已经存在！";

		public static string AddError => "添加出错，请重试！";

		public static string DeleteSucces => "删除成功";

		public static string DeleteSame => "已经存在！";

		public static string DeleteError => "删除出错，请重试！";

		public static string UpdateSucces => "修改成功";

		public static string UpdateSame => "已经存在！";

		public static string UpdateError => "修改出错，请重试！";

		public static string StrConvert(string strInput)
		{
			if (strInput != null && strInput != "")
			{
				string[,] array = new string[13, 2]
				{
					{
						"'",
						"’"
					},
					{
						"%20",
						" "
					},
					{
						"%24",
						" "
					},
					{
						"%27",
						" "
					},
					{
						"%3a",
						" "
					},
					{
						"%3b",
						" "
					},
					{
						"%3c",
						" "
					},
					{
						";",
						"；"
					},
					{
						":",
						"："
					},
					{
						"%",
						"％"
					},
					{
						"--",
						"－－"
					},
					{
						"*",
						"*"
					},
					{
						"\\",
						"、、"
					}
				};
				for (int i = 0; i < array.Length / 2; i++)
				{
					strInput = strInput.Replace(array[i, 0], array[i, 1]);
				}
			}
			return strInput;
		}

		public static string EncryptDES(string encryptString, string encryptKey)
		{
			try
			{
				byte[] bytes = Encoding.UTF8.GetBytes(encryptKey.Substring(0, 8));
				byte[] keys = Keys;
				byte[] bytes2 = Encoding.UTF8.GetBytes(encryptString);
				DESCryptoServiceProvider dESCryptoServiceProvider = new DESCryptoServiceProvider();
				MemoryStream memoryStream = new MemoryStream();
				CryptoStream cryptoStream = new CryptoStream(memoryStream, dESCryptoServiceProvider.CreateEncryptor(bytes, keys), CryptoStreamMode.Write);
				cryptoStream.Write(bytes2, 0, bytes2.Length);
				cryptoStream.FlushFinalBlock();
				return Convert.ToBase64String(memoryStream.ToArray());
			}
			catch
			{
				return encryptString;
			}
		}

		public static string DecryptDES(string decryptString, string decryptKey)
		{
			try
			{
				byte[] bytes = Encoding.UTF8.GetBytes(decryptKey);
				byte[] keys = Keys;
				byte[] array = Convert.FromBase64String(decryptString);
				DESCryptoServiceProvider dESCryptoServiceProvider = new DESCryptoServiceProvider();
				MemoryStream memoryStream = new MemoryStream();
				CryptoStream cryptoStream = new CryptoStream(memoryStream, dESCryptoServiceProvider.CreateDecryptor(bytes, keys), CryptoStreamMode.Write);
				cryptoStream.Write(array, 0, array.Length);
				cryptoStream.FlushFinalBlock();
				return Encoding.UTF8.GetString(memoryStream.ToArray());
			}
			catch
			{
				return decryptString;
			}
		}

/*
		public static void RunScript(Page p, string sScript)
		{
			ClientScriptManager clientScript = p.ClientScript;
			string script = "<script type=\"text/javascript\"> " + sScript + " </script>";
			clientScript.RegisterStartupScript(p.GetType(), "alertMessage", script);
		}

		public static void Alert(Page p, string message)
		{
			ClientScriptManager clientScript = p.ClientScript;
			string script = "<script type=\"text/javascript\"> alert(\"" + message + "\");</script>";
			clientScript.RegisterClientScriptBlock(p.GetType(), "alertMessage", script);
		}

		public static void Alert(Page p, string message, string derectPage)
		{
			ClientScriptManager clientScript = p.ClientScript;
			string script = "<script type=\"text/javascript\"> alert(\"" + message + "\");location.href=\"" + derectPage + "\";</script>";
			clientScript.RegisterClientScriptBlock(p.GetType(), "alertMessage", script);
		}

		public static void Confirm(Page p, string message, string derectPage)
		{
			ClientScriptManager clientScript = p.ClientScript;
			string script = "<script type=\"text/javascript\"> if( confirm(\"" + message + "\")) location.href=\"" + derectPage + "\";</script>";
			clientScript.RegisterClientScriptBlock(p.GetType(), "alertMessage", script);
		}

		public static void Confirm(string message, string url)
		{
			ClientScriptManager clientScript = ((Page)HttpContext.Current.Handler).ClientScript;
			string script = "<script type=\"text/javascript\"> if( confirm(\"" + message + "\")) location.href=\"" + url + "\";</script>";
			clientScript.RegisterClientScriptBlock(((Page)HttpContext.Current.Handler).GetType(), "alertMessage", script);
		}

		public static void Go(string url)
		{
			ClientScriptManager clientScript = ((Page)HttpContext.Current.Handler).ClientScript;
			string script = "<script type=\"text/javascript\"> window.location=\"" + url + "\";</script>";
			clientScript.RegisterClientScriptBlock(((Page)HttpContext.Current.Handler).GetType(), "go", script);
		}

		public static void GoParent(string url)
		{
			ClientScriptManager clientScript = ((Page)HttpContext.Current.Handler).ClientScript;
			string script = "<script type=\"text/javascript\"> window.parent.location=\"" + url + "\";</script>";
			clientScript.RegisterClientScriptBlock(((Page)HttpContext.Current.Handler).GetType(), "go", script);
		}

		public static void GoParent(Page p, string url)
		{
			ClientScriptManager clientScript = p.ClientScript;
			string script = "<script type=\"text/javascript\"> if(parent.frames.length>0) window.parent.location.href=\"" + url + "\";</script>";
			clientScript.RegisterClientScriptBlock(p.GetType(), "go", script);
		}

		public static void Back(Page p, int nFlag)
		{
			ClientScriptManager clientScript = p.ClientScript;
			string script = "<script type=\"text/javascript\"> window.history.back(" + nFlag + ");</script>";
			clientScript.RegisterClientScriptBlock(p.GetType(), "back", script);
		}
*/
		public static bool HasData(DataSet ds)
		{
			bool result = false;
			if (ds != null)
			{
				int count = ds.Tables.Count;
				if (count > 0)
				{
					int count2 = ds.Tables[0].Rows.Count;
					if (count2 > 0)
					{
						result = true;
					}
				}
			}
			return result;
		}

		/*
		public static List<CheckBox> GetCheckedAllControls(string checkedBoxId, GridView gv)
		{
			List<CheckBox> list = new List<CheckBox>();
			for (int i = 0; i < gv.Rows.Count; i++)
			{
				CheckBox checkBox = (CheckBox)gv.Rows[i].FindControl(checkedBoxId);
				if (checkBox != null && checkBox.Checked)
				{
					list.Add(checkBox);
				}
			}
			return list;
		}

		public static List<CheckBox> GetGetCheckedAllControlsAll(string checkedBoxId, Repeater gv)
		{
			List<CheckBox> list = new List<CheckBox>();
			for (int i = 0; i < gv.Items.Count; i++)
			{
				CheckBox checkBox = (CheckBox)gv.Items[i].FindControl(checkedBoxId);
				if (checkBox != null)
				{
					list.Add(checkBox);
				}
			}
			return list;
		}

		public static List<CheckBox> GetCheckedAllControls(string checkedBoxId, Repeater gv)
		{
			List<CheckBox> list = new List<CheckBox>();
			for (int i = 0; i < gv.Items.Count; i++)
			{
				CheckBox checkBox = (CheckBox)gv.Items[i].FindControl(checkedBoxId);
				if (checkBox != null && checkBox.Checked)
				{
					list.Add(checkBox);
				}
			}
			return list;
		}

		public static void GetTreeViewCheckNodeAllList(TreeView treeview, List<TreeNode> getList)
		{
			foreach (TreeNode node in treeview.Nodes)
			{
				getList.Add(node);
				if (node.ChildNodes.Count > 0)
				{
					GetTreeViewNodeAll(node, getList);
				}
			}
		}

		private static void GetTreeViewNodeAll(TreeNode ParentNode, List<TreeNode> getList)
		{
			foreach (TreeNode childNode in ParentNode.ChildNodes)
			{
				getList.Add(childNode);
				if (childNode.ChildNodes.Count > 0)
				{
					GetTreeViewNodeAll(childNode, getList);
				}
			}
		}

		public static void GetTreeViewCheckNodeList(TreeView treeview, List<TreeNode> getList)
		{
			foreach (TreeNode node in treeview.Nodes)
			{
				if (node.Checked)
				{
					getList.Add(node);
				}
				if (node.ChildNodes.Count > 0)
				{
					GetTreeViewNode(node, getList);
				}
			}
		}

		private static void GetTreeViewNode(TreeNode ParentNode, List<TreeNode> getList)
		{
			foreach (TreeNode childNode in ParentNode.ChildNodes)
			{
				if (childNode.Checked)
				{
					getList.Add(childNode);
				}
				if (childNode.ChildNodes.Count > 0)
				{
					GetTreeViewNode(childNode, getList);
				}
			}
		}

		public static void UpdateMessage(ObjectDataSourceStatusEventArgs e, Label lblMessage)
		{
			if (e.Exception != null)
			{
				e.ExceptionHandled = true;
				return;
			}
			int num = (int)e.ReturnValue;
			if (num > 0)
			{
				lblMessage.Text = "编辑成功！";
			}
			else if (num == 0)
			{
				lblMessage.Text = "请重试！";
			}
		}

		public static void UpdateAlertMessage(ObjectDataSourceStatusEventArgs e, Page page)
		{
			if (e.Exception != null)
			{
				e.ExceptionHandled = true;
				return;
			}
			int num = (int)e.ReturnValue;
			if (num > 0)
			{
				Alert(page, "编辑成功！");
			}
			else if (num == 0)
			{
				Alert(page, "请重试！");
			}
		}

		public static void DeleteMessage(ObjectDataSourceStatusEventArgs e, Label lblMessage)
		{
			if (e.Exception != null)
			{
				e.ExceptionHandled = true;
				return;
			}
			int num = (int)e.ReturnValue;
			if (num > 0)
			{
				lblMessage.Text = "删除成功！";
			}
			else if (num == 0)
			{
				lblMessage.Text = "请重试！";
			}
		}

		public static List<ListItem> GetListControl(ListControl control)
		{
			List<ListItem> list = new List<ListItem>();
			for (int i = 0; i < control.Items.Count; i++)
			{
				if (control.Items[i].Selected)
				{
					list.Add(control.Items[i]);
				}
			}
			return list;
		}

		public static DataSet GetXMLDataSet(string key, string xml)
		{
			object obj = HttpContext.Current.Cache[key];
			if (obj == null)
			{
				DataSet dataSet = new DataSet();
				dataSet.ReadXml(xml);
				HttpContext.Current.Cache.Insert(key, dataSet, new CacheDependency(xml));
				return dataSet;
			}
			return (DataSet)obj;
		}

		*/
		public static string Post(string Web, string postData)
		{
			string text = "";
			postData = postData.Replace(" ", "%20");
			try
			{
				HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(Web);
				Stream stream = new MemoryStream();
				StreamWriter streamWriter = new StreamWriter(stream, Encoding.Default);
				streamWriter.Write(postData);
				streamWriter.Flush();
				long length = stream.Length;
				streamWriter.Close();
				httpWebRequest.ContentType = "application/x-www-form-urlencoded";
				httpWebRequest.ContentLength = length;
				httpWebRequest.Method = "POST";
				Stream requestStream = httpWebRequest.GetRequestStream();
				streamWriter = new StreamWriter(requestStream, Encoding.Default);
				streamWriter.Write(postData);
				streamWriter.Close();
				HttpWebResponse httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();
				Stream responseStream = httpWebResponse.GetResponseStream();
				Encoding @default = Encoding.Default;
				StreamReader streamReader = new StreamReader(responseStream, @default);
				text = streamReader.ReadToEnd();
				streamReader.Close();
				httpWebResponse.Close();
				return text;
			}
			catch (Exception ex)
			{
				throw ex;
			}
		}

		private static string UrlEncode(string url, Encoding enc)
		{
			byte[] bytes = enc.GetBytes(url);
			StringBuilder stringBuilder = new StringBuilder();
			for (int i = 0; i < bytes.Length; i++)
			{
				if (bytes[i] < 128)
				{
					stringBuilder.Append((char)bytes[i]);
					continue;
				}
				stringBuilder.Append("%" + bytes[i++].ToString("x").PadLeft(2, '0'));
				stringBuilder.Append("%" + bytes[i].ToString("x").PadLeft(2, '0'));
			}
			return stringBuilder.ToString();
		}

		public static string Get(string url, Encoding enc)
		{
			string requestUriString = UrlEncode(url, enc);
			HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(requestUriString);
			HttpWebResponse httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();
			Stream responseStream = httpWebResponse.GetResponseStream();
			StreamReader streamReader = new StreamReader(responseStream, enc);
			string result = streamReader.ReadToEnd();
			streamReader.Close();
			httpWebResponse.Close();
			return result;
		}

		public static string Get(string url)
		{
			Encoding encoding = Encoding.GetEncoding("gb2312");
			return Get(UrlEncode(url, encoding), encoding);
		}
/*
		public static string RenderUserControl(string UserControlName)
		{
			StringBuilder stringBuilder = new StringBuilder();
			StringWriter writer = new StringWriter(stringBuilder);
			HtmlTextWriter writer2 = new HtmlTextWriter(writer);
			UserControl userControl = new UserControl();
			Control control = userControl.LoadControl(UserControlName);
			control.RenderControl(writer2);
			return stringBuilder.ToString();
		}

		public static void SetListControl(ListControl control, object oItemValue, int flag)
		{
			string b = string.Concat(oItemValue);
			if (flag >= 0)
			{
				for (int i = 0; i < control.Items.Count; i++)
				{
					if (control.Items[i].Value == b)
					{
						control.Items[i].Selected = true;
					}
					else
					{
						control.Items[i].Selected = false;
					}
				}
				return;
			}
			for (int i = 0; i < control.Items.Count; i++)
			{
				if (control.Items[i].Text == b)
				{
					control.Items[i].Selected = true;
				}
				else
				{
					control.Items[i].Selected = false;
				}
			}
		}

		public static void SetListControlFuzz(ListControl control, object oItemValue)
		{
			string text = $"\uff3e{string.Concat(oItemValue)}\uff3e";
			int num = 0;
			while (true)
			{
				if (num < control.Items.Count)
				{
					if ($"\uff3e{control.Items[num].Value}\uff3e".IndexOf(text.ToString()) == 0)
					{
						break;
					}
					control.Items[num].Selected = false;
					num++;
					continue;
				}
				return;
			}
			control.Items[num].Selected = true;
		}
*/
		public static bool IsInt(string str)
		{
			Regex regex = new Regex("^[-]?\\d+$");
			return regex.IsMatch(str);
		}

		public static bool IsNumeric(string str)
		{
			str.Trim();
			Regex regex = new Regex("^[-]?\\d+[.]?\\d*$");
			return regex.IsMatch(str);
		}

		public static bool IsDateTime(string strValue)
		{
			if (null == strValue)
			{
				return false;
			}
			strValue = strValue.Trim();
			string pattern = "[1-2]{1}[0-9]{3}((-|\\/){1}(([0]?[1-9]{1})|(1[0-2]{1}))((-|\\/){1}((([0]?[1-9]{1})|([1-2]{1}[0-9]{1})|(3[0-1]{1})))( (([0-1]{1}[0-9]{1})|2[0-3]{1}):([0-5]{1}[0-9]{1}):([0-5]{1}[0-9]{1})(\\.[0-9]{3})?)?)?)?$";
			if (Regex.IsMatch(strValue, pattern))
			{
				int num = -1;
				int num2 = -1;
				int num3 = -1;
				if (-1 != (num = strValue.IndexOf("-")))
				{
					num2 = strValue.IndexOf("-", num + 1);
					num3 = strValue.IndexOf(":");
				}
				else
				{
					num = strValue.IndexOf("/");
					num2 = strValue.IndexOf("/", num + 1);
					num3 = strValue.IndexOf(":");
				}
				if (-1 == num2)
				{
					return true;
				}
				if (-1 == num3)
				{
					num3 = strValue.Length + 3;
				}
				int num4 = Convert.ToInt32(strValue.Substring(0, num));
				int num5 = Convert.ToInt32(strValue.Substring(num + 1, num2 - num - 1));
				int num6 = Convert.ToInt32(strValue.Substring(num2 + 1, num3 - num2 - 4));
				if ((num5 < 8 && 1 == num5 % 2) || (num5 > 8 && 0 == num5 % 2))
				{
					if (num6 < 32)
					{
						return true;
					}
				}
				else if (num5 != 2)
				{
					if (num6 < 31)
					{
						return true;
					}
				}
				else if (num4 % 400 == 0 || (num4 % 4 == 0 && 0 < num4 % 100))
				{
					if (num6 < 30)
					{
						return true;
					}
				}
				else if (num6 < 29)
				{
					return true;
				}
			}
			return false;
		}

		public static string DisplayBoolean(int boolFlag)
		{
			string result = string.Empty;
			switch (boolFlag)
			{
			case 0:
				result = "是";
				break;
			case 1:
				result = "否";
				break;
			}
			return result;
		}

		public static string GetExtendName(string fileName)
		{
			string text = "";
			int num = fileName.LastIndexOf('.') + 1;
			text = fileName.Substring(num, fileName.Length - num);
			return text.ToLower();
		}
/*
		public static void DownloadFileByFilePath(Page WebForm, string FileNameWhenUserDownload, string FilePath)
		{
			WebForm.Response.ContentType = "application/x-zip-compressed";
			WebForm.Response.AddHeader("Content-Disposition", "attachment;filename=" + FileNameWhenUserDownload);
			WebForm.Response.TransmitFile(FilePath);
		}

		public static string GetClientIP()
		{
			string text = HttpContext.Current.Request.ServerVariables["HTTP_X_FORWARDED_FOR"];
			if (text == null || text == string.Empty)
			{
				text = HttpContext.Current.Request.ServerVariables["REMOTE_ADDR"];
			}
			if (text == null || text == string.Empty)
			{
				text = HttpContext.Current.Request.UserHostAddress;
			}
			return text;
		}
*/
	}
}
