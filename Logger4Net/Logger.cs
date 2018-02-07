using System;

namespace Logger4Net
{
    public class Logger :ILog
    {
        public Logger() { }

        public void Debug(string debugMessge)
        {
            Console.WriteLine(" this debugMessge  :" + debugMessge);
        }

        public void Error(string errorMessge)
        {
            Console.WriteLine(" this errorMessge  :" + errorMessge);
        }

        public void Info(string infoMessge)
        {
            Console.WriteLine(" this infoMessge  :" + infoMessge);
        }

        public void Warn(string warnMessge)
        {
            Console.WriteLine(" this warnMessge  :" + warnMessge);
        }
    }
}
