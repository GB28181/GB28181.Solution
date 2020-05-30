using System;
using System.Collections.Generic;
using System.Xml;

namespace GB28181.App
{
    public interface ISIPAsset
    {
        Guid Id { get; set; }

        void Load(System.Data.DataRow row);
        Dictionary<Guid, object> Load(XmlDocument dom);
        System.Data.DataTable GetTable();
        string ToXML();
        string ToXMLNoParent();
        string GetXMLElementName();
        string GetXMLDocumentElementName();
    }
}
