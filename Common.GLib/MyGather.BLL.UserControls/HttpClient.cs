using System;
using System.Drawing;
using System.IO;
using System.Net;
using System.Text;
using System.Web;

namespace MyGather.BLL.UserControls
{
	public class HttpClient : WebClient
	{
		private CookieContainer cookieContainer;

		public CookieContainer Cookies
		{
			get
			{
				return cookieContainer;
			}
			set
			{
				cookieContainer = value;
			}
		}

		public static HttpClient Create()
		{
			return new HttpClient();
		}

		public static HttpClient Create(CookieContainer cookieContainer)
		{
			return new HttpClient(cookieContainer);
		}

		public HttpClient()
		{
			cookieContainer = new CookieContainer();
		}

		public HttpClient(CookieContainer cookies)
		{
			cookieContainer = cookies;
		}

		protected override WebRequest GetWebRequest(Uri address)
		{
			WebRequest webRequest = base.GetWebRequest(address);
			if (webRequest is HttpWebRequest)
			{
				HttpWebRequest httpWebRequest = webRequest as HttpWebRequest;
				httpWebRequest.CookieContainer = cookieContainer;
			}
			return webRequest;
		}

		public string PostData(string uriString, string postString, string postStringEncoding, string dataEncoding, out string msg)
		{
			try
			{
				byte[] bytes = Encoding.GetEncoding(postStringEncoding).GetBytes(postString);
				base.Headers.Add("Content-Type", "application/x-www-form-urlencoded");
				byte[] bytes2 = UploadData(uriString, "POST", bytes);
				string @string = Encoding.GetEncoding(dataEncoding).GetString(bytes2);
				msg = string.Empty;
				return @string;
			}
			catch (WebException ex)
			{
				msg = ex.Message;
				return string.Empty;
			}
		}

		public string PostData(string uriString, string postString, out string msg)
		{
			return PostData(uriString, postString, "gb2312", "gb2312", out msg);
		}

		public string GetSrc(string uriString, string dataEncoding, out string msg)
		{
			try
			{
				byte[] bytes = DownloadData(uriString);
				string @string = Encoding.GetEncoding(dataEncoding).GetString(bytes);
				@string = @string.Replace("\t", "");
				@string = @string.Replace("\r", "");
				@string = @string.Replace("\n", "");
				msg = string.Empty;
				return @string;
			}
			catch (WebException ex)
			{
				msg = ex.Message;
				return string.Empty;
			}
		}

		public string GetSrc(string uriString, out string msg)
		{
			return GetSrc(uriString, "gb2312", out msg);
		}

		public Stream GetSteam(string uriString, string dataEncoding, out string msg)
		{
			try
			{
				byte[] array = DownloadData(uriString);
				MemoryStream memoryStream = new MemoryStream(array);
				Image image = new Bitmap(memoryStream);
				string @string = Encoding.GetEncoding(dataEncoding).GetString(array);
				@string = @string.Replace("\t", "");
				@string = @string.Replace("\r", "");
				@string = @string.Replace("\n", "");
				msg = string.Empty;
				return memoryStream;
			}
			catch (WebException ex)
			{
				msg = ex.Message;
				return null;
			}
		}

		public bool GetFile(string urlString, string fileName, out string msg)
		{
			try
			{
				DownloadFile(urlString, fileName);
				msg = string.Empty;
				return true;
			}
			catch (WebException ex)
			{
				msg = ex.Message;
				return false;
			}
		}

		public string UrlEncode(string url)
		{
			return HttpUtility.UrlEncode(url);
		}
	}
}
