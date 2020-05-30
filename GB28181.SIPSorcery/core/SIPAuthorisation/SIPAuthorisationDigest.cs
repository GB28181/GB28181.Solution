//-----------------------------------------------------------------------------
// Filename: Digest.cs
//
// Description: Implements Digest Authentication as defined in RFC2617.
//
// History:
// 08 Sep 2005	Aaron Clauson	Created.
//
// License: 
// BSD 3-Clause "New" or "Revised" License, see included LICENSE.md file.
//


using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using SIPSorcery.Sys;

#if UNITTEST
using NUnit.Framework;
#endif

namespace GB28181
{
    public enum SIPAuthorisationHeadersEnum
    {
        Unknown = 0,
        Authorize = 1,
        ProxyAuthenticate = 2,
        ProxyAuthorization = 3,
        WWWAuthenticate = 4,
    }

    public class SIPAuthorisationDigest
    {
        public const string AUTH_ALGORITHM = "MD5";

        public const string QOP_AUTHENTICATION_VALUE = "auth";

        private const int NONCE_DEFAULT_COUNT = 1;

        private static char[] m_headerFieldRemoveChars = new char[] { ' ', '"', '\'' };

        public SIPAuthorisationHeadersEnum AuthorisationType { get; private set; }              // This is the type of authorisation request received.
        public SIPAuthorisationHeadersEnum AuthorisationResponseType { get; private set; }      // If this is set it's the type of authorisation response to use otherwise use the same as the request (God knows why you need a different response header?!?)

