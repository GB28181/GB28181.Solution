using System;
using System.Runtime.InteropServices;


namespace GB28181.WinTool.Codec
{
    public class FFScale {

        private int _handle = -1;
        private int _inWidth, _inHeight, _inPix, _inBits;
        private int _outWidth, _outHeight, _outPix, _outBits;
        private object _sync = new object();
        public static void Test() {
            try {
                FFScale s = new FFScale(320, 240, 29, 32, 320, 240, 0, 12);
                byte[] @in = new byte[320 * 240 * 4];
                byte[] @out = s.FormatS(@in);
                @out = s.FormatS(@in);
            } catch (Exception e) {

            }
        }

        /**
         * 
         * @param in_width
         * @param in_height
         * @param in_pix
         *            输入格式 对应ffmpeg中的AVPixelFormat
         * @param in_bits
         *            输入图片的位长，如16位，24位，32位
         * @param out_width
         * @param out_height
         * @param out_pix
         *            输出 格式，对应ffmpeg中的AVPixelFormat
         * @param out_bits
         *            输入图片的位长，如16位，24位，32位
         */
        public FFScale(int in_width, int in_height, int in_pix, int in_bits, int out_width, int out_height, int out_pix, int out_bits) {
            _inWidth = in_width;
            _inHeight = in_height;
            _inPix = in_pix;
            _inBits = in_bits;
            _outWidth = out_width;
            _outHeight = out_height;
            _outPix = out_pix;
            _outBits = out_bits;
            _handle = CreateSwsScale(in_width, in_height, in_pix, in_bits, out_width, out_height, out_pix, out_bits);

        }

        public byte[] FormatS(byte[] @in) {
            return Convert(@in);
        }

        public byte[] Convert(byte[] @in) {

            if (_inWidth == _outWidth && _inHeight == _outHeight && _inPix == _outPix && _inBits == _outBits)
                return @in;

            byte[] @out = new byte[_outWidth * _outHeight * _outBits / 8];
            lock (_sync)
                SwsScale(_handle, @in, _inWidth, _inHeight, _inPix, _inBits, @out, _outWidth, _outHeight, _outPix, _outBits);

            return @out;
        }
        public void Convert(byte[] @in, byte[] @out) {
            lock (_sync)
                SwsScale(_handle, @in, _inWidth, _inHeight, _inPix, _inBits, @out, _outWidth, _outHeight, _outPix, _outBits);
        }
        public void Convert(IntPtr @in, IntPtr @out) {

            if (_inWidth == _outWidth && _inHeight == _outHeight && _inPix == _outPix && _inBits == _outBits)
                @out = @in;
            lock (_sync)
                SwsScale(_handle, @in, _inWidth, _inHeight, _inPix, _inBits, @out, _outWidth, _outHeight, _outPix, _outBits);

        }
        public void Convert(IntPtr @in, ref byte[] @out) {
            lock (_sync)
                SwsScale(_handle, @in, _inWidth, _inHeight, _inPix, _inBits, @out, _outWidth, _outHeight, _outPix, _outBits);
        }

        public void ConvertAVFrame(IntPtr avframe, IntPtr @out) {
            lock (_sync)
                SwsScaleAVFrame(_handle, avframe, _inWidth, _inHeight, _inPix, _inBits, @out, _outWidth, _outHeight, _outPix, _outBits);
        }
        public void ConvertAVFrame(IntPtr avframe, byte[] @out) {
            lock (_sync)
                SwsScaleAVFrame(_handle, avframe, _inWidth, _inHeight, _inPix, _inBits, @out, _outWidth, _outHeight, _outPix, _outBits);
        }
        public void Release() {
            lock (_sync) {
                if (_handle != 0)
                    FreeSwsScale(_handle);
                _handle = 0;
            }
        }

        const string DLLFile = @"newcjj.dll";
        [DllImport(DLLFile, CallingConvention = CallingConvention.Cdecl)]
        private extern static int CreateSwsScale(int in_width, int in_height, int in_pix, int in_bits, int out_width, int out_height, int out_pix, int out_bits);
        [DllImport(DLLFile, CallingConvention = CallingConvention.Cdecl)]
        private extern static int FreeSwsScale(int handle);
        [DllImport(DLLFile, CallingConvention = CallingConvention.Cdecl)]
        private extern static void SwsScale(int handle, byte[] @in, int in_width, int in_height, int in_pix, int in_bits, byte[] @out, int out_width, int out_height, int out_pix, int out_bits);

        [DllImport(DLLFile, CallingConvention = CallingConvention.Cdecl)]
        private extern static void SwsScale(int handle, IntPtr pin, int in_width, int in_height, int in_pix, int in_bits, IntPtr pout, int out_width, int out_height, int out_pix, int out_bits);
        [DllImport(DLLFile, CallingConvention = CallingConvention.Cdecl)]
        private extern static void SwsScale(int handle, IntPtr pin, int in_width, int in_height, int in_pix, int in_bits, byte[] @out, int out_width, int out_height, int out_pix, int out_bits);

        [DllImport(DLLFile, CallingConvention = CallingConvention.Cdecl)]
        private extern static void SwsScaleAVFrame(int handle, IntPtr avFrame, int in_width, int in_height, int in_pix, int in_bits, IntPtr pout, int out_width, int out_height, int out_pix, int out_bits);
        [DllImport(DLLFile, CallingConvention = CallingConvention.Cdecl)]
        private extern static void SwsScaleAVFrame(int handle, IntPtr avFrame, int in_width, int in_height, int in_pix, int in_bits, byte[] @out, int out_width, int out_height, int out_pix, int out_bits);


    }
}
