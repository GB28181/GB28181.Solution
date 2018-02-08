using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
namespace ComHelper
{
    public class Log
    {
        //是否停止记录日志
        public static LogStatus Status = LogStatus.Started;
        /// <summary>
        /// 输出模式
        /// </summary>
        public static LogOutputMode OutputMode = LogOutputMode.Console | LogOutputMode.File;
        /// <summary>
        /// 日志输出级别
        /// </summary>
        public static LogLevel Level = LogLevel.Info | LogLevel.Warn | LogLevel.Error;
        /// <summary>
        /// 远程IP
        /// </summary>
        public static string RemoteIP = string.Empty;
        /// <summary>
        /// 远程端口
        /// </summary>
        public static int RemotePort = 5433;

        public static event Action<string> OnTrace;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        public static void Write(string message)
        {
            Write(LogLevel.Info, "system", message);
        }
        /// <summary>
        /// 写入基本信息到指定的目录
        /// </summary>
        /// <param name="directory"></param>
        /// <param name="message"></param>
        public static void Write(string directory, string message)
        {
            Write(LogLevel.Info, directory, message);
        }
        public static void Write(Exception ex, string directory, string message)
        {
            StringBuilder msgbuilder = new StringBuilder();
            try
            {
                msgbuilder.Append(message + "\r\n   ");
                msgbuilder.Append("[Exception]:" + ex.Message + "\r\n   ");
                if (ex.StackTrace != null)
                    msgbuilder.Append("[StackTrace]:" + ex.StackTrace + "\r\n   ");
                Write(LogLevel.Error, directory, msgbuilder.ToString());
            }
            catch (IOException)
            {
                //Write(LogLevel.Error, "system", "写入消息：" + message + "的时候发生了异常" + ex.Message + ":" + exception.StackTrace);
                //throw exception;
            }
            catch (Exception)
            {
                //  throw exception;
            }

        }
        /// <summary>
        /// 写入日志到指定的目录
        /// </summary>
        /// <param name="directory"></param>
        /// <param name="message"></param>
        public static void Write(LogLevel level, string directory, string message)
        {
            if (Status == LogStatus.Stoped)
                return;
            if (LogOutputMode.Console == (OutputMode & LogOutputMode.Console))
                CopyToConsole(directory, message, level);
            if (LogOutputMode.File == (OutputMode & LogOutputMode.File))
                CopyToFile(directory, message, level);
            if (LogOutputMode.Udp == (OutputMode & LogOutputMode.Udp))
                CopyToSocket(directory, message, level, RemoteIP, RemotePort);
            CopyToTrace(directory, message, level);

        }
        public static void Write(LogLevel level, bool stackTrace, string directory, string message)
        {
            if (stackTrace)
                message += PrintStackTrace(level, 2);
            Write(level, directory, message);
        }
        /// <summary>
        /// 格式化写入日志, 包含零个或多个要格式化的对象的 System.Object 数组。
        /// </summary>
        /// <param name="level">日志写入级别</param>
        /// <param name="stackTraceEnable">是否答应当前堆栈信息</param>
        /// <param name="directory">日志目录或类别</param>
        /// <param name="format">格式化字符串</param>
        /// <param name="args">参数组</param>
        public static void Write(LogLevel level, bool stackTrace, string directory, string format, params object[] args)
        {
            if ((format == null) || (args == null))
            {
                throw new ArgumentNullException((format == null) ? "format" : "args");
            }
            StringBuilder builder = new StringBuilder(format.Length + (args.Length * 8));

            builder.AppendFormat(format, args);
            if (stackTrace)
                builder.Append(PrintStackTrace(level, 2));
            Write(level, directory, builder.ToString());
        }

