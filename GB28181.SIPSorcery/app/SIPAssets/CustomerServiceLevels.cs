// ============================================================================
// FileName: CustomerServiceLevels.cs
//
// Description:
// The list of the different sipsorcery service levels.
//
// Author(s):
// Aaron Clauson
//
// History:
// 26 Apr 2011	Aaron Clauson	Created.
//
// License: 
// BSD 3-Clause "New" or "Revised" License, see included LICENSE.md file.
//


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GB28181.App
{
    public enum CustomerServiceLevels
    {
        None = 0,
        Beta = 1,
        Free = 2,
        Premium = 3,
        Professional = 4,
        Gold = 5,
        Silver = 6,
        Bronze = 7,
        Evaluation = 8,
        PremiumPayReqd = 9,
        ProfessionalPayReqd = 10,
        Switchboard = 11,
        SwitchboardPayReqd = 12
    }
}
