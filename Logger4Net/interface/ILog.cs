namespace Logger4Net
{


    using System;

    public interface ILog
    {

        void Debug(string debugMessge, ConsoleColor color = ConsoleColor.White);
        void Error(string errorMessge);
        void Warn(string warnMessge);
        void Info(string warnMessge);

    }


}
