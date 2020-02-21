using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Win.MediaNetEngine.Codec
{

    //编码配置基类
    public class CodecCfgBase {
        public int encoder = -1;// 通用编码代号
        public String encodeName = null;// 通用编码名称

        public Dictionary<String, Object> Params = new Dictionary<String, Object>();

        // 设置编码名称

        public void SetEncoder(String name) {
            name = name.ToUpper();
            encodeName = name;
            encoder = GetGeneralEncoder(name);
        }

        // 获取通用编码代号
        public static int GetGeneralEncoder(String name) {
            byte[] buf = Encoding.UTF8.GetBytes(name);
            return BitConverter.ToInt32(buf,0);
        }

        // 获取通用的编码名称
        public static String GetGeneralEncodecName(int generalEncoder) {
            byte[] buf = BitConverter.GetBytes(generalEncoder);
            return BitConverter.ToString(buf);
        }
    }


    public class VideoEncodeCfg : CodecCfgBase {
        public int width;
        public int height;
        public int frameRate;
        public int cameraId = -1;// 0为后置摄像头，1为前置摄像头
        public int videoBitRate = 1024 * 640;
        public byte[] SPS;
        public byte[] PPS;
        public String profileLevel;
        public String strSPS;
        public String strPPS;
     

        // 获取H264的SPS PPS
        public byte[] GetSPSPPSBytes() {
            if (this.PPS == null || this.SPS == null || this.profileLevel == null)
                throw new Exception();
            var ms = new MemoryStream();
            var baoStream = new BinaryWriter(ms);
 
            baoStream.Write(new byte[] { 0, 0, 0, 1 });
            baoStream.Write(SPS);
            baoStream.Write(new byte[] { 0, 0, 0, 1 });
            baoStream.Write(PPS);
            byte[] pps_sps = ms.ToArray();
            return pps_sps;
        }


      
    }



    public class AudioEncodeCfg : CodecCfgBase {
        public int micId = 0;
        public int frequency = 8000;// 采样
        public int format = 16;// 位元
        public int channel = 2;// 通道模式
        public int samples = 160;// 这个参数设置小了可以降底延迟
        public int keyFrameRate = 50;// 关键帧间隔
        public int bitrate = 32000;// 比特率



        public static AudioEncodeCfg GetDefault() {
            AudioEncodeCfg r = new AudioEncodeCfg();
            r.SetEncoder("AAC_");
            r.frequency = 32000;
            r.format = 16;
            r.channel = 1;
            r.samples = 1024*2;
            r.micId = 0;
            return r;
        }
    }
}
