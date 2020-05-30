//-----------------------------------------------------------------------------
// Filename: SIPUserAgentsRolesEnum.cs
//
// Description: The roles a SIP User Agent can beahve as.
// 
// History:
// 29 Jul 2006	Aaron Clauson	Created.
//
// License: 
// BSD 3-Clause "New" or "Revised" License, see included LICENSE.md file.
//


using System;

namespace GB28181
{
	public enum SIPUserAgentRolesEnum
	{
		Client = 1,
		Server = 2,
	}

	public class SIPUserAgentRolesTypes
	{
		public static SIPUserAgentRolesEnum GetSIPUserAgentType(string userRoleType)
		{
			return (SIPUserAgentRolesEnum)Enum.Parse(typeof(SIPUserAgentRolesEnum), userRoleType, true);
		}
	}
}
