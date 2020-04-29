#define ConvertAVFrame
using Helpers;
using System;
using System.Runtime.InteropServices;
using System.Threading;


namespace GB28181.WinTool.Codec
{


    public partial class FFImp {
        private static object _lock = new object();
        public IntPtr pAVObj = IntPtr.Zero;
        protected AVCodecCfg cfg = null;
        protected bool isDec = false;
        protected IntPtr pOutBuf = IntPtr.Zero;
        protected FFScale _ffscale = null;
        private bool _isReleased = false;
        public FFImp(AVCodecCfg cfg, bool isDec,bool isvideo=true) {
            this.isDec = isDec;
            this.cfg = cfg;
            var pcfg = FunctionEx.StructToIntPtr(cfg);
            pAVObj = FunctionEx.StructToIntPtr(new AVModel());
            lock (_lock) {
                if (!ffimp_init()) {
                    throw new Exception("ffimp init error");
                }
            }
            if (isvideo)
            {
                int pOutBufSize = cfg.width * cfg.height * 3;
                if (pOutBufSize == 0)
                    pOutBufSize = 1920 * 1080 * 4;
                pOutBuf = Marshal.AllocHGlobal(pOutBufSize);
                int init_r = 0;
                lock (_lock)
                {
                    if (this.isDec)
                        init_r = ffimp_video_decode_init(ref pAVObj, pcfg, pOutBuf, pOutBufSize);
                    else
                        init_r = ffimp_video_encode_init(ref pAVObj, pcfg, pOutBuf, pOutBufSize);

                    _ffscale = new FFScale(cfg.width, cfg.height, 0, 12, cfg.width, cfg.height, 0, 12);

                }
            }
            else
            {
                int pOutBufSize = cfg.width * cfg.height * 3;
                if (pOutBufSize == 0)
                    pOutBufSize = 2048*2;
                pOutBuf = Marshal.AllocHGlobal(pOutBufSize);
                int init_r = 0;
                lock (_lock)
                {
                    if (this.isDec)
                        init_r = ffimp_audio_decode_init(ref pAVObj, pcfg, pOutBuf, pOutBufSize);
                    else
                        init_r = ffimp_audio_decode_init(ref pAVObj, pcfg, pOutBuf, pOutBufSize);

                  //  _ffscale = new FFScale(cfg.width, cfg.height, 0, 12, cfg.width, cfg.height, 0, 12);

                }
            }
            Marshal.FreeHGlobal(pcfg);
        }

        public int VideoDec(byte[] inData, ref IntPtr frame) {

        
            lock (this) {
                if (_isReleased)
                    return 0;

                IntPtr pFrame = IntPtr.Zero;
                var decFrameLen = 0;

                decFrameLen = ffimp_video_decode(pAVObj, inData, inData.Length, ref frame);

                return decFrameLen;
            }
        }

        public byte[] VideoDec(byte[] inData, byte[] outData) {

            lock (this) {

                if (_isReleased)
                    return new byte[0];

                IntPtr pFrame = IntPtr.Zero;
                int len = VideoDec(inData, ref pFrame);
                if (len > 0) {


#if ConvertAVFrame
                    _ffscale.ConvertAVFrame(pFrame, outData);
#else
                ffimp_YUVFrame2Buff(pAVObj, pFrame, outData);
#endif


                    return outData;
                } else {
                    return null;
                }
            }
        }

        public byte[] VideoDec(byte[] inData) {
            byte[] outData = new byte[cfg.width * cfg.height * 3 / 2];
           return VideoDec(inData, outData);
        }
         
