#if _D
#define _D1 //模拟视联网发流
#endif

using BoxMatrix.Media;
using Helpers;
using StreamingKit;
using System;
using System.IO;

namespace GB28181.WinTool.Media
{


    public class MediaSteamConverter
    {

        public class StreamInfo
        {
            public int Video_FPS { get; set; }
            public byte[] Video_SPS { get; set; }
            public byte[] Video_PPS { get; set; }
            public string Video_SPSString { get; set; }
            public string Video_PPSString { get; set; }
            public bool SPS_PPSInited { get; set; } = false;
            public bool IsFirstAudioFrame { get; set; } = true;
            public int Width { get; set; } = -1;
            public int Height { get; set; } = -1;

        }
        public static MediaFrame MediaSteamEntity2MediaFrame(MediaFrameEntity entity, ref StreamInfo streamInfo)
        {
            if (entity.MediaType == MediaType.VideoES)
            {
                if (entity.KeyFrame == 0)
                {
                    MediaFrame mf = null;
                    if (streamInfo != null && streamInfo.SPS_PPSInited && streamInfo.Width == entity.Width && streamInfo.Height == entity.Height)
                    {
                        mf = new MediaFrame()
                        {
                            IsAudio = 0,
                            IsKeyFrame = 0,
                            Size = entity.Length,
                            Height = entity.Height,
                            Width = entity.Width,
                            SPSLen = (short)streamInfo.Video_SPS.Length,
                            PPSLen = (short)streamInfo.Video_PPS.Length,
                            NTimetick = ThreadEx.TickCount,
                            Offset = 0,
                            Encoder = MediaFrame.H264Encoder,
                            Ex = 1,
                            MediaFrameVersion = 0x00,
                        };
                        mf.SetData(entity.Buffer);
                    }
                    return mf;
                }
                else if (entity.KeyFrame == 1)
                {
                    bool needResetCodec = false;
                    if (streamInfo == null || (streamInfo != null && (streamInfo.Width != entity.Width || streamInfo.Height != entity.Height)))
                    {
                        streamInfo = new StreamInfo();
                        var sps_pps = GetSPS_PPS(entity.Buffer);
                        if (sps_pps != null)
                        {
                            streamInfo.Video_SPS = sps_pps[0];
                            streamInfo.Video_PPS = sps_pps[1];
                            streamInfo.Video_SPSString = streamInfo.Video_SPS.To16Strs();
                            streamInfo.Video_PPSString = streamInfo.Video_PPS.To16Strs();
                            streamInfo.SPS_PPSInited = true;
                            streamInfo.Width = entity.Width;
                            streamInfo.Height = entity.Height;
                            needResetCodec = true;
                        }
                    }
                    if (streamInfo.Video_SPS == null)
                    {
                        var mf = new MediaFrame()
                        {
                            IsAudio = 0,
                            IsKeyFrame = 1,
                            Size = entity.Length,
                            Height = entity.Height,
                            Width = entity.Width,
                            SPSLen = 0,
                            PPSLen = 0,
                            NTimetick = ThreadEx.TickCount,
                            Offset = 0,
                            Encoder = MediaFrame.H264Encoder,
                            Ex = (byte)(needResetCodec ? 0 : 1),
                            //nEx=(byte)entity.Ex,
                            MediaFrameVersion = 0x01,
                        };
                        mf.SetData(entity.Buffer);
                        return mf;
                    }
                    else
                    {
                        var mf = new MediaFrame()
                        {
                            IsAudio = 0,
                            IsKeyFrame = 1,
                            Size = entity.Length,
                            Height = entity.Height,
                            Width = entity.Width,
                            SPSLen = (short)streamInfo.Video_SPS.Length,
                            PPSLen = (short)streamInfo.Video_PPS.Length,
                            NTimetick = ThreadEx.TickCount,
                            Offset = 0,
                            Encoder = MediaFrame.H264Encoder,
                            Ex = (byte)(needResetCodec ? 0 : 1),
                            //nEx=(byte)entity.Ex,
                            MediaFrameVersion = 0x01,
                        };
                        mf.SetData(entity.Buffer);
                        return mf;
                    }
                }
                else
                {
                    throw new Exception("帧类型错误");
                }



            }
            else if (entity.MediaType == MediaType.AudioES)
            {
                if (streamInfo == null)
                {
                    streamInfo = new StreamInfo();
                }
                try
                {
                    var mf = new MediaFrame()
                    {
                        IsAudio = 1,
                        IsKeyFrame = 1,
                        Size = entity.Length,
                        Channel = 1,
                        Frequency = 32000,
                        AudioFormat = 2,
                        NTimetick = ThreadEx.TickCount,
                        Offset = 0,
                        Encoder = MediaFrame.AAC_Encoder,
                        Ex = 1,
                        MediaFrameVersion = 0x01,
                    };
                    mf.SetData(entity.Buffer);
                    //if (mf.nIsKeyFrame == 1)
                    //    mf.nEx = 0;
                    mf.StreamID = (short)entity.Index;//区分俩路音频数据
                    streamInfo.IsFirstAudioFrame = false;
                    return mf;
                }
                catch (Exception ex)
                {

                    throw ex;
                }

            }
            else
            {
                throw new Exception("流类型错误");
            }
        }

