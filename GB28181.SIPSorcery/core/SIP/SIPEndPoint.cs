using SIPSorcery.SIP;
using System.Linq.Dynamic;
using System.Text.RegularExpressions;
namespace GB28181
{
    public static class SIPEndPointExtension
    {

        public static string ToHost(this SIPEndPoint sp)
        {
            return sp?.Address + ":" + sp.Port;
        }

        public static SIPURI ParseSIPURIRelaxed(string sipAddress)
        {

            if (string.IsNullOrWhiteSpace(sipAddress))
            {
                return null;
            }

            else
            {
                string regexSchemePattern = "^(" + SIPSchemesEnum.sip + "|" + SIPSchemesEnum.sips + "):";

                if (Regex.Match(sipAddress, regexSchemePattern + @"\S+").Success)
                {
                    // The partial uri is already valid.
                    return SIPURI.ParseSIPURI(sipAddress);
                }
                else
                {
                    // The partial URI is missing the scheme.
                    return SIPURI.ParseSIPURI(SIPSchemesEnum.sip.ToString() + SIPURI.SCHEME_ADDR_SEPARATOR.ToString() + sipAddress);
                }
            }
        }

        public static SIPURI TryParse(this SIPEndPoint sp, string uri)
        {
            return ParseSIPURIRelaxed(uri);

        }

    }
}