        public int VideoEnc(byte[] inData, ref byte[] outData) {
  
            lock (this) {
                if (_isReleased)
                    return 0;

                var pInData = FunctionEx.BytesToIntPtr(inData);

                IntPtr frame = IntPtr.Zero;

                if (cfg.pix_fmt == (int)PixelFormat.PIX_FMT_YUV420P)
                    frame = ffimp_YUVBuff2YUVAVFrame1(pAVObj, pInData, inData.Length);
                else if (cfg.pix_fmt == (int)PixelFormat.PIX_FMT_RGB565)
                    frame = ffimp_RGBBuff2YUVAVFrame1(pAVObj, pInData, inData.Length);
                else
                    frame = ffimp_FMTBuff2YUVAVFrame1(pAVObj, cfg.pix_fmt, pInData, inData.Length);

                var encFrameBuffLen = cfg.width * cfg.height * 3;
                var encFrameBuff = Marshal.AllocHGlobal(encFrameBuffLen);


                var encSize = ffimp_video_encode(pAVObj, encFrameBuff, encFrameBuffLen, frame);
                outData = FunctionEx.IntPtrToBytes(encFrameBuff, 0, encSize);

                Marshal.FreeHGlobal(pInData);
                Marshal.FreeHGlobal(encFrameBuff);
                return encSize;
            }

        }

        public int VideoEnc(IntPtr frame, ref byte[] outData) {
            lock (this) {
                if (_isReleased)
                    return 0;
                var encFrameBuffLen = cfg.width * cfg.height * 3;
                var encFrameBuff = Marshal.AllocHGlobal(encFrameBuffLen);
                var encSize = ffimp_video_encode(pAVObj, encFrameBuff, encFrameBuffLen, frame);
                if (encSize > 0)
                    outData = FunctionEx.IntPtrToBytes(encFrameBuff, 0, encSize);
                Marshal.FreeHGlobal(encFrameBuff);
                return encSize;
            }

        }

        public int AudioEnc(byte[] inData, ref byte[] outData) {
            lock (this) {
                if (_isReleased)
                    return 0;
                var pInData = FunctionEx.BytesToIntPtr(inData);

                BFrame bframe = new BFrame();
                bframe.Buff = FunctionEx.BytesToIntPtr(inData);
                bframe.Size = inData.Length;
                IntPtr pbframe = FunctionEx.StructToIntPtr(bframe);
                var encFrameBuffLen = 192000;
                var encFrameBuff = Marshal.AllocHGlobal(encFrameBuffLen);
 
                var encSize = ffimp_audio_encode(pAVObj, encFrameBuff, encFrameBuffLen, pbframe);
                if (encSize == -1) return encSize;
                outData = FunctionEx.IntPtrToBytes(encFrameBuff, 0, encSize);

                Marshal.FreeHGlobal(pInData);
                Marshal.FreeHGlobal(encFrameBuff);
                return encSize;
            }

        }
        static AutoResetEvent are = new AutoResetEvent(true);
        public int AudioDec(byte[] inData, ref byte[] outData) {
            //lock (this) {
            
                if (_isReleased)
                    return 0;

                // var size = System.Diagnostics.Process.GetCurrentProcess().PrivateMemorySize64;

                BFrame bframe = new BFrame();
                //bframe.Buff = FunctionEx.BytesToIntPtr(inData);
                //bframe.Size = inData.Length;
                // IntPtr pbframe = FunctionEx.StructToIntPtr(bframe);
                IntPtr pbframe = IntPtr.Zero;
              
                var decSize = ffimp_audio_decode(pAVObj, inData, inData.Length, ref pbframe);
                bframe = FunctionEx.IntPtrToStruct<BFrame>(pbframe, 0, Marshal.SizeOf(bframe));
                outData = FunctionEx.IntPtrToBytes(bframe.Buff, 0, bframe.Size);
              
                return bframe.Size;
                //}
            
        }

        public void GeSize(ref int width, ref int height) {
            lock (this) {
                if (_isReleased)
                    return  ;
                ffimp_video_getSize(pAVObj, ref width, ref height);
            }
        }

