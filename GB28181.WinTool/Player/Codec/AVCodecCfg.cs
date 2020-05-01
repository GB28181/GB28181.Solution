using StreamingKit;
using System.Runtime.InteropServices;

namespace GB28181.WinTool.Codec
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
            var r = new AVCodecCfg
            {
                codec_id = codec_id,
                width = width,
                height = height,
                codec_type = (int)AVCode.CODEC_TYPE_VIDEO,
                bit_rate = bit_rate,
                time_base_den = 15,
                time_base_num = 2,
                gop_size = 15,
                pix_fmt = (int)AVCode.PIX_FMT_YUV420P
            };
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


    public class VideoEncodeCfg : StreamingKit.Codec.VideoEncodeCfg {
       
        public IYUVDraw Draw { get; set; }
 

        public static VideoEncodeCfg GetDefaule(IYUVDraw _Draw = null) {
            VideoEncodeCfg encCfg = new VideoEncodeCfg
            {
                VideoBitRate = 256 * 1000,
                FrameRate = 10,
                Height = 240,
                Width = 320,
                CameraId = 0,
                Draw = _Draw
            };

            encCfg.SetEncoder("H264");
            return encCfg;
        }

    }


    public class AudioEncodeCfg : StreamingKit.Codec.AudioEncodeCfg
    {

    }


}
