
using System.Collections.Generic;

namespace GB28181.Logger4Net
{
    public class LogManager
    {

        private static readonly Dictionary<string, ILog> _loggerMapper = new Dictionary<string, ILog>();

        public static ILog GetLogger(string loggerName)
        {
            if (!_loggerMapper.ContainsKey(loggerName))
            {
                _loggerMapper.Add(loggerName, new Logger());
            }
            return _loggerMapper[loggerName];

        }

    }
}
