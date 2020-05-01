using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace GB28181.Sys.XML
{
    /// <summary>
    /// 强制关键帧命令
    /// </summary>
    [XmlRoot("Control")]
    public class KeyFrameCmd : XmlHelper<KeyFrameCmd>
    {
        private static KeyFrameCmd _instance;
        /// <summary>
        /// 单例模式访问
        /// </summary>
        public static KeyFrameCmd Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new KeyFrameCmd();
                }
                return _instance;
            }
        }

        /// <summary>
        /// 命令类型
        /// </summary>
        [XmlElement("CmdType")]
        public CommandType CommandType { get; set; }

        /// <summary>
        /// 序号
        /// </summary>
        [XmlElement("SN")]
        public int SN { get; set; }

        /// <summary>
        /// 设备编码
        /// </summary>
        [XmlElement("DeviceID")]
        public string DeviceID { get; set; }

        /// <summary>
        /// 强制关键帧
        /// </summary>
        [XmlElement("IFrameCmd")]
        public string IFrameCmd { get; set; }


        public string IFameCmd { get; set; }
    }
}
