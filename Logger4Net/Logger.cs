using System;

namespace Logger4Net
{
    public class Logger :ILog
    {
        public Logger() { }

        public virtual void Debug(string debugMessge)
        {
            Console.WriteLine(" this debugMessge  :" + debugMessge);
        }

        public virtual void Error(string errorMessge)
        {
            Console.WriteLine(" this errorMessge  :" + errorMessge);
        }

        public virtual void Info(string infoMessge)
        {
            Console.WriteLine(" this infoMessge  :" + infoMessge);
        }

        public virtual void Warn(string warnMessge)
        {
            Console.WriteLine(" this warnMessge  :" + warnMessge);
        }
    }


    public class LoggingEvent
    {
        public string RenderedMessage { get; set; }

        public int Level { get; set; }
    }
}
