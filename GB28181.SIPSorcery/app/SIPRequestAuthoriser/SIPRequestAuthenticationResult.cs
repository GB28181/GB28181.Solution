//-----------------------------------------------------------------------------
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


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GB28181
{
    public class SIPRequestAuthenticationResult {

        public bool Authenticated;
        public bool WasAuthenticatedByIP;
        public SIPResponseStatusCodesEnum ErrorResponse;
        public SIPAuthenticationHeader AuthenticationRequiredHeader;

        public SIPRequestAuthenticationResult(bool isAuthenticated, bool wasAuthenticatedByIP) {
            Authenticated = isAuthenticated;
            WasAuthenticatedByIP = wasAuthenticatedByIP;
        }

        public SIPRequestAuthenticationResult(SIPResponseStatusCodesEnum errorResponse, SIPAuthenticationHeader authenticationRequiredHeader) {
            Authenticated = false;
            ErrorResponse = errorResponse;
            AuthenticationRequiredHeader = authenticationRequiredHeader;
        }
    }
}
