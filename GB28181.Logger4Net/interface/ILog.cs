namespace GB28181.Logger4Net
{


    using System;

    public interface ILog
    {

        void Debug(string debugMessge);
        void Error(string errorMessge);
        void Warn(string warnMessge);
        void Info(string warnMessge);

    }


}
