using System.Management;
using System.Web;

namespace GLib.Utilities
{
	public class IPHelper
	{
		public static string GetServerIPAddress()
		{
			string result = "";
			ManagementClass managementClass = new ManagementClass("Win32_NetworkAdapterConfiguration");
			ManagementObjectCollection instances = managementClass.GetInstances();
			foreach (ManagementObject item in instances)
			{
				if ((bool)item["IPEnabled"])
				{
					string[] array = (string[])item["IPAddress"];
					if (array.Length > 0)
					{
						result = array[0];
					}
				}
			}
			return result;
		}

		public static string GetServerMACAddress()
		{
			string text = "";
			ManagementClass managementClass = new ManagementClass("Win32_NetworkAdapterConfiguration");
			ManagementObjectCollection instances = managementClass.GetInstances();
			foreach (ManagementObject item in instances)
			{
				if ((bool)item["IPEnabled"])
				{
					text += item["MACAddress"].ToString();
				}
			}
			return text;
		}

		public static string GetIPAddress()
		{
			//string text = (HttpContext.Current.Request.ServerVariables["HTTP_X_FORWARDED_FOR"] != null && HttpContext.Current.Request.ServerVariables["HTTP_X_FORWARDED_FOR"] != string.Empty) ? HttpContext.Current.Request.ServerVariables["HTTP_X_FORWARDED_FOR"] : HttpContext.Current.Request.ServerVariables["REMOTE_ADDR"];
			//if (string.IsNullOrEmpty(text))
			//{
			//	text = HttpContext.Current.Request.UserHostAddress;
			//}
			//return text;
			return string.Empty;
		}
	}
}
