using System;

namespace Logger4Net
{
    public class Logger : ILog
    {

        private static string format = "yyyy-MM-dd HH:mm:ss.fff";

        public Logger() { }

        private static string GetTimeString()
        {
            return DateTime.Now.ToString(format);
        }

        public virtual void Debug(string debugMessge)
        {
            Console.WriteLine( GetTimeString() + " [Debug] :" + debugMessge);
        }

        public virtual void Error(string errorMessge)
        {
            Console.WriteLine(GetTimeString() + " [Error] :" + errorMessge);
        }

        public virtual void Info(string infoMessge)
        {
            Console.WriteLine(GetTimeString() + " [Info] :" + infoMessge);
        }

        public virtual void Warn(string warnMessge)
        {
            Console.WriteLine(GetTimeString() + " [Warn] :" + warnMessge);
        }




    }


    public class LoggingEvent
    {
        public string RenderedMessage { get; set; }

        public int Level { get; set; }
    }
}
