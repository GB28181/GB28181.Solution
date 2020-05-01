using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace GB28181.Sys.XML
{
    /// <summary>
    /// 系统设备配置类型
    /// </summary>
    [XmlRoot("Query")]
    public class DeviceConfigType : XmlHelper<DeviceConfigType>
    {
        private static DeviceConfigType _instance;
        /// <summary>
        /// 单例模式访问
        /// </summary>
        public static DeviceConfigType Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new DeviceConfigType();
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
        /// 系统编码/行政区划码/设备编码/业务分组编码/虚拟组织编码
        /// </summary>
        [XmlElement("DeviceID")]
        public string DeviceID { get; set; }

        /// <summary>
        /// 配置类型参数
        /// 1，基本参数配置：BasicParam,
        /// 2，视频参数范围：VideoParamOpt
        /// 3，SVAC编码配置：SVACEncodeConfig
        /// 4，SVAC解码配置：SVACDecodeConfig
        /// </summary>
        public string ConfigType { get; set; }
    }
}
