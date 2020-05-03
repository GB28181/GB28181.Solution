using System.Xml;
using GB28181.Logger4Net;
using SIPSorcery.Sys;

namespace GB28181.Sys.Auth
{
    public class SIPSorcerySecurityHeader 
    {
        private const string SECURITY_NAMESPACE = "http://www.sipsorcery.com/security";
        private const string SECURITY_HEADER_NAME = "Security";
        private const string SECURITY_PREFIX = "sssec";
        private const string AUTHID_ELEMENT_NAME = "AuthID";
        private const string APIKEY_ELEMENT_NAME = "apikey";

        private static ILog logger = AppState.logger;

        public string AuthID;
        public string APIKey;

        public  bool MustUnderstand { get { return true; } }
        public  string Name { get { return SECURITY_HEADER_NAME; } }
        public  string Namespace { get { return SECURITY_NAMESPACE; } }

        public SIPSorcerySecurityHeader(string authID, string apiKey)
        {
            AuthID = authID;
            APIKey = apiKey;
        }

        protected  void OnWriteHeaderContents(XmlDictionaryWriter writer, string messageVersion)
        {
            if (!AuthID.IsNullOrBlank())
            {
                writer.WriteStartElement(SECURITY_PREFIX, AUTHID_ELEMENT_NAME, SECURITY_NAMESPACE);
                writer.WriteString(AuthID);
                writer.WriteEndElement();
            }

            if (!APIKey.IsNullOrBlank())
            {
                writer.WriteStartElement(SECURITY_PREFIX, APIKEY_ELEMENT_NAME, SECURITY_NAMESPACE);
                writer.WriteString(AuthID);
                writer.WriteEndElement();
            }
        }

        protected  void OnWriteStartHeader(XmlDictionaryWriter writer, string messageVersion)
        {
            writer.WriteStartElement(SECURITY_PREFIX, this.Name, this.Namespace);
        }

        public static SIPSorcerySecurityHeader ParseHeader(/*OperationContext context*/)
        {
            //try
            //{
            //    int headerIndex = context.IncomingMessageHeaders.FindHeader(SECURITY_HEADER_NAME, SECURITY_NAMESPACE);
            //    if (headerIndex != -1)
            //    {
            //        XmlDictionaryReader reader = context.IncomingMessageHeaders.GetReaderAtHeader(headerIndex);

            //        if (reader.IsStartElement(SECURITY_HEADER_NAME, SECURITY_NAMESPACE))
            //        {
            //            reader.ReadStartElement();
            //            reader.MoveToContent();

            //            if (reader.IsStartElement(AUTHID_ELEMENT_NAME, SECURITY_NAMESPACE))
            //            {
            //                string authID = reader.ReadElementContentAsString();
            //                return new SIPSorcerySecurityHeader(authID, null);
            //            }

            //            if (reader.IsStartElement(APIKEY_ELEMENT_NAME, SECURITY_NAMESPACE))
            //            {
            //                string apiKey = reader.ReadElementContentAsString();
            //                return new SIPSorcerySecurityHeader(null, apiKey);
            //            }
            //        }
            //    }
            //     return null;
            //}
            //catch (Exception excp)
            //{
            //    logger.Error("Exception SIPSorcerySecurityHeader ParseHeader. " + excp.Message);
            //    throw;
            //}
            return null;
        }
    }
}
