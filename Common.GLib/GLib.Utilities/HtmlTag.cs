using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace GLib.Utilities
{
	public class HtmlTag
	{
		private string m_Name;

		private string m_BeginTag;

		private string m_InnerHTML;

		private Hashtable m_Attributes = new Hashtable();

		private static Regex attrReg = new Regex("([a-zA-Z1-9_-]+)\\s*=\\s*(\\x27|\\x22)([^\\x27\\x22]*)(\\x27|\\x22)", RegexOptions.IgnoreCase);

		public string TagName => m_Name;

		public string InnerHTML => m_InnerHTML;

		private HtmlTag(string name, string beginTag, string innerHTML)
		{
			m_Name = name;
			m_BeginTag = beginTag;
			m_InnerHTML = innerHTML;
			MatchCollection matchCollection = attrReg.Matches(beginTag);
			foreach (Match item in matchCollection)
			{
				m_Attributes[item.Groups[1].Value.ToUpper()] = item.Groups[3].Value;
			}
		}

		public List<HtmlTag> FindTag(string name)
		{
			return FindTag(m_InnerHTML, name, $"<{name}(\\s[^<>]*|)>");
		}

		public List<HtmlTag> FindTag(string name, string format)
		{
			return FindTag(m_InnerHTML, name, format);
		}

		public List<HtmlTag> FindTagByAttr(string tagName, string attrName, string attrValue)
		{
			return FindTagByAttr(m_InnerHTML, tagName, attrName, attrValue);
		}

		public List<HtmlTag> FindNoEndTag(string name)
		{
			return FindNoEndTag(m_InnerHTML, name, $"<{name}(\\s[^<>]*|)>");
		}

		public List<HtmlTag> FindNoEndTag(string name, string format)
		{
			return FindNoEndTag(m_InnerHTML, name, format);
		}

		public List<HtmlTag> FindNoEndTagByAttr(string tagName, string attrName, string attrValue)
		{
			return FindNoEndTagByAttr(m_InnerHTML, tagName, attrName, attrValue);
		}

		public string GetAttribute(string name)
		{
			return m_Attributes[name.ToUpper()] as string;
		}

		public static string GetHtml(string url)
		{
			try
			{
				HttpWebRequest httpWebRequest = WebRequest.Create(url) as HttpWebRequest;
				httpWebRequest.Timeout = 30000;
				HttpWebResponse httpWebResponse = httpWebRequest.GetResponse() as HttpWebResponse;
				Stream responseStream = httpWebResponse.GetResponseStream();
				MemoryStream memoryStream = new MemoryStream();
				byte[] buffer = new byte[4096];
				int num = 0;
				while ((num = responseStream.Read(buffer, 0, 4096)) > 0)
				{
					memoryStream.Write(buffer, 0, num);
				}
				return Encoding.GetEncoding(httpWebResponse.CharacterSet).GetString(memoryStream.GetBuffer());
			}
			catch
			{
				return string.Empty;
			}
		}

		public static List<HtmlTag> FindTagByAttr(string html, string tagName, string attrName, string attrValue)
		{
			string format = $"<{tagName}\\s[^<>]*{attrName}\\s*=\\s*(\\x27|\\x22){attrValue}(\\x27|\\x22)[^<>]*>";
			return FindTag(html, tagName, format);
		}

		public static List<HtmlTag> FindTag(string html, string name, string format)
		{
			Regex regex = new Regex(format, RegexOptions.IgnoreCase);
			Regex regex2 = new Regex($"<(\\/|)({name})(\\s[^<>]*|)>", RegexOptions.IgnoreCase);
			List<HtmlTag> list = new List<HtmlTag>();
			int startat = 0;
			while (true)
			{
				bool flag = true;
				Match match = regex.Match(html, startat);
				if (!match.Success)
				{
					break;
				}
				startat = match.Index + match.Length;
				Match match2 = null;
				int num = 1;
				do
				{
					flag = true;
					match2 = regex2.Match(html, startat);
					if (!match2.Success)
					{
						match2 = null;
						break;
					}
					startat = match2.Index + match2.Length;
					num = ((!(match2.Groups[1].Value == "/")) ? (num + 1) : (num - 1));
				}
				while (num != 0);
				if (match2 != null)
				{
					HtmlTag item = new HtmlTag(name, match.Value, html.Substring(match.Index + match.Length, match2.Index - match.Index - match.Length));
					list.Add(item);
					continue;
				}
				break;
			}
			return list;
		}

		public static List<HtmlTag> FindNoEndTagByAttr(string html, string tagName, string attrName, string attrValue)
		{
			string format = $"<{tagName}\\s[^<>]*{attrName}\\s*=\\s*(\\x27|\\x22){attrValue}(\\x27|\\x22)[^<>]*>";
			return FindNoEndTag(html, tagName, format);
		}

		public static List<HtmlTag> FindNoEndTag(string html, string name, string format)
		{
			Regex regex = new Regex(format, RegexOptions.IgnoreCase);
			Regex regex2 = new Regex($"<({name})(\\s[^<>]*|)(\\/)+>", RegexOptions.IgnoreCase);
			List<HtmlTag> list = new List<HtmlTag>();
			int startat = 0;
			while (true)
			{
				bool flag = true;
				Match match = regex.Match(html, startat);
				if (match.Success)
				{
					startat = match.Index + match.Length;
					Match match2 = match;
					if (match2 != null)
					{
						HtmlTag item = new HtmlTag(name, match.Value, string.Empty);
						list.Add(item);
						continue;
					}
					break;
				}
				break;
			}
			return list;
		}
	}
}
