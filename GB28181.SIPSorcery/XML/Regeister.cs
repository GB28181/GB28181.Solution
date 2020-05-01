using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace GB28181.Sys.XML
{
    [XmlRoot("Notify")]
    public class Register : XmlHelper<Register>
    {
        private static Register instance;

        /// <summary>
        /// 以单例模式访问
        /// </summary>
        public static Register Instance
        {
            get
            {
                if (instance == null)
                    instance = new Register();
                return instance;
            }
        }
        /// <summary>
        /// 命令类型：设备信息查询(必选)
        /// </summary>
        [XmlElement("CmdType")]
        public CommandType CmdType { get; set; }

        [XmlElement("SN")]
        public int SN { get; set; }

        [XmlElement("DeviceID")]
        public string DeviceID { get; set; }

        [XmlElement("SumNum")]
        public int SumNum { get; set; }

        [XmlElement("DeviceList")]
        public DevList DeviceList { get; set; }


        public class DevList
        {
            private List<Items> _item = new List<Items>();
            [XmlAttribute("Num")]
            public int Num { get; set; }

            [XmlElement("Item")]
            public List<Items> Item { 
                get { return _item; }
            }

        }

        public class Items
        {
            [XmlElement("DeviceID")]
            public string DeviceID { get; set; }

            [XmlElement("Event")]
            public string Event { get; set; }
        }
    }
}
