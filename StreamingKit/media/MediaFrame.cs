using Helpers;
using Common.Networks;
using StreamingKit.Codec;
using System;
using System.IO;
using System.Text;
namespace StreamingKit
{

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
    public partial class MediaFrame : IByteObj
    {
        public byte MediaFrameVersion;//0x00小版本，0x01全版本,0xff为命令（命令的内容见nRawType对应的枚举）
        //0x02版本为区分音频版本

        /// <summary>
        /// 扩展字段，默认为1 
        /// 当MediaFrameVersion 为0x00或0x01，则该字段：0为不可被丢弃(一般只有首帧是不可丢弃的。)，1为可被丢弃,
        /// 当MediaFrameVersion为0xff时，该字段为命令类型，命令包不可丢弃
        /// </summary>
        public byte Ex { get; set; } = 1;//
        public byte IsKeyFrame { get; set; }	//是否为关键帧，0为非关键帧，1为关键帧
        public long NTimetick { get; set; }		//时间辍
        public byte IsAudio { get; set; }		//是否为音频,0:视频,1:音频
        public int Size { get; set; }		//数据大小,紧跟着该结构后的数据媒体数据
        public int Offset { get; set; }//偏移量

        public short StreamID { get; set; } = 0;//区分音频（或视频）数据。0代表一路  1代表一路

        /// <summary>
        /// 编码器CODE
        /// </summary>
        public int NEncoder { get; set; }
        /// <summary>
        /// 编码器名称
        /// </summary>
        public string EncodeName { get { return GetGeneralEncodecName(NEncoder); } }

        /// <summary>
        /// H264里面的SPS长度
        /// </summary>
        public short SPSLen { get; set; }
        /// <summary>
        /// H264里面的PPS长度
        /// </summary>
        public short PPSLen { get; set; }
        /// <summary>
        /// 视频宽
        /// </summary>
        public int Width { get; set; }
        /// <summary>
        /// 视频高
        /// </summary>
        public int Height { get; set; }
        /// <summary>
        /// 采样率,speex一般为8000
        /// </summary>
        public int Frequency { get; set; }
        /// <summary>
        /// 1=单通道，2=双通道,speex编码一般为1
        /// </summary>
        public int Channel { get; set; }
        /// <summary>
        /// 0=8位,1=16位,一般为2
        /// </summary>
        public short AudioFormat { get; set; }
        /// <summary>
        /// 采集大小,speex 一般为160
        /// </summary>
        public short Samples { get; set; }
        /// <summary>
        /// 媒体数据
        /// </summary>
        public byte[] Data { get; set; } = Array.Empty<byte>();

        public byte[] SerializableData { get; set; }//序列化数据

        public MediaFrame()
        {
        }

