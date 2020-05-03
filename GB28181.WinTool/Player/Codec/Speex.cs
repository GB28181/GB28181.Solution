using Helpers;
using System;
using System.Runtime.InteropServices;


namespace GB28181.WinTool.Codec
{
    /// <summary>
    /// SPEEX编码器
    /// </summary>
    public partial class Speex : IDisposable
    {

        public IntPtr pSpx = IntPtr.Zero;
        private readonly int Samples = 160;
        private bool _isDisoseing = false;
        private bool _isDisosed = false;
        public Speex(int quality, int samples = 160)
        {
            this.Samples = samples;
            if (quality < 0 || quality > 10)
            {
                throw new Exception("quality value must be between 0 and 10.");
            }
            pSpx = SpeexOpen(quality);
        }

        //编码
        public byte[] Encode(byte[] data)
        {

            if (_isDisoseing || _isDisosed)
                return null;
            var pData = FunctionEx.BytesToIntPtr(data);
            var pOut = Marshal.AllocHGlobal(200);
            var encSize = Speex.SpeexEncode(this.pSpx, pData, pOut);
            byte[] buffer = FunctionEx.IntPtrToBytes(pOut, 0, encSize);
            Marshal.FreeHGlobal(pOut);
            Marshal.FreeHGlobal(pData);
            return buffer;
          
        }
        //解码
        public byte[] Decode(byte[] data)
        {
            if (_isDisoseing || _isDisosed)
                return null;
            var pData = FunctionEx.BytesToIntPtr(data);
            var pOut = Marshal.AllocHGlobal(Samples * 2);
            var decSize = SpeexDecode(this.pSpx, data.Length, pData, pOut);
            var bytes = FunctionEx.IntPtrToBytes(pOut, 0, decSize);
            Marshal.FreeHGlobal(pOut);
            Marshal.FreeHGlobal(pData);
            return bytes;
 
        }

        public byte[] Cancellation(byte[] play, byte[] mic)
        {
            if (_isDisoseing || _isDisosed)
                return null;
            var pPlay = FunctionEx.BytesToIntPtr(play);
            var pMic = FunctionEx.BytesToIntPtr(mic);
            var pOut = Marshal.AllocHGlobal(mic.Length);
            SpeexEchoCancellation(pSpx, pPlay, pMic, pOut);
            var data = FunctionEx.IntPtrToBytes(pOut, 0, mic.Length);
            Marshal.FreeHGlobal(pPlay);
            Marshal.FreeHGlobal(pMic);
            Marshal.FreeHGlobal(pOut);
            return data;
        }

        public void SpeexDenoise(int noiseSuppress = -25)
        {
            if (_isDisoseing || _isDisosed)
                return;
            SpeexDenoise(pSpx, noiseSuppress);
        }
        public void SpeexAGC(int level = 24000)
        {
            if (_isDisoseing || _isDisosed)
                return ;
            SpeexAGC(pSpx, level);
        }
        public void SpeexVAD(int vadProbStart = 80, int vadProbContinue = 65)
        {
            if (_isDisoseing || _isDisosed)
                return ;
            SpeexVAD(pSpx, vadProbStart, vadProbContinue);
        }


        protected virtual void Dispose(bool cleanNative)
        {

            _isDisoseing = true;
            if (cleanNative)
            {
                //clean native
                try
                {
                    SpeexClose(pSpx);
                }
                catch
                {
                }
                finally
                {
                    _isDisoseing = false;
                    _isDisosed = true;
                }
            }
            //clean managed

        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }

    public partial class Speex
    {
 
        const string DLLFile = @"speexNet.dll";

        [DllImport(DLLFile, CallingConvention = CallingConvention.Cdecl)]
       static extern IntPtr SpeexOpen(int quality);
        [DllImport(DLLFile, CallingConvention = CallingConvention.Cdecl)]
        static extern void SpeexClose(IntPtr pSpx);

        [DllImport(DLLFile, CallingConvention = CallingConvention.Cdecl)]
       static extern void SpeexDenoise(IntPtr pSpx, int noiseSuppress = -25);
        [DllImport(DLLFile, CallingConvention = CallingConvention.Cdecl)]
        static extern void SpeexAGC(IntPtr pSpx, float level = 24000);
        [DllImport(DLLFile, CallingConvention = CallingConvention.Cdecl)]
       static extern void SpeexVAD(IntPtr pSpx, int vadProbStart = 80, int vadProbContinue = 65);

        [DllImport(DLLFile, CallingConvention = CallingConvention.Cdecl)]
       static extern int SpeexEncode(IntPtr pSpx, IntPtr data, IntPtr output);
        [DllImport(DLLFile, CallingConvention = CallingConvention.Cdecl)]
       static extern int SpeexDecode(IntPtr pSpx, int nbBytes, IntPtr data, IntPtr output);
        [DllImport(DLLFile, CallingConvention = CallingConvention.Cdecl)]
       static extern void SpeexEchoCancellation(IntPtr pSpx, IntPtr play, IntPtr mic, IntPtr output);

        [DllImport(DLLFile, CallingConvention = CallingConvention.Cdecl)]
       static extern void SpeexEchoCapture(IntPtr pSpx, IntPtr mic, IntPtr output);
        [DllImport(DLLFile, CallingConvention = CallingConvention.Cdecl)]
       static extern void SpeexEchoPlayback(IntPtr pSpx, IntPtr play);
         


    }
}