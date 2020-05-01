using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Xml.Serialization;
using GB28181.Config;

namespace GB28181.Sys.XML
{
    /// <summary>
    /// 设备平台配置信息
    /// </summary>
    [XmlRoot("NvrTable")]
    public class NvrTable : XmlHelper<NvrTable>,IDisposable
    {
        private static NvrTable instance;
        private List<NvrItem> mVmsTables = new List<NvrItem>(500);

        /// <summary>
        /// 以单例模式访问
        /// </summary>
        public static NvrTable Instance
        {
            get
            {
                if (instance == null)
                    instance = new NvrTable();
                return instance;
            }
        }

        #region 方法
        public void Save()
        {

            base.Save(this);
        }

        public void Read()
        {
            instance = base.Read(this.GetType());

        }

        public int CreatGuid()
        {
            int Guid = 0;

            foreach (var nvrItem in this.mVmsTables)
            {
                foreach (var item in nvrItem.Items)
                {
                    if (item.Guid >= Guid)
                        Guid = item.Guid;
                }
            }
            return Guid + 1;
        }
        public int CreatNvrId()
        {
            int NvrID = 0;

            foreach (var nvrItem in this.mVmsTables)
            {
                if (nvrItem.NvrID >= NvrID)
                {
                    NvrID = nvrItem.NvrID;
                }
            }
            return NvrID + 1;
        }
        public NvrItem GetNvrItem(int id)
        {
            NvrItem item = null;
            foreach (var nvrItem in this.mVmsTables)
            {
                if (nvrItem.NvrID == id)
                {
                    item = nvrItem;
                    break;
                }
            }
            return item;
        }


        #endregion

        private int streamselect = 1;
        [XmlElement("Streamselect")]
        public int Streamselect { get { return streamselect; } set { streamselect = value; } }
        /// <summary>
        /// 
        /// </summary>
        [XmlElement("NvrItem")]
        public List<NvrItem> Items
        {
            get { return this.mVmsTables; }
        }

        public NvrItem Get(string ip)
        {
            foreach (var item in NvrTable.Instance.Items)
            {
                if (ip == item.CamIP)
                    return item;
            }
            return null;
        }


        #region 子类
        public class NvrItem
        {
            private List<ChannelItem> mChannelItem = new List<ChannelItem>(500);
            private int channelItemCount = 0;
            private string devTypeName = string.Empty;
            private string camPort = string.Empty;

            /// <summary>
            /// 设备/平台唯一标识
            /// </summary>
            [XmlAttribute()]
            public int NvrID { get; set; }
            /// <summary>
            /// 设备/平台名称
            /// </summary>
            [XmlAttribute()]
            public string NvrName { get; set; }
            /// <summary>
            /// 设备/平台ID
            /// </summary>
            [XmlAttribute()]
            public string CamID { get; set; }
            /// <summary>
            /// 设备/平台IP
            /// </summary>
            [XmlAttribute()]
            public string CamIP { get; set; }
            /// <summary>
            /// 设备/平台端口号
            /// </summary>
            [XmlAttribute()]
            public int CamPort { get; set; }
            /// <summary>
            /// 设备/平台用户名
            /// </summary>
            [XmlAttribute()]
            public string CamUser { get; set; }
            /// <summary>
            /// 设备/平台密码
            /// </summary>
            [XmlAttribute()]
            public string CamPassWord { get; set; }
            /// <summary>
            /// 设备/平台类型
            /// </summary>
            [XmlAttribute()]
            public string DevType { get; set; }
            /// <summary>
            /// onvif地址
            /// </summary>
            [XmlAttribute()]
            public string OnvifAddress { get; set; }
            /// <summary>
            /// 是/否标准化码流
            /// </summary>
            [XmlAttribute()]
            public bool IsAnalyzer { get; set; }
            /// <summary>
            /// 是/否录像
            /// </summary>
            [XmlAttribute()]
            public bool IsBackRecord { get; set; }

            /// <summary>
            /// 本地编码
            /// </summary>
            [XmlAttribute()]
            public string LocalID { get; set; }