        public void Release()
        {
            //lock (this)
            //{
                _isReleased = true;
                try
                {

                    var avmode = new AVModel()
                    {
                        context = FunctionEx.IntPtrToStruct<AVModel>(pAVObj).context,
                    };

                    lock (this)
                        ffimp_free_avobj(FunctionEx.StructToIntPtr(avmode));

                    avmode = new AVModel()
                    {
                        codec = FunctionEx.IntPtrToStruct<AVModel>(pAVObj).codec,
                    };

                    lock (this)
                        ffimp_free_avobj(FunctionEx.StructToIntPtr(avmode));
                    //ffimp_free_avobj(pAVObj);
                    if (_ffscale != null)
                        _ffscale.Release();
                    Marshal.FreeHGlobal(pOutBuf);

                }
                catch (Exception e)
                {
                    Console.WriteLine("ffimp error:{0}", e);
                }
            //}
        }
    }


    public struct BFrame {
        public IntPtr Buff;
        public int Size;
    }

 
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class AVModel {
        public IntPtr codec;
        public IntPtr context;
        public IntPtr vframe;
        public IntPtr aframe;
        public IntPtr picture;
        public IntPtr avframe;
        public AVCodecCfg cfg;
        public IntPtr pic2;
        //public IntPtr avpacket;
    }

    public partial class FFImp {
        [DllImport(DLLFile, CallingConvention = CallingConvention.Cdecl)]
        public extern static IntPtr a1(IntPtr rgbdata);


        const string DLLFile = @"newcjj.dll";

        [DllImport(DLLFile, CallingConvention = CallingConvention.Cdecl)]
        public extern static bool ffimp_init();
        //视频
        [DllImport(DLLFile, CallingConvention = CallingConvention.Cdecl)]
        public extern static int ffimp_video_decode_init(ref IntPtr pAVObj, IntPtr pAVCodecCfg, IntPtr pBuf, int size);
        [DllImport(DLLFile, CallingConvention = CallingConvention.Cdecl)]
        public extern static int ffimp_video_encode_init(ref IntPtr pAVObj, IntPtr pAVCodecCfg, IntPtr pBuf, int size);
        [DllImport(DLLFile, CallingConvention = CallingConvention.Cdecl)]
        public extern static int ffimp_video_decode(IntPtr pAVObj, IntPtr inBuff, int buffSize, ref IntPtr pFrame);
        [DllImport(DLLFile, CallingConvention = CallingConvention.Cdecl)]
        public extern static int ffimp_video_decode(IntPtr pAVObj, byte[] inBuff, int buffSize, ref IntPtr pFrame);

        [DllImport(DLLFile, CallingConvention = CallingConvention.Cdecl)]
        public extern static int ffimp_video_encode(IntPtr pAVObj, IntPtr outBuff, int buffSize, IntPtr frame);
        [DllImport(DLLFile, CallingConvention = CallingConvention.Cdecl)]
        public extern static int ffimp_video_decode1(IntPtr pAVObj, IntPtr inBuff, int inBuffSize, IntPtr outBuff, ref int outBuffSize);
        [DllImport(DLLFile, CallingConvention = CallingConvention.Cdecl)]
        public extern static int ffimp_video_getSize(IntPtr pAVObj, ref int width, ref int height);

        //音频
        [DllImport(DLLFile, CallingConvention = CallingConvention.Cdecl)]
        public extern static int ffimp_audio_decode_init(ref IntPtr pAVObj, IntPtr pAVCodecCfg, IntPtr pBuf, int size);
        [DllImport(DLLFile, CallingConvention = CallingConvention.Cdecl)]
        public extern static int ffimp_audio_encode_init(ref IntPtr pAVObj, IntPtr pAVCodecCfg, IntPtr pBuf, int size);
        [DllImport(DLLFile, CallingConvention = CallingConvention.Cdecl)]
        public extern static int ffimp_audio_decode(IntPtr obj, IntPtr inBuff, int inBuffSize, ref IntPtr pFrame);

        [DllImport(DLLFile, CallingConvention = CallingConvention.Cdecl)]
        public extern static int ffimp_audio_decode(IntPtr obj, byte[] inBuff, int inBuffSize, ref IntPtr pFrame);
 