        public string Realm { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string DestinationURL { get; set; }
        public string URI { get; set; }
        public string Nonce { get; set; }
        public string RequestType { get; set; }
        public string Response { get; set; }
        public string Algorithhm { get; set; }

        public string Cnonce { get; set; }       // Client nonce (used with WWW-Authenticate and qop=auth).
        public string Qop { get; set; }           // Quality of Protection. Values permitted are auth (authentication) and auth-int (authentication with integrity protection).
        private int NonceCount = 0;  // Client nonce count.
        public string Opaque { get; set; }

        public string Digest
        {
            get
            {
                Algorithhm = AUTH_ALGORITHM;

                // Just to make things difficult For some authorisation requests the header changes when the authenticated response is generated.
                if (AuthorisationType == SIPAuthorisationHeadersEnum.ProxyAuthenticate)
                {
                    AuthorisationResponseType = SIPAuthorisationHeadersEnum.ProxyAuthorization;
                }
                else if (AuthorisationType == SIPAuthorisationHeadersEnum.WWWAuthenticate)
                {
                    AuthorisationResponseType = SIPAuthorisationHeadersEnum.Authorize;
                }

                // If the authorisation header has specified quality of protection equal to "auth" a client nonce needs to be supplied.
                string nonceCountStr = null;
                if (Qop == QOP_AUTHENTICATION_VALUE)
                {
                    NonceCount = (NonceCount != 0) ? NonceCount : NONCE_DEFAULT_COUNT;
                    nonceCountStr = GetPaddedNonceCount(NonceCount);

                    if (Cnonce == null || Cnonce.Trim().Length == 0)
                    {
                        Cnonce = Crypto.GetRandomInt().ToString();
                    }
                }

                if (Nonce == null)
                {
                    Nonce = Crypto.GetRandomString(12);
                }

                return HTTPDigest.DigestCalcResponse(
                    AUTH_ALGORITHM,
                    Username,
                    Realm,
                    Password,
                    URI,
                    Nonce,
                    nonceCountStr,
                    Cnonce,
                    Qop,
                    RequestType,
                    null,
                    null);
            }
        }

        public SIPAuthorisationDigest()
        {
            AuthorisationType = SIPAuthorisationHeadersEnum.ProxyAuthorization;
        }

        public SIPAuthorisationDigest(SIPAuthorisationHeadersEnum authorisationType)
        {
            AuthorisationType = authorisationType;
        }

        public static SIPAuthorisationDigest ParseAuthorisationDigest(SIPAuthorisationHeadersEnum authorisationType, string authorisationRequest)
        {
            SIPAuthorisationDigest authRequest = new SIPAuthorisationDigest(authorisationType);

            string noDigestHeader = Regex.Replace(authorisationRequest, @"^\s*Digest\s*", "", RegexOptions.IgnoreCase);
            string[] headerFields = noDigestHeader.Split(',');

            if (headerFields != null && headerFields.Length > 0)
            {
                foreach (string headerField in headerFields)
                {
                    int equalsIndex = headerField.IndexOf('=');

                    if (equalsIndex != -1 && equalsIndex < headerField.Length)
                    {
                        string headerName = headerField.Substring(0, equalsIndex).Trim();
                        string headerValue = headerField.Substring(equalsIndex + 1).Trim(m_headerFieldRemoveChars);

                        if (Regex.Match(headerName, "^" + AuthHeaders.AUTH_REALM_KEY + "$", RegexOptions.IgnoreCase).Success)
                        {
                            authRequest.Realm = headerValue;
                        }
                        else if (Regex.Match(headerName, "^" + AuthHeaders.AUTH_NONCE_KEY + "$", RegexOptions.IgnoreCase).Success)
                        {
                            authRequest.Nonce = headerValue;
                        }
                        else if (Regex.Match(headerName, "^" + AuthHeaders.AUTH_USERNAME_KEY + "$", RegexOptions.IgnoreCase).Success)
                        {
                            authRequest.Username = headerValue;
                        }
                        else if (Regex.Match(headerName, "^" + AuthHeaders.AUTH_RESPONSE_KEY + "$", RegexOptions.IgnoreCase).Success)
                        {
                            authRequest.Response = headerValue;
                        }
                        else if (Regex.Match(headerName, "^" + AuthHeaders.AUTH_URI_KEY + "$", RegexOptions.IgnoreCase).Success)
                        {
                            authRequest.URI = headerValue;
                        }
                        else if (Regex.Match(headerName, "^" + AuthHeaders.AUTH_CNONCE_KEY + "$", RegexOptions.IgnoreCase).Success)
                        {
                            authRequest.Cnonce = headerValue;
                        }
                        else if (Regex.Match(headerName, "^" + AuthHeaders.AUTH_NONCECOUNT_KEY + "$", RegexOptions.IgnoreCase).Success)
                        {
                            int.TryParse(headerValue, out authRequest.NonceCount);
                        }
                        else if (Regex.Match(headerName, "^" + AuthHeaders.AUTH_QOP_KEY + "$", RegexOptions.IgnoreCase).Success)
                        {
                            authRequest.Qop = headerValue.ToLower();
                        }
                        else if (Regex.Match(headerName, "^" + AuthHeaders.AUTH_OPAQUE_KEY + "$", RegexOptions.IgnoreCase).Success)
                        {
                            authRequest.Opaque = headerValue;
                        }
                        else if (Regex.Match(headerName, "^" + AuthHeaders.AUTH_ALGORITHM_KEY + "$", RegexOptions.IgnoreCase).Success)
                        {
                            authRequest.Algorithhm = headerValue;
                        }
                    }
                }
            }

            return authRequest;
        }

        public SIPAuthorisationDigest(SIPAuthorisationHeadersEnum authorisationType, string realm, string username, string password, string uri, string nonce, string request)
        {
            AuthorisationType = authorisationType;
            Realm = realm;
            Username = username;
            Password = password;
            URI = uri;
            Nonce = nonce;
            RequestType = request;
        }

        public void SetCredentials(string username, string password, string uri, string method)
        {
            Username = username;
            Password = password;
            URI = uri;
            RequestType = method;
        }

        public override string ToString()
        {
            string authHeader = AuthHeaders.AUTH_DIGEST_KEY + " ";

            authHeader += (Username != null && Username.Trim().Length != 0) ? AuthHeaders.AUTH_USERNAME_KEY + "=\"" + Username + "\"" : null;
            authHeader += (authHeader.IndexOf('=') != -1) ? "," + AuthHeaders.AUTH_REALM_KEY + "=\"" + Realm + "\"" : AuthHeaders.AUTH_REALM_KEY + "=\"" + Realm + "\"";
            authHeader += (Nonce != null) ? "," + AuthHeaders.AUTH_NONCE_KEY + "=\"" + Nonce + "\"" : null;
            authHeader += (URI != null && URI.Trim().Length != 0) ? "," + AuthHeaders.AUTH_URI_KEY + "=\"" + URI + "\"" : null;
            authHeader += (Response != null && Response.Length != 0) ? "," + AuthHeaders.AUTH_RESPONSE_KEY + "=\"" + Response + "\"" : null;
            authHeader += (Cnonce != null) ? "," + AuthHeaders.AUTH_CNONCE_KEY + "=\"" + Cnonce + "\"" : null;
            authHeader += (NonceCount != 0) ? "," + AuthHeaders.AUTH_NONCECOUNT_KEY + "=" + GetPaddedNonceCount(NonceCount) : null;
            authHeader += (Qop != null) ? "," + AuthHeaders.AUTH_QOP_KEY + "=" + Qop : null;
            authHeader += (Opaque != null) ? "," + AuthHeaders.AUTH_OPAQUE_KEY + "=\"" + Opaque + "\"" : null;
            authHeader += (Algorithhm != null) ? "," + AuthHeaders.AUTH_ALGORITHM_KEY + "=" + Algorithhm : null;

            return authHeader;
        }

        private string GetPaddedNonceCount(int count)
        {
            return "00000000".Substring(0, 8 - NonceCount.ToString().Length) + count;
        }
    }

