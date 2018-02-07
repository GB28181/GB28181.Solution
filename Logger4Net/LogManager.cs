using System;
using System.Collections.Generic;
using System.Text;

namespace Logger4Net
{
    public class LogManager
    {

        public static ILog GetLogger(string loggerName)
        {
            return new Logger();

        }

    }
}
