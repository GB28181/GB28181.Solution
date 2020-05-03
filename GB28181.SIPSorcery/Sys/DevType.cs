namespace GB28181.Sys.XML
{
    /// <summary>
    /// 设备状态
    /// </summary>
    public enum DevStatus
    {
        /// <summary>
        /// 正常
        /// </summary>
        ON = 0,
        /// <summary>
        /// 故障
        /// </summary>
        OFF = 1
    }

    /// <summary>
    /// 设备目录类型
    /// </summary>
    public enum DevCataType
    {
        /// <summary>
        /// 未知的
        /// </summary>
        UnKnown,
        /// <summary>
        /// 平台根
        /// </summary>
        Root,
        /// <summary>
        /// 省级行政区划
        /// </summary>
        ProviceCata,
        /// <summary>
        /// 市级行政区划
        /// </summary>
        CityCata,
        /// <summary>
        /// 区县级行政区划
        /// </summary>
        AreaCata,
        /// <summary>
        /// 基层接入单位行政区划
        /// </summary>
        BasicUnit,
        /// <summary>
        /// 系统目录项
        /// </summary>
        SystemCata,
        /// <summary>
        /// 业务分组目录项
        /// </summary>
        BusinessGroupCata,
        /// <summary>
        /// 虚拟目录分组项
        /// </summary>
        VirtualGroupCata,
        /// <summary>
        /// 设备
        /// </summary>
        Device,
        /// <summary>
        /// 其他
        /// </summary>
        Other
    }

    public class DevType
    {
        /// <summary>
        /// 获取设备目录类型
        /// </summary>
        /// <param name="devId">编码</param>
        /// <returns></returns>
        public static DevCataType GetCataType(string devId)
        {
            DevCataType devCata = DevCataType.UnKnown;

            switch (devId.Length)
            {
                case 2:
                    devCata = DevCataType.ProviceCata;
                    break;
                case 4:
                    devCata = DevCataType.CityCata;
                    break;
                case 6:
                    devCata = DevCataType.AreaCata;
                    break;
                case 8:
                    devCata = DevCataType.BasicUnit;
                    break;
                case 20:
                    int extId = int.Parse(devId.Substring(10, 3));
                    if (extId == 200)    //ID编码11-13位采用200标识系统ID类型
                    {
                        devCata = DevCataType.SystemCata;
                    }
                    else if (extId == 215)   //业务分组标识，编码采用D.1中的20位ID格式，扩展215类型代表业务分组
                    {
                        devCata = DevCataType.BusinessGroupCata;
                    }
                    else if (extId == 216)   //虚拟组织标识，编码采用D.1中的20位ID格式，扩展216类型代表虚拟组织
                    {
                        devCata = DevCataType.VirtualGroupCata;
                    }
                    else if (extId == 131 || extId == 132||extId==134||extId==137)  //D.1中摄像机，网络摄像机编码
                    {
                        devCata = DevCataType.Device;
                    }
                    else
                    {
                        devCata = DevCataType.Other;    //其他类型
                    }
                    break;
            }
            return devCata;
        }
    }
}
