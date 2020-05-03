using GB28181.Servers.SIPMessage;
using GB28181.Sys.XML;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GB28181.Servers
{
    public class GB28181Catalog
    {
        private static GB28181Catalog _instance;
        private Dictionary<string, string> _devList;
        public SIPMessageCore MessageCore;

        public Action<Catalog> OnCatalogReceived;

        public Action<NotifyCatalog> OnNotifyCatalogReceived;

        /// <summary>
        /// 设备列表
        /// </summary>
        public Dictionary<string, string> DevList
        {
            get { return _devList; }
        }

        /// <summary>
        /// 以单例模式访问
        /// </summary>
        public static GB28181Catalog Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new GB28181Catalog();
                }
                return _instance;
            }
        }

        private GB28181Catalog()
        {
            _devList = new Dictionary<string, string>();
        }

        /// <summary>
        /// 获取设备目录
        /// </summary>
        public void GetCatalog()
        {
            MessageCore.DeviceCatalogQuery();
        }
    }
}
