using GB28181.Servers;
using GB28181.Sys.XML;

namespace GB28181.Server.Message
{

    #region 委托回调

    /// <summary>
    /// 设置服务状态回调
    /// </summary>
    /// <param name="msg"></param>
    /// <param name="state"></param>
    public delegate void SIPServiceStatusHandler(string msg, ServiceStatus state);
    /// <summary>
    /// 设置设备查询目录回调
    /// </summary>
    /// <param name="cata"></param>
    public delegate void CatalogQueryHandler(Catalog cata);
    /// <summary>
    /// 设置录像文件查询回调
    /// </summary>
    /// <param name="record"></param>
    public delegate void RecordQueryHandler(RecordInfo record);
    /// <summary>
    /// 设置心跳消息回调
    /// </summary>
    /// <param name="remoteEP"></param>
    /// <param name="keepalive"></param>
    public delegate void KeepAliveHandler(string remoteEP, KeepAlive keepalive);
    #endregion

}