            /// <summary>
            /// 本地IP
            /// </summary>
            [XmlAttribute()]
            public string LocalIP { get; set; }

            /// <summary>
            /// 本地端口
            /// </summary>
            [XmlAttribute()]
            public int LocalPort { get; set; }

            /// <summary>
            /// rtmp CACHE
            /// </summary>
            [XmlAttribute()]
            public int Cache { get; set; }

            /// <summary>
            /// 通道列表
            /// </summary>
            [XmlElement("ChannelItem")]
            public List<ChannelItem> Items
            {
                get { return this.mChannelItem; }
                set { mChannelItem = value; }
            }

            /// <summary>
            /// 通道数
            /// </summary>
            [XmlAttribute()]
            public int ChannelItemCount
            {
                get { return this.mChannelItem.Count; }
                set { channelItemCount = value; }
            }
            /// <summary>
            /// 设备类型名称
            /// </summary>
            [XmlAttribute()]
            public string DevTypeName
            {
                get {
                    DvsType dvsType = EnumHelper.GetDvsType(DevType);
                    return EnumHelper.GetEnumDescription<DvsType>(dvsType);
                }
                set { devTypeName = value; }
            }

            /// <summary>
            /// 国标类型
            /// </summary>
            [XmlAttribute()]
            public string GBType { get; set; }
        }

        /// <summary>
        /// 通道列表
        /// </summary>
        public class ChannelItem
        {
            public ChannelItem(int guid, string chName, string channel, int frameRate,
                string strFormat, string audio, string rtsp1, string rtsp2,
                ImageResolution main, ImageResolution child, StreamType streamtype,
                string cameraID, string cityName, bool isBackRecord, string remoteEP)
            {
                Guid = guid;
                Name = chName;
                Channel = channel;
                FrameRate = frameRate;
                StrFormat = strFormat;
                Audio = audio;
                Rtsp1 = rtsp1;
                Rtsp2 = rtsp2;
                MainResolution = main;
                SubResolution = child;
                StreamType = streamtype;
                CameraID = cameraID;
                AreaName = cityName;
                IsBackRecord = isBackRecord;
                RemoteEP = remoteEP;
			}
            public ChannelItem()
            {

            }
            /// <summary>
            /// 通道唯一标识
            /// </summary>
            [XmlAttribute()]
            public int Guid { get; set; }
            /// <summary>
            /// 通道号
            /// </summary>
            [XmlAttribute()]
            public string Channel { get; set; }
            /// <summary>
            /// 通道名称
            /// </summary>
            [XmlAttribute()]
            public string Name { get; set; }
            /// <summary>
            /// 帧率
            /// </summary>
            [XmlAttribute()]
            public int FrameRate { get; set; }
            /// <summary>
            /// 流格式
            /// </summary>
            [XmlAttribute()]
            public string StrFormat { get; set; }
            /// <summary>
            /// 音频格式
            /// </summary>
            [XmlAttribute()]
            public string Audio { get; set; }
            /// <summary>
            /// rtsp地址1(主码流)
            /// </summary>
            [XmlAttribute()]
            public string Rtsp1 { get; set; }
            /// <summary>
            /// rtsp地址2(子码流)
            /// </summary>
            [XmlAttribute()]
            public string Rtsp2 { get; set; }
            /// <summary>
            /// 主码流分辨率
            /// </summary>
            [XmlAttribute()]
            public ImageResolution MainResolution { get; set; }
            /// <summary>
            /// 子码流分辨率
            /// </summary>
            [XmlAttribute()]
            public ImageResolution SubResolution { get; set; }
            /// <summary>
            /// 码流类型
            /// </summary>
            [XmlAttribute()]
            public StreamType StreamType { get; set; }
            /// <summary>
            /// sip平台摄像机编码
            /// </summary>
            [XmlAttribute()]
            public string CameraID { get; set; }
            /// <summary>
            /// 所属区域名称
            /// </summary>
            [XmlAttribute()]
            public string AreaName { get; set; }
            /// <summary>
            /// 是/否有录像
            /// </summary>
            [XmlAttribute()]
            public bool IsBackRecord { get; set; }
            /// <summary>
            /// 远程设备终结点
            /// </summary>
            [XmlAttribute()]
            public string RemoteEP { get; set; }

