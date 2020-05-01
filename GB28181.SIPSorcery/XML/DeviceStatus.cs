using System.Xml.Serialization;

namespace GB28181.Sys.XML
{
    /// <summary>
    /// 设备状态
    /// </summary>
    [XmlRoot("Response")]
    public class DeviceStatus : XmlHelper<DeviceStatus>
    {
        private static DeviceStatus _instance;

        /// <summary>
        /// 以单例模式访问
        /// </summary>
        public static DeviceStatus Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new DeviceStatus();
                }
                return _instance;
            }
        }

        /// <summary>
        /// 指令类型
        /// </summary>
        [XmlElement("CmdType")]
        public CommandType CmdType { get; set; }

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
        /// 查询结果标志
        /// </summary>
        [XmlElement("Result")]
        public string Result { get; set; }
        /// <summary>
        /// 是否在线
        /// </summary>
        [XmlElement("Online")]
        public string Online { get; set; }
        /// <summary>
        /// 状态
        /// </summary>
        [XmlElement("Status")]
        public string Status { get; set; }
        /// <summary>
        /// 不正常工作原因
        /// </summary>
        [XmlElement("Reason")]
        public string Reason { get; set; }
    }
}
