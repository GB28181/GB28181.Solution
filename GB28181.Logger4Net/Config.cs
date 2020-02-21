using System;
using System.Collections.Generic;
using System.Text;

namespace GB28181.Logger4Net
{
   public class Config
    {

    }
    public enum LogStatus
    {
        Started,
        Stoped
    }

    [Serializable, Flags]
    public enum LogLevel
    {
        Info = 1,
        Warn = 2,
        Error = 4
    }
    [Serializable, Flags]
    public enum LogOutputMode
    {
        /// <summary>
        /// 文件
        /// </summary>
        File = 1,
        /// <summary>
        /// 控制台
        /// </summary>
        Console = 2,
        /// <summary>
        ///UDP
        /// </summary>
        Udp = 4
    }


}
