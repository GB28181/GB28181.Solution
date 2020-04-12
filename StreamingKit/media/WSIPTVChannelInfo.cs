namespace StreamingKit.Media.TS
{

    public class WSIPTVChannelInfo
    {
        public string Name { get; set; }

        public string Key { get; set; }

        public string IP { get; set; }
        public int Port { get; set; }

        public WSIPTVChannelInfo()
        {

        }

        public WSIPTVChannelInfo(string key, string name, string ip, int port)
        {
            Key = key;
            Name = name;
            IP = ip;
            Port = port;
        }


    }

}