        public MediaFrame(byte version)
        {
            MediaFrameVersion = version;
        }
        public MediaFrame(byte[] buf)
            : this(new System.IO.MemoryStream(buf))
        {

        }
        public MediaFrame(Stream stream)
        {
            SetBytes(stream);
        }
        public void SetBytes(byte[] buf)
        {
            SetBytes(new System.IO.MemoryStream(buf));

        }
        //充填媒体帧
        public void SetBytes(Stream stream)
        {
            var br = new System.IO.BinaryReader(stream);
            MediaFrameVersion = br.ReadByte();
            Ex = br.ReadByte();
            IsKeyFrame = br.ReadByte();
            NTimetick = br.ReadInt64();
            IsAudio = br.ReadByte();
            if (MediaFrameVersion == 2)
            {
                StreamID = br.ReadInt16();
            }
            Size = br.ReadInt32();
            Offset = br.ReadInt32();
            if (MediaFrameVersion == 1 || MediaFrameVersion == 2)
            {
                NEncoder = br.ReadInt32();
                if (IsAudio == 0)
                {
                    SPSLen = br.ReadInt16();
                    PPSLen = br.ReadInt16();
                    Width = br.ReadInt32();
                    Height = br.ReadInt32();
                }
                else
                {
                    Frequency = br.ReadInt32();
                    Channel = br.ReadInt32();
                    AudioFormat = br.ReadInt16();
                    Samples = br.ReadInt16();
                }
            }
            try
            {
                Data = br.ReadBytes(Size + Offset);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        //获取媒体帧对应字节数组
        public byte[] GetBytes()
        {
            var ms = new MemoryStream();
            var bw = new BinaryWriter(ms);
            bw.Write(MediaFrameVersion);
            bw.Write(Ex);
            bw.Write(IsKeyFrame);
            bw.Write(NTimetick);
            bw.Write(IsAudio);
            if (MediaFrameVersion == 2)
            {
                bw.Write(StreamID);
            }
            bw.Write(Size);
            int _tOffset = Offset;// 重置偏移量
            Offset = 0;
            bw.Write(Offset);

            if (MediaFrameVersion == 1 || MediaFrameVersion == 2)
            {
                bw.Write(NEncoder);
                if (IsAudio == 0)
                {
                    bw.Write(SPSLen);
                    bw.Write(PPSLen);
                    bw.Write(Width);
                    bw.Write(Height);
                }
                else
                {
                    bw.Write(Frequency);
                    bw.Write(Channel);
                    bw.Write(AudioFormat);
                    bw.Write(Samples);
                }
            }
            bw.Write(Data, _tOffset, Size);
            Offset = _tOffset;
            return ms.ToArray();
        }

        public override string ToString()
        {
            if (!this.IsCommandMediaFrame())
            {
                if (this.IsKeyFrame == 1)
                {
                    if (this.IsAudio == 0)
                    {
                        return string.Format("nIsAudio:%{0}  nIsKeyFrame:{1}  nSize:{2}  codec:{3}  width:{4}  height:{5}", IsAudio, IsKeyFrame, Size, EncodeName, Width, Height);
                    }
                    else
                    {
                        return string.Format("nIsAudio:%{0}  nIsKeyFrame:{1}  nSize:{2}  codec:{3}  channel:{4} frequency:{5}", IsAudio, IsKeyFrame, Size, EncodeName, Channel, Frequency);
                    }

                }
                else
                {
                    return string.Format("nIsAudio:{0}  nIsKeyFrame:{1}  nTick:{2}  nSize:{3}   ", IsAudio, IsKeyFrame, NTimetick, Size);
                }
            }
            else
            {
                return string.Format("nIsAudio:{0}  Command:{1}", IsAudio, (MediaFrameCommandType)Ex);
            }

        }

        public bool IsAllowDiscard()
        {

            return !IsCommandMediaFrame() && !((MediaFrameVersion == 0 || MediaFrameVersion == 1) && Ex == 0);
        }
    }
    public partial class MediaFrame
    {

        public byte[] GetSPS()
        {
            if (this.IsAudio == 0 && this.IsKeyFrame == 1)
            {
                byte[] sps = new byte[SPSLen];
                Array.Copy(Data, 4, sps, 0, SPSLen);
                return sps;
            }
            else
                throw new Exception();
        }
        public byte[] GetPPS()
        {
            if (this.IsAudio == 0 && this.IsKeyFrame == 1)
            {
                byte[] pps = new byte[PPSLen];
                Array.Copy(Data, SPSLen + 8, pps, 0, PPSLen);
                return pps;
            }
            else
                throw new Exception();
        }
        public bool IsCommandMediaFrame()
        {
            return MediaFrameVersion == 0xff;
        }
        public MediaFrameCommandType GetCommandType()
        {
            if (!IsCommandMediaFrame())
                throw new Exception("");
            return (MediaFrameCommandType)Ex;
        }
        public MediaFrame CreateCommandMediaFrame(MediaFrameCommandType cmd)
        {
            return new MediaFrame()
            {
                MediaFrameVersion = 0xff,
                Ex = (byte)cmd,
                IsKeyFrame = 0,
                IsAudio = this.IsAudio,
                NTimetick = 0,
                Size = 0,
                Data = Array.Empty<byte>(),
            };
        }
        public static MediaFrame CreateCommandMediaFrame(bool isAudio, MediaFrameCommandType cmd)
        {
            return CreateCommandMediaFrame(isAudio, cmd, null);
        }
        public static MediaFrame CreateCommandMediaFrame(bool isAudio, MediaFrameCommandType cmd, byte[] data = null)
        {
            data ??= Array.Empty<byte>();
            var mf = new MediaFrame()
            {
                MediaFrameVersion = 0xff,
                Ex = (byte)cmd,
                IsKeyFrame = 0,
                IsAudio = (byte)(isAudio ? 1 : 0),
                NTimetick = 0,
                Size = data.Length,
                Data = data ?? Array.Empty<byte>(),
            };
            return mf;
        }
        public static int H264Encoder = GetGeneralEncoder("H264");
        public static int H263Encoder = GetGeneralEncoder("H263");
        public static int SPEXEncoder = GetGeneralEncoder("SPEX");
        public static int FLV1Encoder = GetGeneralEncoder("FLV1");
        public static int AAC_Encoder = GetGeneralEncoder("AAC_");
        public static int PCM_Encoder = GetGeneralEncoder("PCM_");

        public static int GetGeneralEncoder(string name)
        {
            name = name.ToUpper();
            byte[] buf = Encoding.UTF8.GetBytes(name);
            return BitConverter.ToInt32(buf, 0);
        }
        public static string GetGeneralEncodecName(int generalEncoder)
        {
            byte[] buf = BitConverter.GetBytes(generalEncoder);
            return Encoding.UTF8.GetString(buf).ToUpper();
        }
        public static int GetEncoderByAVCoderID(AVCode code_id)
        {
            switch (code_id)
            {
                case AVCode.CODEC_ID_H264:
                    return H264Encoder;
                case AVCode.CODEC_ID_H263:
                    return H263Encoder;
                case AVCode.CODEC_ID_FLV1:
                    return FLV1Encoder;
                case AVCode.CODEC_ID_XVID:
                    return GetGeneralEncoder("XVID");
                case AVCode.CODEC_ID_MPEG4:
                    return GetGeneralEncoder("MP4V");
                default:
                    break;
            }
            return 0;
        }
        public static int ConverterToFFMPEGCoderID(int id)
        {
            var name = GetGeneralEncodecName(id);
            if (name.EqIgnoreCase("H264"))
                return (int)AVCode.CODEC_ID_H264;
            if (name.EqIgnoreCase("H263"))
                return (int)AVCode.CODEC_ID_H263;
            if (name.EqIgnoreCase("FLV1"))
                return (int)AVCode.CODEC_ID_FLV1;
            if (name.EqIgnoreCase("XVID"))
                return (int)AVCode.CODEC_ID_XVID;
            if (name.EqIgnoreCase("MP4V"))
                return (int)AVCode.CODEC_ID_MPEG4;
            return -1;
        }
        public static byte[][] GetSPS_PPS(byte[] enc)
        {
            int i = 4, sps_len = 0, pps_len = 0;
            while (i < enc.Length - 4)
            {
                if (enc[i] == 0 && enc[i + 1] == 0 && enc[i + 2] == 0 && enc[i + 3] == 1)
                {
                    sps_len = i;
                    break;
                }
                i++;
            }
            i += 1;
            while (i < enc.Length - 4)
            {
                if (enc[i] == 0 && enc[i + 1] == 0 && enc[i + 2] == 0 && enc[i + 3] == 1)
                {
                    pps_len = i - sps_len;
                    break;
                }
                i++;
            }
            sps_len -= 4;
            pps_len -= 4;
            byte[] sps = new byte[sps_len];
            byte[] pps = new byte[pps_len];

            Array.Copy(enc, 4, sps, 0, sps_len);
            Array.Copy(enc, sps_len + 4 + 4, pps, 0, pps_len);

            return new byte[][] { sps, pps };
        }


        public static MediaFrame CreateVideoKeyFrame(VideoEncodeCfg cfg, long timetick, byte[] data, int offset, int size)
        {
            MediaFrame mFrame = CreateVideoFrame(cfg, timetick, data, offset, size);// new
            // MediaFrame((byte)
            // 1);
            mFrame.MediaFrameVersion = 1;
            mFrame.IsKeyFrame = 1;
            mFrame.Width = cfg.Width;
            mFrame.Height = cfg.Height;
            mFrame.SPSLen = (short)(cfg.SPS == null ? 0 : (byte)cfg.SPS.Length);
            mFrame.PPSLen = (short)(cfg.PPS == null ? 0 : (byte)cfg.PPS.Length);
            mFrame.NEncoder = cfg.encoder;
            return mFrame;
        }

        public static MediaFrame CreateVideoFrame(VideoEncodeCfg cfg, long timetick, byte[] data, int offset, int size)
        {
            if (cfg is null)
            {
                throw new ArgumentNullException(nameof(cfg));
            }

            MediaFrame mFrame = new MediaFrame(0)
            {
                IsAudio = 0,
                IsKeyFrame = 0,
                NTimetick = timetick,
                Size = size,
                Offset = offset,
                Data = data
            };
            return mFrame;
        }

        public static MediaFrame CreateAudioKeyFrame(AudioEncodeCfg cfg, long timetick, byte[] data, int offset, int size)
        {
            MediaFrame mFrame = CreateAudioFrame(cfg, timetick, data, offset, size);// new
            // MediaFrame((byte)
            // 1);
            mFrame.MediaFrameVersion = 1;
            mFrame.IsKeyFrame = 1;
            mFrame.Frequency = cfg.frequency;
            mFrame.Channel = cfg.channel;
            mFrame.AudioFormat = (short)cfg.format;
            mFrame.Samples = (short)cfg.samples;
            mFrame.NEncoder = cfg.encoder;

            return mFrame;

        }

        public static MediaFrame CreateAudioFrame(AudioEncodeCfg cfg, long timetick, byte[] data, int offset, int size)
        {
            if (cfg is null)
            {
                throw new ArgumentNullException(nameof(cfg));
            }

            MediaFrame mFrame = new MediaFrame(0)
            {
                IsAudio = 1,
                IsKeyFrame = 0,
                NTimetick = timetick,
                Size = size,
                Offset = offset,
                Data = data
            };
            return mFrame;
        }
    }

    /// <summary>
    /// 媒体帧命令类型
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
