using GLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIPSorcery.GB28181.Sys.Config
{
    /// <summary>
    /// 图像分辨率
    /// </summary>
    public enum ImageResolution : byte
    {
        /// <summary>
        /// 未设置
        /// </summary>
        R_Undefined = 0,

        /// <summary>
        /// 720
        /// </summary>
        R_1280_720 = 1,

        /// <summary>
        /// 960
        /// </summary>
        R_1280_960 = 2,

        /// <summary>
        /// 1080
        /// </summary>
        R_1920_1080 = 3,

        /// <summary>
        /// D1
        /// </summary>
        R_704_576 = 4,

        /// <summary>
        /// 4CIF
        /// </summary>
        R_720_576 = 5,

        /// <summary>
        /// CIF
        /// </summary>
        R_352_288 = 6,

        /// <summary>
        /// VGA
        /// </summary>
        R_640_480 = 7,

    }

    /// <summary>
    /// 码流类型
    /// </summary>
    public enum StreamType
    {
        noPcmeetBind = -1,
        /// <summary>
        /// 主码流
        /// </summary>
        mainStream = 0,
        /// <summary>
        /// 子码流
        /// </summary>
        childStream = 1,
        /// <summary>
        /// 双子码流
        /// </summary>
        dounlechildStream = 2,
        /// <summary>
        /// 双流模式
        /// </summary>
        doubleMode = 3,
        /// <summary>
        /// 自适应模式
        /// </summary>
        adaptiveMode = 4,
    }
}
