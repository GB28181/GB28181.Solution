namespace GB28181.Server.Settings
{

    public class SipAccount
    {
        public uint ID { get; set; }
        public string ServiceType { get; set; }
        public string Name { get; set; }
        public string GbVersion { get; set; }
        public string LocalID { get; set; }
        public string LocalIP { get; set; }
        public uint LocalPort { get; set; }
        public uint RemotePort { get; set; }
        public string MediaIP { get; set; }
        public string MediaPort { get; set; }
        public bool Authentication { get; set; }
        public string SIPUsername { get; set; }
        public string SIPPassword { get; set; }
        public string MsgType { get; set; }
        public string StreamType { get; set; }
        public string TcpMode { get; set; }
        public string MsgEncode { get; set; }
        public bool PacketOutOrder { get; set; }
        public uint KeepaliveInterval { get; set; }
        public uint KeepaliveNumber { get; set; }

    }


    public class DbConfig
    {
        public string DefaultConnection { get; set; }
        public string DBStorageType { get; set; }
        public string DBConnStr { get; set; }
    }


    public class SipAccounts
    {
        public SipAccount LocalAccout { get; set; }
        public SipAccount[] Remotes { get; set; }

    }

}
