using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace StreamingKit.Codec
{

    //编码配置基类
    public class CodecCfgBase
    {
        public int encoder = -1;// 通用编码代号
        public string encodeName = null;// 通用编码名称

        public Dictionary<string, object> Params { get; set; } = new Dictionary<string, object>();

        // 设置编码名称

        public void SetEncoder(string name)
        {
            name = name.ToUpper();
            encodeName = name;
            encoder = GetGeneralEncoder(name);
        }

        // 获取通用编码代号
        public static int GetGeneralEncoder(string name)
        {
            byte[] buf = Encoding.UTF8.GetBytes(name);
            return BitConverter.ToInt32(buf, 0);
        }

        // 获取通用的编码名称
        public static string GetGeneralEncodecName(int generalEncoder)
        {
            var buf = BitConverter.GetBytes(generalEncoder);
            return BitConverter.ToString(buf);
        }
    }


    public class VideoEncodeCfg : CodecCfgBase
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public int FrameRate { get; set; }
        public int CameraId { get; set; } = -1;// 0为后置摄像头，1为前置摄像头
        public int VideoBitRate { get; set; } = 1024 * 640;

        public string ProfileLevel { get; set; }
        public string StrSPS { get; set; }
        public string StrPPS { get; set; }

        private byte[] sPS;

        public byte[] GetSPS()
        {
            return sPS;
        }

        public void SetSPS(byte[] value)
        {
            sPS = value;
        }

        private byte[] pPS;

        public byte[] GetPPS()
        {
            return pPS;
        }

        public void SetPPS(byte[] value)
        {
            pPS = value;
        }


        // 获取H264的SPS PPS
        public byte[] GetSPSPPSBytes()
        {
            if (GetPPS() == null || GetSPS() == null || ProfileLevel == null)
                throw new Exception();
            var ms = new MemoryStream();
            var baoStream = new BinaryWriter(ms);

            baoStream.Write(new byte[] { 0, 0, 0, 1 });
            baoStream.Write(GetSPS());
            baoStream.Write(new byte[] { 0, 0, 0, 1 });
            baoStream.Write(GetPPS());
            return ms.ToArray();
        }



    }



    public class AudioEncodeCfg : CodecCfgBase
    {
        public int MicId { get; set; }  = 0;
        public int Frequency { get; set; } = 8000;// 采样
        public int Format { get; set; } = 16;// 位元
        public int Channel { get; set; } = 2;// 通道模式
        public int Samples { get; set; } = 160;// 这个参数设置小了可以降底延迟
        public int KeyFrameRate { get; set; } = 50;// 关键帧间隔
        public int Bitrate { get; set; } = 32000;// 比特率



        public static AudioEncodeCfg GetDefault()
        {
            var r = new AudioEncodeCfg
            {
                Frequency = 32000,
                Format = 16,
                Channel = 1,
                Samples = 1024 * 2,
                MicId = 0
            };
            r.SetEncoder("AAC_");
            return r;
        }
    }
}