        [DllImport(DLLFile, CallingConvention = CallingConvention.Cdecl)]
        public extern static int ffimp_audio_encode(IntPtr obj, IntPtr outBuff, int outBuffSize, IntPtr pFrame);
 
        [DllImport(DLLFile, CallingConvention = CallingConvention.Cdecl)]
        public extern static int ffimp_free_avobj(IntPtr obj);


        [DllImport(DLLFile, CallingConvention = CallingConvention.Cdecl)]
        public extern static int ffimp_YUVFrame2Buff(IntPtr obj, IntPtr pFrame, IntPtr outBuff);

        [DllImport(DLLFile, CallingConvention = CallingConvention.Cdecl)]
        public extern static int ffimp_YUVFrame2Buff(IntPtr obj, IntPtr pFrame, byte[] outBuff);

        [DllImport(DLLFile, CallingConvention = CallingConvention.Cdecl)]
        public extern static int ffimp_YUVFrame2RGBBuff(IntPtr obj, IntPtr pFrame, IntPtr outBuff);

        [DllImport(DLLFile, CallingConvention = CallingConvention.Cdecl)]
        public extern static IntPtr ffimp_YUVBuff2YUVAVFrame1(IntPtr pAVObj, IntPtr inBuff, int inBuffLen);

        [DllImport(DLLFile, CallingConvention = CallingConvention.Cdecl)]
        public extern static IntPtr ffimp_RGBBuff2YUVAVFrame1(IntPtr pAVObj, IntPtr inBuff, int inBuffLen);
        [DllImport(DLLFile, CallingConvention = CallingConvention.Cdecl)]
        public extern static IntPtr ffimp_FMTBuff2YUVAVFrame1(IntPtr pAVObj, int fmt, IntPtr inBuff, int inBuffLen);

        [DllImport(DLLFile, CallingConvention = CallingConvention.Cdecl)]
        public extern static IntPtr ffimp_set_sdl_bmp(IntPtr pAVObj, IntPtr frame, IntPtr bmp);
    }

