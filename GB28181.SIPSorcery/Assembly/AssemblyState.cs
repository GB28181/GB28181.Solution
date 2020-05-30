// ============================================================================
// FileName: AssemblyState.cs
//
// Description:
//  Holds application configuration information.
//
// Author(s):
//	Aaron Clauson
//
// History:
// 22 May 2005	Aaron Clauson Created.
// 30 May 2020	Edward Chen   Updated.
// License: 
// BSD 3-Clause "New" or "Revised" License, see included LICENSE.md file.

using GB28181.Logger4Net;
using GB28181.Sys;
using System.Reflection;


[assembly: AssemblyCulture("")]

namespace GB28181
{
    public static class AssemblyState
	{

		public static readonly ILog logger = AppState.GetLogger("GB28181");
        public const string XML_DOMAINS_FILENAME = "sipdomains.xml";
        public const string XML_SIPACCOUNTS_FILENAME = "sipaccounts.xml";
        public const string XML_SIPPROVIDERS_FILENAME = "sipproviders.xml";
        public const string XML_DIALPLANS_FILENAME = "sipdialplans.xml";
        public const string XML_REGISTRAR_BINDINGS_FILENAME = "sipregistrarbindings.xml";
        public const string XML_PROVIDER_BINDINGS_FILENAME = "sipproviderbindings.xml";
        public const string XML_SIPDIALOGUES_FILENAME = "sipdialogues.xml";
        public const string XML_SIPCDRS_FILENAME = "sipcdrs.xml";
	}
}