using System;
using System.Collections.Generic;
using System.Web;
//using System.Web.Caching;

namespace GLib.Web
{

	class Cache
	{

	}


	public class HttpCache
	{
		//protected static Cache _cache = null;

		private static void InitCache()
		{
			try
			{
//				_cache = HttpRuntime.Cache;
			}
			catch (Exception ex)
			{
				throw ex;
			}
		}

		public static bool SetCache(string cacheKey, object value)
		{
			//if (_cache == null)
			//{
			//	InitCache();
			//}
			bool flag = false;
			try
			{
	//			_cache[cacheKey] = value;
				return true;
			}
			catch (Exception ex)
			{
				throw ex;
			}
		}

		public static bool SetCache(string cacheKey, object value, int hours)
		{
			TimeSpan span = new TimeSpan(hours, 0, 0);
			return SetCache(cacheKey, value, span);
		}

		public static bool SetCache(string cacheKey, object value, TimeSpan span)
		{
			//if (_cache == null)
			//{
			//	InitCache();
			//}
			bool flag = false;
			try
			{
	//			_cache.Insert(cacheKey, value, null, DateTime.MaxValue, span);
				return true;
			}
			catch (Exception ex)
			{
				throw ex;
			}
		}

		public static object GetCache(string cacheKey)
		{
			//if (_cache == null)
			//{
			//	return null;
			//}
			object obj = null;
			try
			{
	//			obj = _cache[cacheKey];
			}
			catch (Exception ex)
			{
				throw ex;
			}
			return obj;
		}

		public static T GetCache<T>(string cacheKey) where T : new()
		{
			//if (_cache == null)
			//{
			//	return default(T);
			//}
			T val = default(T);
			try
			{
	////			object obj = _cache[cacheKey];
	//			if (!(obj is T))
	//			{
	//				throw new Exception("未能将对象强制转换");
	//			}
	//			val = (T)obj;
			}
			catch (Exception ex)
			{
				throw ex;
			}
			return val;
		}
	}
}
