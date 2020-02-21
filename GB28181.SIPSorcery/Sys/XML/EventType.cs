using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GB28181.SIPSorcery.Sys.XML
{
    /// <summary>
    /// 状态改变事件类型
    /// </summary>
    public enum EventType : byte
    {
        /// <summary>
        /// 上线
        /// </summary>
        ON = 0,
        /// <summary>
        /// 离线
        /// </summary>
        OFF = 1,
        /// <summary>
        /// 视频丢失
        /// </summary>
        VLOST = 2,
        /// <summary>
        /// 故障
        /// </summary>
        DEFECT = 3,
        /// <summary>
        /// 增加
        /// </summary>
        ADD = 4,
        /// <summary>
        /// 删除
        /// </summary>
        DEL = 5,
        /// <summary>
        /// 更新
        /// </summary>
        UPDATE = 6
    }
}
