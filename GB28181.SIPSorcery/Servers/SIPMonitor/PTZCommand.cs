using System.ComponentModel;

namespace GB28181.Servers.SIPMonitor
{
    #region 云台控制命令
    /// <summary>
    /// 云台控制命令
    /// </summary>
    public enum PTZCommand : int
    {
        /// <summary>
        /// 停止
        /// </summary>
        [Description("停止")]
        Stop = 0,
        /// <summary>
        /// 上
        /// </summary>
        [Description("上")]
        Up = 1,
        /// <summary>
        /// 左上
        /// </summary>
        [Description("左上")]
        UpLeft = 2,
        /// <summary>
        /// 右下
        /// </summary>
        [Description("右上")]
        UpRight = 3,
        /// <summary>
        /// 下
        /// </summary>
        [Description("下")]
        Down = 4,
        /// <summary>
        /// 左下
        /// </summary>
        [Description("左下")]
        DownLeft = 5,
        /// <summary>
        /// 右下
        /// </summary>
        [Description("右下")]
        DownRight = 6,
        /// <summary>
        /// 左
        /// </summary>
        [Description("左")]
        Left = 7,
        /// <summary>
        /// 右
        /// </summary>
        [Description("右")]
        Right = 8,
        /// <summary>
        /// 聚焦+
        /// </summary>
        [Description("聚焦+")]
        Focus1 = 9,
        /// <summary>
        /// 聚焦-
        /// </summary>
        [Description("聚焦-")]
        Focus2 = 10,
        /// <summary>
        /// 变倍+
        /// </summary>
        [Description("变倍+")]
        Zoom1 = 11,
        /// <summary>
        /// 变倍-
        /// </summary>
        [Description("变倍-")]
        Zoom2 = 12,
        /// <summary>
        /// 光圈开
        /// </summary>
        [Description("光圈Open")]
        Iris1 = 13,
        /// <summary>
        /// 光圈关
        /// </summary>
        [Description("光圈Close")]
        Iris2 = 14,
        /// <summary>
        /// 设置预置位
        /// </summary>
        [Description("设置预置位")]
        SetPreset = 15,
        /// <summary>
        /// 调用预置位
        /// </summary>
        [Description("调用预置位")]
        GetPreset = 16,
        /// <summary>
        /// 删除预置位
        /// </summary>
        [Description("删除预置位")]
        RemovePreset = 17
    }
    #endregion
}
