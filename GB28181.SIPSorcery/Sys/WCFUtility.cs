//-----------------------------------------------------------------------------
// Filename: WCFUtility.cs
//
// Description: Class to provide utility mehtods for working with WCF. 
// 
// History:
// 23 Feb 2010	Aaron Clauson	Created.
//


using System.Configuration;

namespace GB28181.Sys
{
    public class WCFUtility
    {
        public static bool DoesWCFServiceExist(string serviceName)
        {
            //ServiceModelSectionGroup serviceModelSectionGroup = ServiceModelSectionGroup.GetSectionGroup(ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None));
            //foreach (ServiceElement serviceElement in serviceModelSectionGroup.Services.Services)
            //{
            //    if (serviceElement.Name == serviceName)
            //    {
            //        return true;
            //    }
            //}

            return false;
        }
    }
}