			/// <summary>
			/// RTMP 直播推流Key
			/// </summary>
			[XmlAttribute()]
			public string RtmpStreamKey { get; set; }
            [XmlAttribute()]
            public int Cache { get; set; }
            /// <summary>
            /// 节点ID
            /// </summary>
            [XmlAttribute()]
            public string LocalID { get; set; }

            [XmlAttribute()]
            public string GroupID { get; set; }

            #region GB 2016
            [XmlAttribute()]
            public string DeviceID { get; set; }
            [XmlAttribute()]
            public int Status { get; set; }
            [XmlAttribute()]
            public int RecordStatus { get; set; }
            [XmlAttribute()]
            public string AudioFomate { get; set; }
            [XmlAttribute()]
            public string VideoFomate { get; set; }
            [XmlAttribute()]
            public int RealStreamType { get; set; }
            [XmlAttribute()]
            public double Longitude { get; set; }
            [XmlAttribute()]
            public double Latitude { get; set; }

            [XmlAttribute()]
            public string Adddress { get; set; }
            [XmlAttribute()]
            public int IsPTZ { get; set; }
            [XmlAttribute()]
            public string EndTime { get; set; }
            [XmlAttribute()]
            public string Manufacturer { get; set; }
            [XmlAttribute()]
            public string Model { get; set; }
            [XmlAttribute()]
            public string Owner { get; set; }
            [XmlAttribute()]
            public string CivilCode { get; set; }
            [XmlAttribute()]
            public string Block { get; set; }

            [XmlAttribute()]
            public int Parental { get; set; }
            [XmlAttribute()]
            public int AccessType { get; set; }// 0 GB国标、1 其他类型
            [XmlAttribute()]
            public string ParentID { get; set; }
            [XmlAttribute()]
            public int SafetyWay { get; set; }
            [XmlAttribute()]
            public int RegisterWay { get; set; }
            [XmlAttribute()]
            public string CertNum { get; set; }
            [XmlAttribute()]
            public int Certifiable { get; set; }
            [XmlAttribute()]
            public int ErrCode { get; set; }

            [XmlAttribute()]
            public int Secrecy { get; set; }
            [XmlAttribute()]
            public string IPAddress { get; set; }
            [XmlAttribute()]
            public ushort Port { get; set; }
            [XmlAttribute()]
            public string Password { get; set; }
            [XmlAttribute()]
            public int PTZType { get; set; }
            [XmlAttribute()]
            public int PositionType { get; set; }
            [XmlAttribute()]
            public int RoomType { get; set; }
            [XmlAttribute()]
            public int UserType { get; set; }
            [XmlAttribute()]
            public int SupplyLightType { get; set; }
            [XmlAttribute()]
            public int DirectionType { get; set; }
            [XmlAttribute()]
            public string Resolution { get; set; }
            [XmlAttribute()]
            public string BusinessGroupID { get; set; }
            [XmlAttribute()]
            public string DownloadSpeed { get; set; }
            [XmlAttribute()]
            public int SVCSpaceSupportMode { get; set; }
            [XmlAttribute()]
            public DateTime CreateTime { get; set; }
            [XmlAttribute()]
            public DateTime UpdateTime { get; set; }
            [XmlAttribute()]
            public int SVCTimeSupportMode { get; set; }

            [XmlAttribute()]
            public string SubordinatePlatform { get; set; } //所属上级/平台

            [XmlAttribute()]
            public string ExAttribute { get; set; } //自定义属性
            #endregion
        }
        #endregion

        public void Dispose()
        {
            Items.Clear();
            instance = null;
        }
    }