        public static MediaFrameEntity MediaFrame2MediaSteamEntity(MediaFrame frame)
        {

            var result = new MediaFrameEntity()
            {
                Buffer = frame.GetData(),
                Length = (ushort)frame.Size,
                EncodTime = frame.NTimetick,
                MediaType = frame.IsAudio == 1 ? MediaType.AudioES : MediaType.VideoES,
                SampleRate = 32000,
                FrameRate = 25,
                Width = (ushort)frame.Width,
                Height = (ushort)frame.Height,
                KeyFrame = (byte)(frame.IsAudio == 0 && frame.IsKeyFrame == 1 ? 1 : 0),
            };
            if (frame.IsAudio == 1 && frame.IsKeyFrame == 1)
            {
                result.SampleRate = (ushort)frame.Frequency;
            }
            if (frame.IsCommandMediaFrame())
            {
                result.Buffer = new byte[0];
                result.Length = 0;
            }
            return result;

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
            //if (sps_len > 0 && pps_len < 0)
            //{
            //    sps_len = 0xf;
            //    pps_len = 4;
            //}
            if (sps_len < 0 || pps_len < 0)
            {
                sps_len = 11;
                pps_len = 4;
            }
            byte[] sps = new byte[sps_len];
            byte[] pps = new byte[pps_len];

            System.Buffer.BlockCopy(enc, 4, sps, 0, sps_len);
            System.Buffer.BlockCopy(enc, sps_len + 4 + 4, pps, 0, pps_len);

            return new byte[][] { sps, pps };
        }

        public static byte[] GetMediaFrameEntityBytes(MediaFrameEntity entity)
        {
            var ms = new MemoryStream();
            var bw = new BinaryWriter(ms);
            bw.Write(new byte[4]);

            bw.Write((byte)entity.MediaType);
            bw.Write(entity.Ex);
            bw.Write(entity.EncodTime);

            bw.Write(entity.Index);
            bw.Write(entity.Width);
            bw.Write(entity.Height);
            bw.Write(entity.FrameRate);
            bw.Write(entity.KeyFrame);
            bw.Write(entity.SampleRate);
            bw.Write(entity.Length);
            bw.Write(entity.Buffer);
            bw.Seek(0, SeekOrigin.Begin);
            bw.Write((int)(ms.Length - 4));
            var r = ms.ToArray();
            return r;
        }

        public static MediaFrameEntity GetMediaFrameEntity(byte[] bytes)
        {
            var ms = new MemoryStream(bytes);
            var br = new BinaryReader(ms);
            MediaFrameEntity entity = new MediaFrameEntity();
            entity.MediaType = (MediaType)br.ReadByte();
            entity.Ex = br.ReadInt32();
            entity.EncodTime = br.ReadInt64();

            entity.Index = br.ReadInt32();
            entity.Width = br.ReadUInt16();
            entity.Height = br.ReadUInt16();
            entity.FrameRate = br.ReadUInt16();
            entity.KeyFrame = br.ReadByte();
            entity.SampleRate = br.ReadUInt16();
            entity.Length = br.ReadInt32();
            entity.Buffer = br.ReadBytes(entity.Length);

            return entity;
        }

    }



}
