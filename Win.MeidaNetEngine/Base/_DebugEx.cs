using System;
using System.Linq;
namespace SLW.Comm
{
    public class _DebugEx
    {
        public static string[] LogFlagSwitch = new string[] { };
        public static void Trace(string flag, string msg)
        {
            string[] filter = new string[] {  };
            if (filter.Contains(flag))
                return;
           // Console.WriteLine( string.Format("{0}___{1}", flag, msg));
            GLib.DebugEx.Trace(flag,   msg );
        }
        public static void Trace(Exception e) {
            Trace("Error", e.ToString());
        }
    }


}
