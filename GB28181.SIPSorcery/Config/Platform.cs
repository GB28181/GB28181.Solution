
using GB28181.Sys.XML;

namespace GB28181.Config
{
    /// <summary>
    /// 平台配置
    /// </summary>
    public class PlatformConfig
    {
        /// <summary>
        /// 通道名称
        /// </summary>
        public string ChannelName { get; set; }

        /// <summary>
        /// 远程IP
        /// </summary>
        public string RemoteIP { get; set; }

        /// <summary>
        /// 远程端口
        /// </summary>
        public int RemotePort { get; set; }

        public DevStatus Status { get; set; }
    }
}
