using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIPSorcery.GB28181.Sys.Model
{
    public class CameraInfo
    {
        public long ID { get; set; }
        public string ChannelID { get; set; }
        public string Name { get; set; }
        public string DeviceID { get; set; }
        public Nullable<long> NvrID { get; set; }
        public string Status { get; set; }
        public long RecordStatus { get; set; }
        public Nullable<long> FrameRate { get; set; }
        public string AudioFomate { get; set; }
        public string VideoFomate { get; set; }
        public Nullable<long> RealStreamType { get; set; }
        public Nullable<long> Cache { get; set; }
        public string Longitude { get; set; }
        public string Latitude { get; set; }
        public string Adddress { get; set; }
        public long IsPTZ { get; set; }
        public System.DateTime EndTime { get; set; }
        public string Manufacturer { get; set; }
        public string Model { get; set; }
        public string Owner { get; set; }
        public string CivilCode { get; set; }
        public string Block { get; set; }
        public long Parental { get; set; }
        public long AccessType { get; set; }
        public string ParentID { get; set; }
        public Nullable<long> SafetyWay { get; set; }
        public long RegisterWay { get; set; }
        public string CertNum { get; set; }
        public long Certifiable { get; set; }
        public long ErrCode { get; set; }
        public long Secrecy { get; set; }
        public string IPAddress { get; set; }
        public Nullable<long> Port { get; set; }
        public string Password { get; set; }
        public Nullable<long> PTZType { get; set; }
        public Nullable<long> PositionType { get; set; }
        public Nullable<long> RoomType { get; set; }
        public Nullable<long> UserType { get; set; }
        public Nullable<long> SupplyLightType { get; set; }
        public Nullable<long> DirectionType { get; set; }
        public string Resolution { get; set; }
        public string BusinessGroupID { get; set; }
        public string DownloadSpeed { get; set; }
        public Nullable<long> SVCSpaceSupportMode { get; set; }
        public System.DateTime CreateTime { get; set; }
        public System.DateTime UpdateTime { get; set; }
        public Nullable<long> SVCTimeSupportMode { get; set; }
        public string Rtsp_Main { get; set; }
        public string Rtsp_Sub { get; set; }
        public string SubordinatePlatform { get; set; }
        public string RtmpStreamKey { get; set; }
        public string ExAttribute { get; set; }
        public Nullable<int> VoiceTwoWay { get; set; }
    }
}
