using System;

namespace GB28181.Sys.Model
{
    public class Camera
    {
        public long ID { get; set; }
        public string ChannelID { get; set; }
        public string Name { get; set; }
        public string DeviceID { get; set; }
        public long? NvrID { get; set; }
        public string Status { get; set; }
        public long RecordStatus { get; set; }
        public long? FrameRate { get; set; }
        public string AudioFomate { get; set; }
        public string VideoFomate { get; set; }
        public long? RealStreamType { get; set; }
        public long? Cache { get; set; }
        public string Longitude { get; set; }
        public string Latitude { get; set; }
        public string Adddress { get; set; }
        public long IsPTZ { get; set; }
        public DateTime EndTime { get; set; }
        public string Manufacturer { get; set; }
        public string Model { get; set; }
        public string Owner { get; set; }
        public string CivilCode { get; set; }
        public string Block { get; set; }
        public long Parental { get; set; }
        public long AccessType { get; set; }
        public string ParentID { get; set; }
        public long? SafetyWay { get; set; }
        public long RegisterWay { get; set; }
        public string CertNum { get; set; }
        public long Certifiable { get; set; }
        public long ErrCode { get; set; }
        public long Secrecy { get; set; }
        public string IPAddress { get; set; }
        public int Port { get; set; }
        public string Password { get; set; }
        public long? PTZType { get; set; }
        public long? PositionType { get; set; }
        public long? RoomType { get; set; }
        public long? UserType { get; set; }
        public long? SupplyLightType { get; set; }
        public long? DirectionType { get; set; }
        public string Resolution { get; set; }
        public string BusinessGroupID { get; set; }
        public string DownloadSpeed { get; set; }
        public long? SVCSpaceSupportMode { get; set; }
        public long? SVCTimeSupportMode { get; set; }
        public DateTime CreateTime { get; set; }
        public DateTime UpdateTime { get; set; }
        public string RtspMain { get; set; }
        public string RtspSub { get; set; }
        public string SubordinatePlatform { get; set; }
        public string RtmpStreamKey { get; set; }
        public string ExAttribute { get; set; }
        public int? VoiceTwoWay { get; set; }
    }
}
