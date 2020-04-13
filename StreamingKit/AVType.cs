namespace StreamingKit
{
    /// <summary>
    ///AV  Payload Type
    /// </summary>
    public enum AVCode
    {
        PIX_FMT_YUV420P = 0,
        PIX_FMT_RGB32 = 6,
        CODEC_ID_AAC = 86018,
        CODEC_TYPE_VIDEO = 0,
        CODEC_TYPE_AUDIO = 1,
        CODEC_ID_H264 = 28,
        CODEC_ID_H263 = 5,
        CODEC_ID_FLV1 = 22,
        CODEC_ID_XVID = 63,
        CODEC_ID_MPEG4 = 13,
    }

    /// <summary>
    /// Frame type
    /// </summary>
    public enum MediaFrameCommandType : byte
    {
        /// <summary>
        /// 无效值
        /// </summary>
        None = 0x00,
        /// <summary>
        /// 开始
        /// </summary>
        Start = 0x01,
        /// <summary>
        /// 停止 
        /// </summary>
        Stop = 0x02,
        /// <summary>
        /// 暂停
        /// </summary>
        Pause = 0x03,
        /// <summary>
        /// 继续
        /// </summary>
        Continue = 0x04,
        /// <summary>
        /// 重置播放位置
        /// </summary>
        ResetPos = 0x05,

        /// <summary>
        /// 音视频索引
        /// </summary>
        Index = 0x10,
        /// <summary>
        /// 缩略图
        /// </summary>
        Thumbnail = 0x24,

        /// <summary>
        /// 重置播放位置
        /// </summary>
        ResetCodec = 0x28,
        /// <summary>
        /// 清除发送缓冲区(音视频),该指令一般只能用于音频优先传输模式,暂只在UDP传输模式中有效
        /// </summary>
        ClearTransportBuffer = 0x2A,
        /// <summary>
        /// 清除视频传输缓冲区,该指令一般只能用于音频优先传输模式,暂只在UDP传输模式中有效
        /// </summary>
        ClearVideoTransportBuffer = 0x2B,
        /// <summary>
        /// 清除音频传输缓冲区,该指令一般只能用于音频优先传输模式,暂只在UDP传输模式中有效
        /// </summary>
        ClearAudioTransportBuffer = 0x2C,
    }


}
