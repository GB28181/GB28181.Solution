namespace Logger4Net
{


    public interface ILog
    {

        void Debug(string debugMessge);
        void Error(string errorMessge);
        void Warn(string warnMessge);
        void Info(string warnMessge);


    }


}