    public enum PixelFormat {
        PIX_FMT_NONE = -1,
        PIX_FMT_YUV420P,   ///< Planar YUV 4:2:0, 12bpp, (1 Cr & Cb sample per 2x2 Y samples)
        PIX_FMT_YUYV422,   ///< Packed YUV 4:2:2, 16bpp, Y0 Cb Y1 Cr
        PIX_FMT_RGB24,     ///< Packed RGB 8:8:8, 24bpp, RGBRGB...
        PIX_FMT_BGR24,     ///< Packed RGB 8:8:8, 24bpp, BGRBGR...
        PIX_FMT_YUV422P,   ///< Planar YUV 4:2:2, 16bpp, (1 Cr & Cb sample per 2x1 Y samples)
        PIX_FMT_YUV444P,   ///< Planar YUV 4:4:4, 24bpp, (1 Cr & Cb sample per 1x1 Y samples)
        PIX_FMT_RGB32,     ///< Packed RGB 8:8:8, 32bpp, (msb)8A 8R 8G 8B(lsb), in cpu endianness
        PIX_FMT_YUV410P,   ///< Planar YUV 4:1:0,  9bpp, (1 Cr & Cb sample per 4x4 Y samples)
        PIX_FMT_YUV411P,   ///< Planar YUV 4:1:1, 12bpp, (1 Cr & Cb sample per 4x1 Y samples)
        PIX_FMT_RGB565,    ///< Packed RGB 5:6:5, 16bpp, (msb)   5R 6G 5B(lsb), in cpu endianness
        PIX_FMT_RGB555,    ///< Packed RGB 5:5:5, 16bpp, (msb)1A 5R 5G 5B(lsb), in cpu endianness most significant bit to 0
        PIX_FMT_GRAY8,     ///<        Y        ,  8bpp
        PIX_FMT_MONOWHITE, ///<        Y        ,  1bpp, 0 is white, 1 is black
        PIX_FMT_MONOBLACK, ///<        Y        ,  1bpp, 0 is black, 1 is white
        PIX_FMT_PAL8,      ///< 8 bit with PIX_FMT_RGB32 palette
        PIX_FMT_YUVJ420P,  ///< Planar YUV 4:2:0, 12bpp, full scale (jpeg)
        PIX_FMT_YUVJ422P,  ///< Planar YUV 4:2:2, 16bpp, full scale (jpeg)
        PIX_FMT_YUVJ444P,  ///< Planar YUV 4:4:4, 24bpp, full scale (jpeg)
        PIX_FMT_XVMC_MPEG2_MC,///< XVideo Motion Acceleration via common packet passing(xvmc_render.h)
        PIX_FMT_XVMC_MPEG2_IDCT,
        PIX_FMT_UYVY422,   ///< Packed YUV 4:2:2, 16bpp, Cb Y0 Cr Y1
        PIX_FMT_UYYVYY411, ///< Packed YUV 4:1:1, 12bpp, Cb Y0 Y1 Cr Y2 Y3
        PIX_FMT_BGR32,     ///< Packed RGB 8:8:8, 32bpp, (msb)8A 8B 8G 8R(lsb), in cpu endianness
        PIX_FMT_BGR565,    ///< Packed RGB 5:6:5, 16bpp, (msb)   5B 6G 5R(lsb), in cpu endianness
        PIX_FMT_BGR555,    ///< Packed RGB 5:5:5, 16bpp, (msb)1A 5B 5G 5R(lsb), in cpu endianness most significant bit to 1
        PIX_FMT_BGR8,      ///< Packed RGB 3:3:2,  8bpp, (msb)2B 3G 3R(lsb)
        PIX_FMT_BGR4,      ///< Packed RGB 1:2:1,  4bpp, (msb)1B 2G 1R(lsb)
        PIX_FMT_BGR4_BYTE, ///< Packed RGB 1:2:1,  8bpp, (msb)1B 2G 1R(lsb)
        PIX_FMT_RGB8,      ///< Packed RGB 3:3:2,  8bpp, (msb)2R 3G 3B(lsb)
        PIX_FMT_RGB4,      ///< Packed RGB 1:2:1,  4bpp, (msb)2R 3G 3B(lsb)
        PIX_FMT_RGB4_BYTE, ///< Packed RGB 1:2:1,  8bpp, (msb)2R 3G 3B(lsb)
        PIX_FMT_NV12,      ///< Planar YUV 4:2:0, 12bpp, 1 plane for Y and 1 for UV
        PIX_FMT_NV21,      ///< as above, but U and V bytes are swapped

        PIX_FMT_RGB32_1,   ///< Packed RGB 8:8:8, 32bpp, (msb)8R 8G 8B 8A(lsb), in cpu endianness
        PIX_FMT_BGR32_1,   ///< Packed RGB 8:8:8, 32bpp, (msb)8B 8G 8R 8A(lsb), in cpu endianness

        PIX_FMT_GRAY16BE,  ///<        Y        , 16bpp, big-endian
        PIX_FMT_GRAY16LE,  ///<        Y        , 16bpp, little-endian
        PIX_FMT_YUV440P,   ///< Planar YUV 4:4:0 (1 Cr & Cb sample per 1x2 Y samples)
        PIX_FMT_YUVJ440P,  ///< Planar YUV 4:4:0 full scale (jpeg)
        PIX_FMT_YUYVJ422,   ///< Packed YUV 4:2:2, 16bpp, Y0,Cb, Y1, Cr
        PIX_FMT_YUVA420P,  ///< Planar YUV 4:2:0, 20bpp, (1 Cr & Cb sample per 2x2 Y & A samples)
        PIX_FMT_NB,        ///< number of pixel formats, DO NOT USE THIS if you want to link with shared libav* because the number of formats might differ between versions

    }
}
