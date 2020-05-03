using GB28181.Sys.XML;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace GB28181.Sys.XML
{
     [XmlRoot("Control")]
    public class Control : XmlHelper<Control>
    {
         private static Control _instance;

         /// <summary>
         /// 单例模式访问
         /// </summary>
         public static Control Instance
         {
             get
             {
                 if (_instance == null)
                 {
                     _instance = new Control();
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

         [XmlElement("GuardCmd")]
         public string GuardCmd { get; set; }

         [XmlElement("TeleBoot")]
         public string TeleBoot { get; set; }

         [XmlElement("AlarmCmd")]
         public string AlarmCmd { get; set; }
        [XmlElement("RecordCmd")]
        public string RecordCmd { get; set; }

    }
}
