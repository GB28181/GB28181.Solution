namespace GB28181.Sys.Config
{
    ///// <summary>
    ///// 设备列表
    ///// </summary>
    //public class NvrTable
    //{
    //    private static readonly string m_storageTypeKey = SIPSorceryConfiguration.PERSISTENCE_STORAGETYPE_KEY;
    //    private static readonly string m_connStrKey = SIPSorceryConfiguration.PERSISTENCE_STORAGECONNSTR_KEY;
    //    private static readonly string m_XMLFilename = string.Empty;

    //    private static StorageTypes m_storageType;
    //    private static string m_connStr;

    //    private static NvrTable _instance;

    //    /// <summary>
    //    /// 以单例模式访问
    //    /// </summary>
    //    public static NvrTable Instance
    //    {
    //        get
    //        {
    //            if (_instance == null)
    //            {
    //                _instance = new NvrTable();
    //            }
    //            return _instance;
    //        }
    //    }

    //    /// <summary>
    //    /// 初始化设备列表信息
    //    /// </summary>
    //    static NvrTable()
    //    {
    //        string path = AppDomain.CurrentDomain.BaseDirectory + "Config\\";
    //        m_storageType = (AppState.GetConfigSetting(m_storageTypeKey) != null) ? StorageTypesConverter.GetStorageType(AppState.GetConfigSetting(m_storageTypeKey)) : StorageTypes.Unknown;
    //        m_connStr = AppState.GetConfigSetting(m_connStrKey);
    //        if (m_storageType == StorageTypes.SQLite)
    //        {
    //            m_connStr = string.Format(m_connStr, path);

    //        }
    //        if (m_storageType == StorageTypes.Unknown || m_connStr.IsNullOrBlank())
    //        {
    //            throw new ApplicationException("The SIP Registrar cannot start with no persistence settings.");
    //        }
    //    }

    //    private int _streamselect;
    //    /// <summary>
    //    /// 码流类型
    //    /// </summary>
    //    public int Streamselect
    //    {
    //        get { return _streamselect; }
    //        set { _streamselect = 1; }
    //    }

    //    private List<NvrItem> _nvrItems = new List<NvrItem>();

    //    public SIPAssetPersistor<NvrItem> NvrItems
    //    {
    //        get;
    //        set;
    //    }

    //    public SIPAssetPersistor<ChannelItem> ChannelItems
    //    {
    //        get;
    //        set;
    //    }

    //    /// <summary>
    //    /// 设备列表
    //    /// </summary>
    //    public List<NvrItem> Items
    //    {
    //        get { return _nvrItems; }
    //        set { _nvrItems = value; }
    //    }

    //    /// <summary>
    //    /// 获取设备列表项
    //    /// </summary>
    //    /// <param name="ip"></param>
    //    /// <returns></returns>
    //    public NvrItem Get(string ip)
    //    {
    //        foreach (var item in Items)
    //        {
    //            if (ip == item.CamIP)
    //                return item;
    //        }
    //        return null;
    //    }

    //    /// <summary>
    //    /// 创建设备ID
    //    /// </summary>
    //    /// <returns></returns>
    //    public int CreatGuid()
    //    {
    //        int Guid = 0;

    //        foreach (var nvrItem in _nvrItems)
    //        {
    //            foreach (var item in nvrItem.Items)
    //            {
    //                if (item.Guid >= Guid)
    //                    Guid = item.Guid;
    //            }
    //        }
    //        return Guid + 1;
    //    }

    //    /// <summary>
    //    /// 创建设备id
    //    /// </summary>
    //    /// <returns></returns>
    //    public int CreatNvrId()
    //    {
    //        int NvrID = 0;

    //        foreach (var nvrItem in _nvrItems)
    //        {
    //            if (nvrItem.NvrID >= NvrID)
    //            {
    //                NvrID = nvrItem.NvrID;
    //            }
    //        }
    //        return NvrID + 1;
    //    }

    //    /// <summary>
    //    /// 加载设备信息
    //    /// </summary>
    //    public void Read()
    //    {
    //        SIPAssetPersistor<NvrItem> nvrItem = SIPAssetPersistorFactory<NvrItem>.CreateSIPAssetPersistor(m_storageType, m_connStr, m_XMLFilename);
    //        SIPAssetPersistor<ChannelItem> nvrChannel = SIPAssetPersistorFactory<ChannelItem>.CreateSIPAssetPersistor(m_storageType, m_connStr, m_XMLFilename);
    //        NvrItems = nvrItem;
    //        ChannelItems = nvrChannel;
    //        _nvrItems = nvrItem.Get();
    //        var nvrChannelItems = nvrChannel.Get();
    //        foreach (var item in _nvrItems)
    //        {
    //            foreach (var channel in nvrChannelItems)
    //            {
    //                if (item.NvrID == channel.NvrID)
    //                {
    //                    item.Add(channel);
    //                }
    //            }
    //        }
    //    }

    //    /// <summary>
    //    /// 保存设备信息
    //    /// </summary>
    //    public void Save()
    //    {

    //    }

    //    /// <summary>
    //    /// 设备列表信息
    //    /// </summary>
    //    // [Table(Name = "NvrItem")]
    //    [DataContractAttribute]
    //    public class NvrItem : INotifyPropertyChanged, ISIPAsset
    //    {
    //        private List<ChannelItem> _channelItems = new List<ChannelItem>();

    //        public List<ChannelItem> Items
    //        {
    //            get { return _channelItems; }
    //        }

    //        public void Add(ChannelItem channel)
    //        {
    //            _channelItems.Add(channel);
    //        }

    //        private Guid _id;
    //        // [Column(Name = "Id", DbType = "varchar(36)", IsPrimaryKey = true, CanBeNull = false, UpdateCheck = UpdateCheck.Never)]
    //        public Guid Id
    //        {
    //            get
    //            {
    //                return _id;
    //            }
    //            set
    //            {
    //                _id = value;
    //            }
    //        }

    //        private int _nvrID;
    //        /// <summary>
    //        /// 唯一标识
    //        /// </summary>
    //        // [Column(Name = "NvrID", DbType = "int", CanBeNull = false, UpdateCheck = UpdateCheck.Never)]
    //        public int NvrID
    //        {
    //            get { return _nvrID; }
    //            set { _nvrID = value; }
    //        }
    //        private string _nvrName;
    //        /// <summary>
    //        /// 设备/平台名称
    //        /// </summary>
    //        // [Column(Name = "NvrName", DbType = "varchar(100)", CanBeNull = false, UpdateCheck = UpdateCheck.Never)]
    //        public string NvrName
    //        {
    //            get { return _nvrName; }
    //            set { _nvrName = value; }
    //        }

    //        private string _camID;
    //        /// <summary>
    //        /// 设备/平台id
    //        /// </summary>
    //        // [Column(Name = "CamID", DbType = "varchar(30)", CanBeNull = true, UpdateCheck = UpdateCheck.Never)]
    //        public string CamID
    //        {
    //            get { return _camID; }
    //            set { _camID = value; }
    //        }


    //        private string _campIP;
    //        /// <summary>
    //        /// 设备/平台ip
    //        /// </summary>
    //        // [Column(Name = "CamIP", DbType = "varchar(30)", CanBeNull = true, UpdateCheck = UpdateCheck.Never)]
    //        public string CamIP
    //        {
    //            get { return _campIP; }
    //            set { _campIP = value; }
    //        }
    //        private int _camPort;

    //        /// <summary>
    //        /// 设备/平台端口
    //        /// </summary>
    //        // [Column(Name = "CamPort", DbType = "int", CanBeNull = true, UpdateCheck = UpdateCheck.Never)]
    //        public int CamPort
    //        {
    //            get { return _camPort; }
    //            set { _camPort = value; }
    //        }
    //        private string _camUser;
    //        /// <summary>
    //        /// 设备/平台用户
    //        /// </summary>
    //        // [Column(Name = "CamUser", DbType = "varchar(30)", CanBeNull = true, UpdateCheck = UpdateCheck.Never)]
    //        public string CamUser
    //        {
    //            get { return _camUser; }
    //            set { _camUser = value; }
    //        }
    //        private string _camPassword;

    //        /// <summary>
    //        /// 设备/平台密码
    //        /// </summary>
    //        // [Column(Name = "CamPassword", DbType = "varchar(30)", CanBeNull = true, UpdateCheck = UpdateCheck.Never)]
    //        public string CamPassword
    //        {
    //            get { return _camPassword; }
    //            set { _camPassword = value; }
    //        }
    //        private string _devType;
    //        /// <summary>
    //        /// 设备/平台类型
    //        /// </summary>
    //        // [Column(Name = "DevType", DbType = "varchar(30)", CanBeNull = false, UpdateCheck = UpdateCheck.Never)]
    //        public string DevType
    //        {
    //            get { return _devType; }
    //            set { _devType = value; }
    //        }
    //        private string _onvifAddress;
    //        /// <summary>
    //        /// onvif地址
    //        /// </summary>
    //        // [Column(Name = "OnvifAddress", DbType = "varchar(200)", CanBeNull = true, UpdateCheck = UpdateCheck.Never)]
    //        public string OnvifAddress
    //        {
    //            get { return _onvifAddress; }
    //            set { _onvifAddress = value; }
    //        }
    //        private bool _isAnalyzer;
    //        /// <summary>
    //        /// 标准化码流
    //        /// </summary>
    //        // [Column(Name = "IsAnalyzer", DbType = "int", CanBeNull = false, UpdateCheck = UpdateCheck.Never)]
    //        public bool IsAnalyzer
    //        {
    //            get { return _isAnalyzer; }
    //            set { _isAnalyzer = value; }
    //        }
    //        private int _isBackRecord;
    //        /// <summary>
    //        /// 是/否有录像
    //        /// </summary>
    //        // [Column(Name = "IsBackRecord", DbType = "int", CanBeNull = false, UpdateCheck = UpdateCheck.Never)]
    //        public int IsBackRecord
    //        {
    //            get { return _isBackRecord; }
    //            set { _isBackRecord = value; }
    //        }
    //        private string _localID;
    //        /// <summary>
    //        /// 本地平台Id
    //        /// </summary>
    //        // [Column(Name = "LocalID", DbType = "varchar(30)", CanBeNull = true, UpdateCheck = UpdateCheck.Never)]
    //        public string LocalID
    //        {
    //            get { return _localID; }
    //            set { _localID = value; }
    //        }
    //        private string _localIP;
    //        /// <summary>
    //        /// 本地平台ip
    //        /// </summary>
    //        // [Column(Name = "LocalIP", DbType = "varchar(30)", CanBeNull = true, UpdateCheck = UpdateCheck.Never)]
    //        public string LocalIP
    //        {
    //            get { return _localIP; }
    //            set { _localIP = value; }
    //        }
    //        private int _localPort;
    //        /// <summary>
    //        /// 本地平台端口
    //        /// </summary>
    //        // [Column(Name = "LocalPort", DbType = "int", CanBeNull = true, UpdateCheck = UpdateCheck.Never)]
    //        public int LocalPort
    //        {
    //            get { return _localPort; }
    //            set { _localPort = value; }
    //        }

    //        public event PropertyChangedEventHandler PropertyChanged;



    //        public void Load(System.Data.DataRow row)
    //        {
    //            _id = row["Id"] != null ? new Guid(row["Id"].ToString()) : System.Guid.NewGuid();
    //            _nvrID = row["NvrID"] != null ? Convert.ToInt32(row["NvrID"]) : 0;
    //            _nvrName = row["NvrName"] as string;
    //            _camID = row["CamID"] as string;
    //            _campIP = row["CamIP"] as string;
    //            _camPort = row["CamPort"] != null ? Convert.ToInt32(row["CamPort"]) : 0;
    //            _camUser = row["CamUser"] as string;
    //            _camPassword = row["CamPassword"] as string;
    //            _devType = row["DevType"] as string;
    //            _onvifAddress = row["OnvifAddress"] as string;
    //            _isAnalyzer = row["IsAnalyzer"].ToString() == "1" ? true : false;
    //            _isBackRecord = row["IsBackRecord"] != null ? Convert.ToInt32(row["IsBackRecord"]) : 0;
    //            _localID = row["LocalID"] as string;
    //            _localIP = row["LocalIP"] as string;
    //            _localPort = row["LocalPort"] != null ? Convert.ToInt32(row["LocalPort"]) : 0;
    //        }

    //        public System.Data.DataTable GetTable()
    //        {
    //            throw new NotImplementedException();
    //        }

    //        public string ToXML()
    //        {
    //            throw new NotImplementedException();
    //        }

    //        public string ToXMLNoParent()
    //        {
    //            throw new NotImplementedException();
    //        }

    //        public string GetXMLElementName()
    //        {
    //            throw new NotImplementedException();
    //        }

    //        public string GetXMLDocumentElementName()
    //        {
    //            throw new NotImplementedException();
    //        }


    //        public Dictionary<Guid, object> Load(System.Xml.XmlDocument dom)
    //        {
    //            throw new NotImplementedException();
    //        }
    //    }

    //    /// <summary>
    //    /// 通道信息
    //    /// </summary>
    //    // [Table(Name = "NvrChannel")]
    //    [DataContractAttribute]
    //    public class ChannelItem : INotifyPropertyChanged, ISIPAsset
    //    {
    //        private Guid _id;
    //        // [Column(Name = "Id", DbType = "varchar(36)", IsPrimaryKey = true, CanBeNull = false, UpdateCheck = UpdateCheck.Never)]
    //        public Guid Id
    //        {
    //            get
    //            {
    //                return _id;
    //            }
    //            set
    //            {
    //                _id = value;
    //            }
    //        }

    //        private int _guid;
    //        /// <summary>
    //        /// 通道id
    //        /// </summary>
    //        // [Column(Name = "Guid", DbType = "int",  CanBeNull = false, UpdateCheck = UpdateCheck.Never)]
    //        public int Guid
    //        {
    //            get { return _guid; }
    //            set { _guid = value; }
    //        }
    //        private int _nvrID;
    //        /// <summary>
    //        /// 设备/平台id
    //        /// </summary>
    //        // [Column(Name = "NvrID", DbType = "int", CanBeNull = false, UpdateCheck = UpdateCheck.Never)]
    //        public int NvrID
    //        {
    //            get { return _nvrID; }
    //            set { _nvrID = value; }
    //        }
    //        private int _channelID;
    //        /// <summary>
    //        /// 通道编码
    //        /// </summary>
    //        // [Column(Name = "ChannelID", DbType = "int", CanBeNull = false, UpdateCheck = UpdateCheck.Never)]
    //        public int Channel
    //        {
    //            get { return _channelID; }
    //            set { _channelID = value; }
    //        }
    //        private string _channelName;
    //        /// <summary>
    //        /// 通道名称
    //        /// </summary>
    //        // [Column(Name = "ChannelName", DbType = "varchar(200)", CanBeNull = false, UpdateCheck = UpdateCheck.Never)]
    //        public string Name
    //        {
    //            get { return _channelName; }
    //            set { _channelName = value; }
    //        }
    //        private int _frameRate;
    //        /// <summary>
    //        /// 帧率
    //        /// </summary>
    //        // [Column(Name = "FrameRate", DbType = "int", CanBeNull = true, UpdateCheck = UpdateCheck.Never)]
    //        public int FrameRate
    //        {
    //            get { return _frameRate; }
    //            set { _frameRate = value; }
    //        }
    //        private string _streamFormat;
    //        /// <summary>
    //        /// 码流格式
    //        /// </summary>
    //        // [Column(Name = "StreamFormat", DbType = "varchar(20)", CanBeNull = true, UpdateCheck = UpdateCheck.Never)]
    //        public string StreamFormat
    //        {
    //            get { return _streamFormat; }
    //            set { _streamFormat = value; }
    //        }
    //        private string _audioFormat;
    //        /// <summary>
    //        /// 音频格式
    //        /// </summary>
    //        // [Column(Name = "AudioFormat", DbType = "varchar(20)", CanBeNull = true, UpdateCheck = UpdateCheck.Never)]
    //        public string AudioFormat
    //        {
    //            get { return _audioFormat; }
    //            set { _audioFormat = value; }
    //        }
    //        private string _rtsp1;
    //        /// <summary>
    //        /// rtsp地址1
    //        /// </summary>
    //        // [Column(Name = "Rtsp1", DbType = "varchar(200)", CanBeNull = true, UpdateCheck = UpdateCheck.Never)]
    //        public string Rtsp1
    //        {
    //            get { return _rtsp1; }
    //            set { _rtsp1 = value; }
    //        }
    //        private string _rtsp2;
    //        /// <summary>
    //        /// rtsp地址2
    //        /// </summary>
    //        // [Column(Name = "Rtsp2", DbType = "varchar(200)", CanBeNull = true, UpdateCheck = UpdateCheck.Never)]
    //        public string Rtsp2
    //        {
    //            get { return _rtsp2; }
    //            set { _rtsp2 = value; }
    //        }
    //        private ImageResolution _mainResolution;
    //        /// <summary>
    //        /// 主码流分辨率
    //        /// </summary>
    //        // [Column(Name = "MainResolution", DbType = "varchar(20)", CanBeNull = true, UpdateCheck = UpdateCheck.Never)]
    //        public ImageResolution MainResolution
    //        {
    //            get { return _mainResolution; }
    //            set { _mainResolution = value; }
    //        }
    //        private ImageResolution _subResolution;
    //        /// <summary>
    //        /// 子码流分辨率
    //        /// </summary>
    //        // [Column(Name = "SubResolution", DbType = "varchar(20)", CanBeNull = true, UpdateCheck = UpdateCheck.Never)]
    //        public ImageResolution SubResolution
    //        {
    //            get { return _subResolution; }
    //            set { _subResolution = value; }
    //        }
    //        private StreamType _streamType;
    //        /// <summary>
    //        /// 码流类型
    //        /// </summary>
    //        // [Column(Name = "StreamType", DbType = "varchar(20)", CanBeNull = true, UpdateCheck = UpdateCheck.Never)]
    //        public StreamType StreamType
    //        {
    //            get { return _streamType; }
    //            set { _streamType = value; }
    //        }
    //        private string _cameraID;
    //        /// <summary>
    //        /// 摄像机id
    //        /// </summary>
    //        // [Column(Name = "CameraID", DbType = "varchar(30)", CanBeNull = true, UpdateCheck = UpdateCheck.Never)]
    //        public string CameraID
    //        {
    //            get { return _cameraID; }
    //            set { _cameraID = value; }
    //        }
    //        private string _areaName;
    //        /// <summary>
    //        /// 所属区域名称
    //        /// </summary>
    //        // [Column(Name = "AreaName", DbType = "varchar(100)", CanBeNull = true, UpdateCheck = UpdateCheck.Never)]
    //        public string AreaName
    //        {
    //            get { return _areaName; }
    //            set { _areaName = value; }
    //        }
    //        private byte _isBackRecord;
    //        /// <summary>
    //        /// 是/否有录像
    //        /// </summary>
    //        // [Column(Name = "IsBackRecord", DbType = "int", CanBeNull = true, UpdateCheck = UpdateCheck.Never)]
    //        public byte IsBackRecord
    //        {
    //            get { return _isBackRecord; }
    //            set { _isBackRecord = value; }
    //        }

    //        public event PropertyChangedEventHandler PropertyChanged;



    //        public void Load(System.Data.DataRow row)
    //        {
    //            ImageResolution mainResolution = ImageResolution.R_Undefined;
    //            Enum.TryParse<ImageResolution>(row["MainResolution"].ToString(), out mainResolution);
    //            ImageResolution subResolution = ImageResolution.R_Undefined;
    //            Enum.TryParse<ImageResolution>(row["SubResolution"].ToString(), out subResolution);
    //            StreamType strType = Config.StreamType.mainStream;
    //            Enum.TryParse<StreamType>(row["StreamType"].ToString(), out strType);

    //            _id = row["Id"] != null ? new Guid(row["Id"].ToString()) : System.Guid.NewGuid();
    //            _guid = row["Guid"] != null ? Convert.ToInt32(row["Guid"]) : 0;
    //            _nvrID = row["NvrID"] != null ? Convert.ToInt32(row["NvrID"]) : 0;
    //            _channelID = row["ChannelID"] != null ? Convert.ToInt32(row["ChannelID"]) : 0;
    //            _channelName = row["ChannelName"] as string;
    //            _frameRate = row["FrameRate"] != null ? Convert.ToInt32(row["FrameRate"]) : 0;
    //            _streamFormat = row["StreamFormat"] as string;
    //            _audioFormat = row["AudioFormat"] as string;
    //            _rtsp1 = row["Rtsp1"] as string;
    //            _rtsp2 = row["Rtsp2"] as string;
    //            _mainResolution = mainResolution;
    //            _subResolution = subResolution;
    //            _streamType = strType;
    //            _cameraID = row["CameraID"] as string;
    //            _areaName = row["AreaName"] as string;
    //            _isBackRecord = row["IsBackRecord"] != null ? byte.Parse(row["IsBackRecord"].ToString()) : (byte)0;

    //        }

    //        public System.Data.DataTable GetTable()
    //        {
    //            throw new NotImplementedException();
    //        }

    //        public string ToXML()
    //        {
    //            throw new NotImplementedException();
    //        }

    //        public string ToXMLNoParent()
    //        {
    //            throw new NotImplementedException();
    //        }

    //        public string GetXMLElementName()
    //        {
    //            throw new NotImplementedException();
    //        }

    //        public string GetXMLDocumentElementName()
    //        {
    //            throw new NotImplementedException();
    //        }


    //        public Dictionary<Guid, object> Load(System.Xml.XmlDocument dom)
    //        {
    //            throw new NotImplementedException();
    //        }
    //    }
    //}
}
