﻿using System;
using System.Xml.Linq;
using SIPSorcery.SIP;

namespace GB28181
{
    public class SIPEventPresenceTuple
    {
        private const string m_pidfXMLNS = SIPEventConsts.PIDF_XML_NAMESPACE_URN;

        public string ID { get; set; }
        public SIPEventPresenceStateEnum Status { get; set; }
        public SIPURI ContactURI { get; set; }
        public decimal ContactPriority { get; set; } = decimal.Zero;
        public string AvatarURL { get; set; }

        private SIPEventPresenceTuple()
        { }

        public SIPEventPresenceTuple(string id, SIPEventPresenceStateEnum status)
        {
            ID = id;
            Status = status;
        }

        public SIPEventPresenceTuple(string id, SIPEventPresenceStateEnum status, SIPURI contactURI, decimal contactPriority, string avatarURL)
        {
            ID = id;
            Status = status;
            ContactURI = contactURI;
            ContactPriority = contactPriority;
            AvatarURL = avatarURL;
        }

        public static SIPEventPresenceTuple Parse(string tupleXMLStr)
        {
            XElement tupleElement = XElement.Parse(tupleXMLStr);
            return Parse(tupleElement);
        }

        public static SIPEventPresenceTuple Parse(XElement tupleElement)
        {
            XNamespace ns = m_pidfXMLNS;

            SIPEventPresenceTuple tuple = new SIPEventPresenceTuple
            {
                ID = tupleElement.Attribute("id").Value,
                Status = (SIPEventPresenceStateEnum)Enum.Parse(typeof(SIPEventPresenceStateEnum), tupleElement.Element(ns + "status").Element(ns + "basic").Value, true),
                ContactURI = (tupleElement.Element(ns + "contact") != null) ? SIPURI.ParseSIPURI(tupleElement.Element(ns + "contact").Value) : null
            };
            tuple.ContactPriority = (tuple.ContactURI != null && tupleElement.Element(ns + "contact").Attribute("priority") != null) ? Decimal.Parse(tupleElement.Element(ns + "contact").Attribute("priority").Value) : Decimal.Zero;
            tuple.AvatarURL = (tuple.ContactURI != null && tupleElement.Element(ns + "contact").Attribute("avatarurl") != null) ? tupleElement.Element(ns + "contact").Attribute("avatarurl").Value : null;

            return tuple;
        }

        public XElement ToXML()
        {
            XNamespace ns = m_pidfXMLNS;

            XElement tupleElement = new XElement(ns + "tuple",
                new XAttribute("id", ID),
                new XElement(ns + "status",
                    new XElement(ns + "basic", Status.ToString()))
                );

            if (ContactURI != null)
            {
                XElement contactElement = new XElement(ns + "contact", ContactURI.ToString());
                if (ContactPriority != Decimal.Zero)
                {
                    contactElement.Add(new XAttribute("priority", ContactPriority.ToString("0.###")));
                }
                if (AvatarURL != null)
                {
                    contactElement.Add(new XAttribute("avatarurl", AvatarURL));
                }
                tupleElement.Add(contactElement);
            }

            return tupleElement;
        }
    }
}
