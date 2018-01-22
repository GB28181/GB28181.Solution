using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Linq.Mapping;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace SIPSorcery.GB28181.SIP.App
{
    /// <summary>
    /// 通道信息
    /// </summary>
    [Table(Name = "CameraInfo")]
    [DataContractAttribute]
    public class SvrCamera : INotifyPropertyChanged, ISIPAsset
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private Guid _id;
        /// <summary>
        /// 主键标识
        /// </summary>
        public Guid Id
        {
            get
            {
                return _id;
            }
            set
            {
                _id = value;
            }
        }

        private int _channelGuid;
        /// <summary>
        /// 通道唯一标识
        /// </summary>
        public int ChannelGuid
        {
            get { return _channelGuid; }
            set { _channelGuid = value; }
        }


        private string _channelID;
        /// <summary>
        /// 通道编号
        /// </summary>
        public string ChannelID
        {
            get { return _channelID; }
            set { _channelID = value; }
        }

        private string _gbID;
        /// <summary>
        /// 国标ID
        /// </summary>
        public string GBID
        {
            get { return _gbID; }
            set { _gbID = value; }
        }

        private string _name;
        /// <summary>
        /// 通道名称
        /// </summary>
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }
        private string _deviceID;
        /// <summary>
        /// 设备ID
        /// </summary>
        public string DeviceID
        {
            get { return _deviceID; }
            set { _deviceID = value; }
        }
        private int _videoStatus;
        /// <summary>
        /// 视频状态
        /// </summary>
        public int VideoStatus
        {
            get { return _videoStatus; }
            set { _videoStatus = value; }
        }
        private int _recordStatus;
        /// <summary>
        /// 录像状态
        /// </summary>
        public int RecordStatus
        {
            get { return _recordStatus; }
            set { _recordStatus = value; }
        }
        private string _audioFormat;
        /// <summary>
        /// 音频格式
        /// </summary>
        public string AudioFormat
        {
            get { return _audioFormat; }
            set { _audioFormat = value; }
        }
        private string _videoFormat;
        /// <summary>
        /// 视频格式
        /// </summary>
        public string VideoFormat
        {
            get { return _videoFormat; }
            set { _videoFormat = value; }
        }

        private string _rtspURIFirst;
        /// <summary>
        /// rtsp地址1
        /// </summary>
        public string RtspURIFirst
        {
            get { return _rtspURIFirst; }
            set { _rtspURIFirst = value; }
        }
        private string _rtspURISecond;
        /// <summary>
        /// rtsp地址2
        /// </summary>
        public string RtspURISecond
        {
            get { return _rtspURISecond; }
            set { _rtspURISecond = value; }
        }


        private int _realStreamType;
        /// <summary>
        /// 码流类型
        /// </summary>
        public int RealStreamType
        {
            get { return _realStreamType; }
            set { _realStreamType = value; }
        }
        private string _longitude;
        /// <summary>
        /// 经度
        /// </summary>
        public string Longitude
        {
            get { return _longitude; }
            set { _longitude = value; }
        }
        private string _latitude;
        /// <summary>
        /// 纬度
        /// </summary>
        public string Latitude
        {
            get { return _latitude; }
            set { _latitude = value; }
        }
        private string _address;
        /// <summary>
        /// 地址
        /// </summary>
        public string Address
        {
            get { return _address; }
            set { _address = value; }
        }
        private int _status;
        /// <summary>
        /// 状态
        /// </summary>
        public int Status
        {
            get { return _status; }
            set { _status = value; }
        }
        private DateTime _EndTime;
        /// <summary>
        /// 时间
        /// </summary>
        public DateTime EndTime
        {
            get { return _EndTime; }
            set { _EndTime = value; }
        }
        private DateTime _createTime;
        /// <summary>
        /// 接入时间
        /// </summary>
        public DateTime CreateTime
        {
            get { return _createTime; }
            set { _createTime = value; }
        }
        private DateTime _updateTime;
        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime UpdateTime
        {
            get { return _updateTime; }
            set { _updateTime = value; }
        }

        public void Load(System.Data.DataRow row)
        {

            _id = row["ID"] != null ? new Guid(row["ID"] as string) : System.Guid.NewGuid();
            _channelGuid = row["ChannelGuid"] != null ? Convert.ToInt32(row["ChannelGuid"]) : 0;
            _channelID = row["ChannelID"] as string;
            _gbID = row["GBDevID"] as string;
            _name = row["Name"] as string;
            _deviceID = row["DeviceID"] as string;
            _videoStatus = row["VideoStatus"] != null ? Convert.ToInt32(row["VideoStatus"]) : 0;
            _recordStatus = row["RecordStatus"] != null ? Convert.ToInt32(row["RecordStatus"]) : 0;
            _audioFormat = row["AudioFormat"] as string;
            _videoFormat = row["VideoFormat"] as string;
            _rtspURIFirst = row["RTSP1"] as string;
            _rtspURISecond = row["RTSP2"] as string;
            _realStreamType = row["RealStreamType"] != null ? Convert.ToInt32(row["RealStreamType"]) : 0;
            _longitude = row["Longitude"] as string;
            _latitude = row["Latitude"] as string;
            _address = row["Address"] as string;
            _status = row["Status"] != null ? Convert.ToInt32(row["Status"]) : 0;
            _EndTime = row["EndTime"] != DBNull.Value ? Convert.ToDateTime(row["EndTime"]) : DateTime.Now;
            _createTime = row["CreateTime"] != DBNull.Value ? Convert.ToDateTime(row["CreateTime"]) : DateTime.Now;
            _updateTime = row["UpdateTime"] != DBNull.Value ? Convert.ToDateTime(row["UpdateTime"]) : DateTime.Now;
        }

        public System.Data.DataTable GetTable()
        {
            throw new NotImplementedException();
        }

        public string ToXML()
        {
            throw new NotImplementedException();
        }

        public string ToXMLNoParent()
        {
            throw new NotImplementedException();
        }

        public string GetXMLElementName()
        {
            throw new NotImplementedException();
        }

        public string GetXMLDocumentElementName()
        {
            throw new NotImplementedException();
        }


        public Dictionary<Guid, object> Load(System.Xml.XmlDocument dom)
        {
            throw new NotImplementedException();
        }
    }
}
