using System.Text.RegularExpressions;

namespace GB28181.SIPSorcery.Sys.Net
{

    public class IPSocket
    {

        public static bool IsIPSocket(string socket)
        {
            if (socket == null || socket.Trim().Length == 0)
            {
                return false;
            }
            else
            {

                return Regex.Match(socket, @"^\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}(:\d{1,5})$", RegexOptions.Compiled).Success;

            }
        }

        public static bool IsIPAddress(string socket)
        {
            if (socket == null || socket.Trim().Length == 0)
            {
                return false;
            }
            else
            {

                return Regex.Match(socket, @"^\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}$", RegexOptions.Compiled).Success;
            }
        }

       
    }


}
