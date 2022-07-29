//-----------------------------------------------------------------------------
// Filename: SIPReplacesParameter.cs
//
// Description: Represents the Replaces parameter on a Refer-To header. The Replaces parameter
// is used to identify involved in a transfer operation.
//
// History:
// 26 Sep 2011	Aaron Clauson	Created.
//
// License: 
// BSD 3-Clause "New" or "Revised" License, see included LICENSE.md file.
//


using System.Text.RegularExpressions;

namespace GB28181
{
    public class SIPReplacesParameter
    {
        public string CallID;
        public string ToTag;
        public string FromTag;

        public static SIPReplacesParameter Parse(string replaces)
        {
            var callIDMatch = Regex.Match(replaces, "^(?<callid>.*?);");
            if (replaces.IndexOf(';') != -1)
            {
                var toTagMatch = Regex.Match(replaces, "to-tag=(?<totag>.*?)(;|$)", RegexOptions.IgnoreCase);
                var fromTagMatch = Regex.Match(replaces, "from-tag=(?<fromtag>.*?)(;|$)", RegexOptions.IgnoreCase);

                if (toTagMatch.Success && fromTagMatch.Success)
                {
                    SIPReplacesParameter replacesParam = new SIPReplacesParameter
                    {
                        CallID = replaces.Substring(0, replaces.IndexOf(';')),
                        ToTag = toTagMatch.Result("${totag}"),
                        FromTag = fromTagMatch.Result("${fromtag}")
                    };

                    return replacesParam;
                }
            }

            return null;
        }
    }
}
