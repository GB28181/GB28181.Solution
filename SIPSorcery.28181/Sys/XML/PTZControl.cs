using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SIPSorcery.GB28181.Sys.XML
{
    [XmlRoot("Control")]
    public class PTZControl : XmlHelper<PTZControl>
    {
        private static PTZControl _instance;
        /// <summary>
        /// 单例模式访问
        /// </summary>
        public static PTZControl Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new PTZControl();
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

        [XmlElement("PTZCmd")]
        public string PTZCmd { get; set; }
    }
}
