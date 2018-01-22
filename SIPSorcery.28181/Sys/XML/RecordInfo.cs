using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SIPSorcery.GB28181.Sys.XML
{
    /// <summary>
    /// 录像文件查询
    /// </summary>
    [XmlRoot("Query")]
    public class RecordQuery : XmlHelper<RecordQuery>
    {
        private static RecordQuery _instance;

        /// <summary>
        /// 以单例模式访问
        /// </summary>
        public static RecordQuery Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new RecordQuery();
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
        /// 开始时间
        /// </summary>
        [XmlElement("StartTime")]
        public string StartTime { get; set; }

        /// <summary>
        /// 结束时间
        /// </summary>
        [XmlElement("EndTime")]
        public string EndTime { get; set; }

        /// <summary>
        /// 文件路径
        /// </summary>
        [XmlElement("FilePath")]
        public string FilePath { get; set; }

        /// <summary>
        /// 录像地址
        /// </summary>
        [XmlElement("Address")]
        public string Address { get; set; }

        /// <summary>
        /// 保密属性 0不涉密 1涉密
        /// </summary>
        [XmlElement("Secrecy")]
        public byte Secrecy { get; set; }

        /// <summary>
        /// 录像产生类型 time alarm manual all
        /// </summary>
        [XmlElement("Type")]
        public string Type { get; set; }

        /// <summary>
        /// 录像产生者ID
        /// </summary>
        [XmlElement("RecorderID")]
        public string RecorderID { get; set; }
    }

    /// <summary>
    /// 录像信息
    /// </summary>
    [XmlRoot("Response")]
    public class RecordInfo : XmlHelper<RecordInfo>
    {
        private static RecordInfo _instance;

        /// <summary>
        /// 以单例模式访问
        /// </summary>
        public static RecordInfo Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new RecordInfo();
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
        /// 设备名称
        /// </summary>
        [XmlElement("Name")]
        public string Name { get; set; }

        /// <summary>
        /// 录像总条数
        /// </summary>
        [XmlElement("SumNum")]
        public int SumNum { get; set; }

        /// <summary>
        /// 录像列表
        /// </summary>
        [XmlElement("RecordList")]
        public RecordList RecordItems { get; set; }

        /// <summary>
        /// 录像列表信息
        /// </summary>
        public class RecordList
        {
            private List<Item> _recordItems = new List<Item>();

            /// <summary>
            /// 设备项
            /// </summary>
            [XmlElement("Item")]
            public List<Item> Items
            {
                get
                {
                    return _recordItems;
                }
            }
        }

        public class Item
        {
            /// <summary>
            /// 设备编码
            /// </summary>
            [XmlElement("DeviceID")]
            public string DeviceID { get; set; }

            /// <summary>
            /// 录像名称
            /// </summary>
            [XmlElement("Name")]
            public string Name { get; set; }

            /// <summary>
            /// 文件路径
            /// </summary>
            [XmlElement("FilePath")]
            public string FilePath { get; set; }

            /// <summary>
            /// 录像地址
            /// </summary>
            [XmlElement("Address")]
            public string Address { get; set; }

            /// <summary>
            /// 开始时间
            /// </summary>
            [XmlElement("StartTime")]
            public string StartTime { get; set; }

            /// <summary>
            /// 结束时间
            /// </summary>
            [XmlElement("EndTime")]
            public string EndTime { get; set; }

            /// <summary>
            /// 保密属性 0不涉密 1涉密
            /// </summary>
            [XmlElement("Secrecy")]
            public byte Secrecy { get; set; }

            /// <summary>
            /// 录像产生类型 time alarm manual all
            /// </summary>
            [XmlElement("Type")]
            public string Type { get; set; }

            /// <summary>
            /// 录像产生者ID
            /// </summary>
            [XmlElement("RecorderID")]
            public string RecorderID { get; set; }
        }
    }
}
