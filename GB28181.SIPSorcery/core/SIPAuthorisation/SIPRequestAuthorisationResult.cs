﻿//-----------------------------------------------------------------------------
// Filename: SIPRequestAuthorisationResult.cs
//
// Description: Holds the results of a SIP request authorisation attempt.
// 
// History:
// 08 Mar 2009	Aaron Clauson	    Created.
//
// License: 
// BSD 3-Clause "New" or "Revised" License, see included LICENSE.md file.
//

using SIPSorcery.SIP;

namespace GB28181
{
    public class SIPRequestAuthorisationResult
    {
        public bool Authorised;
        public string SIPUsername;
        public string SIPDomain;
        public SIPResponseStatusCodesEnum ErrorResponse;
        public SIPAuthenticationHeader AuthenticationRequiredHeader;

        public SIPRequestAuthorisationResult(bool authorised, string sipUsername, string sipDomain)
        {
            Authorised = authorised;
            SIPUsername = sipUsername;
            SIPDomain = sipDomain;
        }

        public SIPRequestAuthorisationResult(SIPResponseStatusCodesEnum errorResponse, SIPAuthenticationHeader authenticationRequiredHeader)
        {
            ErrorResponse = errorResponse;
            AuthenticationRequiredHeader = authenticationRequiredHeader;
        }
    }
}