    public enum DvsType
    {
        [Description("大华")]
        Dahua = 1,
        [Description("汉邦")]
        HB = 3,
        [Description("黄河")]
        HH = 5,
        [Description("海康")]
        Hik = 0,
        [Description("巨峰")]
        JF = 4,
        [Description("波粒")]
        WAPA = 6,
        [Description("科达")]
        KeDa = 7,
        [Description("朗驰")]
        Launet = 2,
        [Description("屏幕编码")]
        MirScreen = 8,
        [Description("Onvif")]
        Onvif = 9,
        [Description("RTSP")]
        RTSP = 10,
        [Description("RTP-MPEG-TS")]
        RTP = 11,
        [Description("GB28181")]
        GB28181 = 12,
        [Description("宇视")]
        UniView = 13,
        [Description("GB28059")]
        GB28059 = 14,
        [Description("GB28181Platform")]
        GB28181Platform = 15,
        [Description("RtmpPull")]
        RtmpPull = 16,
        [Description("RtmpPush")]
        RtmpPush = 17,
        [Description("天地伟业")]
        Tiandy = 18,
    }

    public class EnumHelper
    {
        public static string GetDescription(object obj)
        {
            obj.ToString();
            return GetDescription(obj.GetType(), obj.ToString());
        }

        public static string GetDescription(Type type, string fieldName)
        {
            string description = string.Empty;
            foreach (FieldInfo info in type.GetFields())
            {
                if (!info.IsSpecialName && !(info.Name != fieldName))
                {
                    object[] customAttributes = info.GetCustomAttributes(typeof(DescriptionAttribute), false);
                    if (customAttributes.Length > 0)
                    {
                        description = ((DescriptionAttribute)customAttributes[0]).Description;
                    }
                }
            }
            if (description == string.Empty)
            {
                description = fieldName;
            }
            return description;
        }
        public static string GetEnumDescription<T>(T value)
        {
            System.Reflection.FieldInfo fi = value.GetType().GetField(value.ToString());

            System.ComponentModel.DescriptionAttribute[] attributes =
                (System.ComponentModel.DescriptionAttribute[])fi.GetCustomAttributes(
                    typeof(System.ComponentModel.DescriptionAttribute),
                    false
            );

            if (attributes != null &&
                attributes.Length > 0)
            {
                return attributes[0].Description;
            }
            else
            {
                return value.ToString();
            }
        }
        public static string[] GetDescriptions(Type type)
        {
            FieldInfo[] fields = type.GetFields();
            ArrayList list = new ArrayList();
            string description = string.Empty;
            foreach (FieldInfo info in fields)
            {
                if (!info.IsSpecialName)
                {
                    object[] customAttributes = info.GetCustomAttributes(typeof(DescriptionAttribute), false);
                    if (customAttributes.Length > 0)
                    {
                        description = ((DescriptionAttribute)customAttributes[0]).Description;
                        if (description == string.Empty)
                        {
                            description = customAttributes[0].ToString();
                        }
                        list.Add(description);
                    }
                }
            }
            string[] array = new string[list.Count];
            list.CopyTo(array);
            return array;
        }

        public static DvsType GetDvsType(string type)
        {
            DvsType hik = DvsType.Hik;
            if (Enum.IsDefined(typeof(DvsType), type))
            {
                hik = (DvsType)Enum.Parse(typeof(DvsType), type, true);
            }
            return hik;
        }

        public static List<EnumContext> GetEnumContextItems(Type type)
        {
            List<EnumContext> list = new List<EnumContext>();
            foreach (string str in Enum.GetNames(type))
            {
                EnumContext item = new EnumContext
                {
                    EnumItem = str,
                    Description = GetDescription(type, str)
                };
                list.Add(item);
            }
            return list;
        }


    }

    public class EnumContext
    {
        private string _description = "";
        private string _enumItem = "";

        public override string ToString()
        {
            return this._description;
        }

        public string Description
        {
            get
            {
                return this._description;
            }
            set
            {
                this._description = value;
            }
        }

        public string EnumItem
        {
            get
            {
                return this._enumItem;
            }
            set
            {
                this._enumItem = value;
            }
        }
    }
}
