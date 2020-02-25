using System;
using System.Web;

namespace GLib.Web
{
	public class HttpSession
	{
		public static bool SetSession(string sessionKey, object value)
		{
			bool result = false;
			//if (HttpContext.Current != null && HttpContext.Current.Session != null)
			//{
			//	try
			//	{
			//		HttpContext.Current.Session[sessionKey] = value;
			//		result = true;
			//	}
			//	catch (Exception ex)
			//	{
			//		throw ex;
			//	}
			//}
			return result;
		}

		public static object GetSession(string sessionKey)
		{
			object result = null;
			//if (HttpContext.Current != null && HttpContext.Current.Session != null)
			//{
			//	try
			//	{
			//		result = HttpContext.Current.Session[sessionKey];
			//	}
			//	catch (Exception ex)
			//	{
			//		throw ex;
			//	}
			//}
			return result;
		}

		public static T GetSession<T>(string sessionKey)
		{
			T result = default(T);
			object session = GetSession(sessionKey);
			if (session != null)
			{
				if (session is T)
				{
					return (T)session;
				}
				throw new Exception($"对象不能强制转换为\"{typeof(T).ToString()}\"类型");
			}
			return result;
		}

		public static void ClearSession()
		{
			//if (HttpContext.Current != null && HttpContext.Current.Session != null)
			//{
			//	HttpContext.Current.Session.Clear();
			//}
		}

		public static string GetSessionId()
		{
			//if (HttpContext.Current != null && HttpContext.Current.Session != null)
			//{
			//	return HttpContext.Current.Session.SessionID;
			//}
			//throw new Exception("Session未实例化");
			return string.Empty;
		}
	}
}
