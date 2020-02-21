//-----------------------------------------------------------------------------
// Filename: RandomNumberProxy.cs
//
// Description: Calls a web service at random.org to get a random number seed.
//
// History:
// 23 Apr 2006	Aaron Clauson	Created.
//
// License:
// Aaron Clauson
//-----------------------------------------------------------------------------

using System;
using GB28181.Logger4Net;
using GB28181.SIPSorcery.Sys;

namespace Aza.Configuration
{
   // [System.Web.Services.WebServiceBindingAttribute(Name="RandomDotOrgBinding", Namespace="http://www.random.org/RandomDotOrg.wsdl")]
	public class RandomNumberSeedProxy // : System.Web.Services.Protocols.SoapHttpClientProtocol 
	{	
		private static ILog logger = AppState.logger;
		
        public string Url { get; set; }

        /// <remarks/>
        public RandomNumberSeedProxy() 
		{
			this.Url = "http://www.random.org/cgi-bin/Random.cgi";
		}

		//[System.Web.Services.Protocols.SoapRpcMethodAttribute("", RequestNamespace="RandomDotOrg", ResponseNamespace="RandomDotOrg")]
		[return: System.Xml.Serialization.SoapElementAttribute("return")]
		public int Lrand48() 
		{
			object[] results = this.Invoke("lrand48", new object[0]);
			return ((int)(results[0]));
		}

        private object[] Invoke(string v1, object[] v2)
        {
            throw new NotImplementedException();
        }

        //[System.Web.Services.Protocols.SoapRpcMethodAttribute("", RequestNamespace="RandomDotOrg", ResponseNamespace="RandomDotOrg")]
        [return: System.Xml.Serialization.SoapElementAttribute("return")]
		public long Mrand48() 
		{
			object[] results = this.Invoke("mrand48", new object[0]);
			return ((long)(results[0]));
		}
	}
}