        private static void CopyToConsole(string directory, string message, LogLevel level)
        {
            Console.WriteLine(string.Format("[{0}]:[{1}] [{2}] {3}", DateTime.Now.TimeOfDay, level.ToString(), directory, message));
        }
        private static void CopyToTrace(string directory, string message, LogLevel level)
        {
            OnTrace?.Invoke(string.Format("[{0}]:[{1}] [{2}] {3}", DateTime.Now.TimeOfDay, level.ToString(), directory, message));
        }
        private static object objectLockWrite = new object();
        private static void CopyToFile(string directory, string message, LogLevel level)
        {
            try
            {
                string Dir = AppDomain.CurrentDomain.BaseDirectory + "\\Log\\" + directory + "\\";

                string FileName = "log_" + System.DateTime.Now.ToString("yyyy-MM-dd") + ".txt";

               var Dirinfo = new DirectoryInfo(Dir);

                if (!Dirinfo.Exists)
                    Directory.CreateDirectory(Dirinfo.ToString());

                lock (objectLockWrite)
                {
                   var writer = File.AppendText(Dir + "\\" + FileName);

                    writer.WriteLine(string.Format("[{0}][{1}]: {2}", DateTime.Now.TimeOfDay, level.ToString(), message));
                    writer.Close();
                }
            }
            catch (IOException )
            {

            }
            catch (Exception)
            {

            }
        }
        private static void CopyToSocket(string directory, string message, LogLevel level, string remoteIp, int port)
        {
            string msg = string.Format("{0}:{1} {2} {3}", DateTime.Now.TimeOfDay, level.ToString(), directory, message);
            SocketLog.Instance.SendPackage(RemoteIP, RemotePort, message);
        }
        private static string PrintStackTrace(LogLevel level, int stackLevel)
        {
            StringBuilder msgbuilder = new StringBuilder();
            try
            {
                StackFrame frame = new StackTrace(stackLevel, true).GetFrame(0);
                object obj2 = "File[" + frame.GetFileName() + "],  Function[" + frame.GetMethod().Name + "],  ";
                msgbuilder.Append(string.Concat(new object[] { obj2, "row[", frame.GetFileLineNumber(), "]" }));
                msgbuilder.Append("\t");
            }
            catch (IOException exception)
            {
                 Write(exception.Message);
            }
            return msgbuilder.ToString();

        }

        #region <<输出buffer>>
        /// <summary>
        /// 写buffer到日志
        /// </summary>
        /// <param name="directory"></param>
        /// <param name="buffer"></param>
        public static void Write(string directory, byte[] buffer)
        {
            if (buffer == null || string.IsNullOrEmpty(directory))
                return;
            Write(LogLevel.Info, directory, BytesSequenceToHexadecimalString(buffer, " "));

        }

        /// <summary>
        /// 转换字节序到字符串
        /// </summary>
        /// <param name="sequence"></param>
        /// <param name="separator"></param>
        /// <returns></returns>
        private static string BytesSequenceToHexadecimalString(byte[] sequence, string separator)
        {
            string result = string.Empty;
            foreach (byte b in sequence)
                result += string.Format(CultureInfo.InvariantCulture, "{0:x2}", b) + separator;
            return result;
        }
        #endregion

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
    public class SocketLog
    {
        private SocketLog() { }
        private Socket mSendLogSocket = null;

        private static SocketLog instance;
        //单例模式访问
        public static SocketLog Instance
        {
            get
            {
                if (instance == null)
                    instance = new SocketLog();
                return instance;
            }
        }
        private void NewSocket()
        {
            mSendLogSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            mSendLogSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout, 100);
            mSendLogSocket.Blocking = false;
        }

        public void SendPackage(string remoteIp, int remotePort, string msg)
        {
            try
            {
                if (string.IsNullOrEmpty(remoteIp))
                    return;
                if (remotePort == 0)
                    return;

                byte[] buffer = Encoding.Default.GetBytes(msg);
                int length = buffer.Length;
                IPEndPoint remoteEp = new IPEndPoint(IPAddress.Parse(remoteIp), remotePort);
                SendPacket(buffer, 0, length, remoteEp);
            }
            catch (System.Exception ex)
            {
                throw ex;
            }
        }
        private void SendPacket(byte[] packet, int offset, int count, IPEndPoint remoteEP)
        {
            try
            {
                if (mSendLogSocket == null)
                    NewSocket();
                mSendLogSocket.SendTo(packet, 0, count, SocketFlags.None, remoteEP);
            }
            catch (System.Exception ex)
            {
                throw ex;
            }
        }
    }
}