    public class HTTPDigest
    {
        /// <summary>
        /// Calculate H(A1) as per HTTP Digest specification.
        /// </summary>
        public static string DigestCalcHA1(
            string username,
            string realm,
            string password)
        {
            string a1 = String.Format("{0}:{1}:{2}", username, realm, password);
            return GetMD5HashBinHex(a1);
        }

        /// <summary>
        /// Calculate H(A2) as per HTTP Digest specification.
        /// </summary>
        public static string DigestCalcHA2(
            string method,
            string uri)
        {
            string A2 = String.Format("{0}:{1}", method, uri);

            return GetMD5HashBinHex(A2);
        }

        public static string DigestCalcResponse(
            string algorithm,
            string username,
            string realm,
            string password,
            string uri,
            string nonce,
            string nonceCount,
            string cnonce,
            string qop,         // qop-value: "", "auth", "auth-int".
            string method,
            string digestURL,
            string hEntity
            )
        {
            string HA1 = DigestCalcHA1(username, realm, password);
            string HA2 = DigestCalcHA2(method, uri);

            string unhashedDigest = null;
            if (nonceCount != null && cnonce != null && qop != null)
            {
                unhashedDigest = String.Format("{0}:{1}:{2}:{3}:{4}:{5}",
                HA1,
                nonce,
                nonceCount,
                cnonce,
                qop,
                HA2);
            }
            else
            {
                unhashedDigest = String.Format("{0}:{1}:{2}",
                HA1,
                nonce,
                HA2);
            }

            return GetMD5HashBinHex(unhashedDigest);
        }

        public static string GetMD5HashBinHex(string val)
        {
            using var md5 = new MD5CryptoServiceProvider();
            byte[] bHA1 = md5.ComputeHash(Encoding.UTF8.GetBytes(val));
            string HA1 = null;
            for (int i = 0; i < 16; i++)
                HA1 += String.Format("{0:x02}", bHA1[i]);
            return HA1;
        }
    }
}
