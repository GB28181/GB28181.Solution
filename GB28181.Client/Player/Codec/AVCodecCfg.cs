using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using SLW.Media;
using System.IO;

namespace SLW.ClientBase.Codec
{
    /// <summary>
    /// 音视频编码参数设置
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class AVCodecCfg
    {
        public int codec_id;
        public int codec_type;
        public int bit_rate;
        public int width;
        public int height;
        public int time_base_den;
        public int time_base_num;
        public int gop_size;
        public int pix_fmt;
        public int max_b_frames;
        public int sample_rate;
        public int channels;
        public AVCodecCfg()
        {

        }
        public static AVCodecCfg CreateAudio(int channels, int sample_rate, int codec_id = (int)AVCode.CODEC_ID_H264, int bit_rate = 64000)
        {
            var r = new AVCodecCfg();
            r.codec_id = codec_id;
            r.codec_type = (int)AVCode.CODEC_TYPE_AUDIO;
            r.bit_rate = bit_rate;
            r.time_base_den = 0;
            r.time_base_num = 0;
            r.gop_size = 0;
            r.pix_fmt = 0;
            r.max_b_frames = 0;
            r.sample_rate = sample_rate;
            r.channels = channels;
            return r;
        }
        public static AVCodecCfg CreateVideo(int width, int height, int codec_id = (int)AVCode.CODEC_ID_H264, int bit_rate = 98000)
        {
            var r = new AVCodecCfg();
            r.codec_id = codec_id;
            r.width = width;
            r.height = height;
            r.codec_type = (int)AVCode.CODEC_TYPE_VIDEO;
            r.bit_rate = bit_rate;
            r.time_base_den = 15;
            r.time_base_num = 2;
            r.gop_size = 15;
            r.pix_fmt = (int)AVCode.PIX_FMT_YUV420P;
            return r;

        }

    }




    ////编码配置基类
    //public class CodecCfgBase {
    //    public int encoder = -1;// 通用编码代号
    //    public String encodeName = null;// 通用编码名称

    //    public Dictionary<String, Object> Params = new Dictionary<String, Object>();

    //    // 设置编码名称

    //    public void SetEncoder(String name) {
    //        name = name.ToUpper();
    //        encodeName = name;
    //        encoder = GetGeneralEncoder(name);
    //    }
         

    //    // 获取通用编码代号
    //    public static int GetGeneralEncoder(String name) {
    //        byte[] buf = Encoding.UTF8.GetBytes(name);
    //        return BitConverter.ToInt32(buf,0);
    //    }

    //    // 获取通用的编码名称
    //    public static String GetGeneralEncodecName(int generalEncoder) {
    //        byte[] buf = BitConverter.GetBytes(generalEncoder);
    //        return BitConverter.ToString(buf);
    //    }
    //}


    public class VideoEncodeCfg : SLW.Media.Codec.VideoEncodeCfg {
       
        public IYUVDraw Draw;
 

        public static VideoEncodeCfg GetDefaule(IYUVDraw _Draw = null) {
            VideoEncodeCfg encCfg = new VideoEncodeCfg();

            encCfg.SetEncoder("H264");
            encCfg.videoBitRate = 256 * 1000;
            encCfg.frameRate = 10;
            encCfg.height = 240;
            encCfg.width = 320;
            encCfg.cameraId = 0;
            encCfg.Draw = _Draw;
            return encCfg;
        }

    }



    public class AudioEncodeCfg : SLW.Media.Codec.AudioEncodeCfg {
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
            r.samples = 1024 * 2;
            r.micId = 0;
            return r;
        }
    }
}
