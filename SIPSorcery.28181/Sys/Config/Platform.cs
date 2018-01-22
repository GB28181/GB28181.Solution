using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIPSorcery.GB28181.Sys.Config
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

        public SIPSorcery.GB28181.Sys.XML.DevStatus Status { get; set; }
    }
}
