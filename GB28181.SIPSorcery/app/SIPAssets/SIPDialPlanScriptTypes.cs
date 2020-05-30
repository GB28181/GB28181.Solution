// ============================================================================
// FileName: SIPDialPlanScriptTypes.cs
//
// Description:
// The list of script types available for the dial plan contents.
//
// Author(s):
// Aaron Clauson
//
// History:
// 29 Sep 2008	Aaron Clauson	Created.
//
// License: 
// BSD 3-Clause "New" or "Revised" License, see included LICENSE.md file.
//


using System;
using System.Collections;
using System.Runtime.Serialization;

namespace GB28181.App
{
    [DataContractAttribute]
    public enum SIPDialPlanScriptTypesEnum
    {
        Unknown = 0,
        Asterisk = 1,
        Ruby = 2,
        Python = 3,
        JScript = 4,
        Wizard = 5,
        TelisWizard = 6,
        SimpleWizard = 7,
    }

    public class SIPDialPlanScriptTypes
    {
        public static SIPDialPlanScriptTypesEnum GetSIPDialPlanScriptType(string scriptType)
        {
            return (SIPDialPlanScriptTypesEnum)Enum.Parse(typeof(SIPDialPlanScriptTypesEnum), scriptType, true);
        }
    }
}
