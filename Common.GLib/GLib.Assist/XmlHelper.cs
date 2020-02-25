using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

namespace GLib.Assist
{
	public static class XmlHelper
	{
		public static string GetXPath(string xeName, string xeAttribute, string xeValue, XmlRange range)
		{
			string result = null;
			switch (range)
			{
			case XmlRange.All:
				result = ((xeAttribute == null || xeValue == null || !(xeAttribute.Trim() != "") || !(xeValue.Trim() != "")) ? ("//" + xeName) : $"//{xeName}[@{xeAttribute}=\"{xeValue}\"]");
				break;
			case XmlRange.Root:
				result = ((xeAttribute == null || xeValue == null || !(xeAttribute.Trim() != "") || !(xeValue.Trim() != "")) ? ("/" + xeName) : $"/{xeName}[@{xeAttribute}=\"{xeValue}\"]");
				break;
			case XmlRange.ThisNode:
				result = ((xeAttribute == null || xeValue == null || !(xeAttribute.Trim() != "") || !(xeValue.Trim() != "")) ? (xeName ?? "") : $"{xeName}[@{xeAttribute}=\"{xeValue}\"]");
				break;
			}
			return result;
		}

		public static XmlElement GetFirstXmlElement(string strXML, string xPath)
		{
			bool flag = false;
			XmlDocument xmlDocument = new XmlDocument();
			try
			{
				xmlDocument.LoadXml(strXML);
				flag = true;
			}
			catch (Exception)
			{
				throw new ArgumentException("Xml字符串格式错误", "strXML");
			}
			if (flag)
			{
				return GetFirstXmlElement(xmlDocument, xPath);
			}
			return null;
		}

		public static XmlElement GetFirstXmlElement(XmlElement xElement, string name, XmlRange range)
		{
			XmlDocument xmlDocument = new XmlDocument();
			xmlDocument.LoadXml(xElement.OuterXml);
			return GetFirstXmlElement(xmlDocument, name, range);
		}

		public static XmlElement GetFirstXmlElement(XmlElement xElement, string xPath)
		{
			XmlDocument xmlDocument = new XmlDocument();
			xmlDocument.LoadXml(xElement.OuterXml);
			return GetFirstXmlElement(xmlDocument, xPath);
		}

		public static XmlElement GetFirstXmlElement(XmlDocument xDoc, string name, XmlRange range)
		{
			string xPath = GetXPath(name, null, null, range);
			return GetFirstXmlElement(xDoc, xPath);
		}

		public static XmlElement GetFirstXmlElement(XmlDocument xDoc, string xPath)
		{
			XmlNode xmlNode = xDoc.DocumentElement.SelectSingleNode(xPath);
			return (XmlElement)xmlNode;
		}

		public static XmlElement GetFirstXmlElement(XmlDocument xDoc, string name, string attribute, string value, XmlRange range)
		{
			string xPath = GetXPath(name, attribute, value, range);
			return GetFirstXmlElement(xDoc, xPath);
		}

		public static XmlElement GetFirstXmlElement(XmlDocument xDoc, string name, string attribute, string value)
		{
			string xPath = GetXPath(name, attribute, value, XmlRange.ThisNode);
			return GetFirstXmlElement(xDoc, xPath);
		}

		public static XmlNodeList GetXmlNodeList(XmlDocument xDoc, string name, XmlRange range)
		{
			string xPath = GetXPath(name, null, null, range);
			return GetXmlNodeList(xDoc, xPath);
		}

		public static XmlNodeList GetXmlNodeList(XmlDocument xDoc, string name, string attribute, string value)
		{
			string xPath = GetXPath(name, attribute, value, XmlRange.ThisNode);
			return GetXmlNodeList(xDoc, xPath);
		}

		public static XmlNodeList GetXmlNodeList(XmlDocument xDoc, string name, string attribute, string value, XmlRange range)
		{
			string xPath = GetXPath(name, attribute, value, range);
			return GetXmlNodeList(xDoc, xPath);
		}

		public static XmlNodeList GetXmlNodeList(XmlDocument xDoc, string xPath)
		{
			return xDoc.DocumentElement.SelectNodes(xPath);
		}

		public static XElement GetFirstXElement(XElement xElement, string name, XmlRange range)
		{
			string xPath = GetXPath(name, null, null, range);
			return GetFirstXElement(xElement, xPath);
		}

		public static XElement GetFirstXElement(XElement xElement, string xPath)
		{
			return xElement.XPathSelectElement(xPath);
		}

		public static XElement GetFirstXElement(XElement xElement, string name, string attribute, string value, XmlRange range)
		{
			string xPath = GetXPath(name, null, null, range);
			return GetFirstXElement(xElement, xPath);
		}

		public static IEnumerable<XElement> GetXElement(XElement xElement, string name, XmlRange range)
		{
			string xPath = GetXPath(name, null, null, range);
			return GetXElement(xElement, xPath);
		}

		public static IEnumerable<XElement> GetXElement(XElement xElement, string xPath)
		{
			return xElement.XPathSelectElements(xPath);
		}

		public static IEnumerable<XElement> GetXElement(XElement xElement, string name, string attribute, string value, XmlRange range)
		{
			string xPath = GetXPath(name, null, null, range);
			return GetXElement(xElement, xPath);
		}

		public static XElement GetFirstXElement(XDocument xDoc, string name, XmlRange range)
		{
			string xPath = GetXPath(name, null, null, range);
			return xDoc.Root.XPathSelectElement(xPath);
		}

		public static XElement GetFirstXElement(XDocument xDoc, string xPath)
		{
			return xDoc.Root.XPathSelectElement(xPath);
		}

		public static XElement GetFirstXElement(XDocument xDoc, string name, string attribute, string value, XmlRange range)
		{
			string xPath = GetXPath(name, null, null, range);
			return xDoc.Root.XPathSelectElement(xPath);
		}

		public static IEnumerable<XElement> GetXElement(XDocument xDoc, string name, XmlRange range)
		{
			string xPath = GetXPath(name, null, null, range);
			return xDoc.Root.XPathSelectElements(xPath);
		}

		public static IEnumerable<XElement> GetXElement(XDocument xDoc, string xPath)
		{
			return xDoc.Root.XPathSelectElements(xPath);
		}

		public static IEnumerable<XElement> GetXElement(XDocument xDoc, string name, string attribute, string value, XmlRange range)
		{
			string xPath = GetXPath(name, null, null, range);
			return xDoc.Root.XPathSelectElements(xPath);
		}
	}
}
