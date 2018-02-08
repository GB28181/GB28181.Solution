﻿using System;
using System.Collections.Generic;
using System.Xml;

namespace SIPSorcery.GB28181.SIP.App
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
