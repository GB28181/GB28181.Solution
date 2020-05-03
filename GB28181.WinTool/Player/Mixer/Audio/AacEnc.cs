using GB28181.WinTool.Codec;
using System;

namespace GB28181.WinTool.Mixer.Audio
{

    public class AacEnc : IDisposable
    {
        private bool _isworking = false;

        private FaacImp _faacImp = null;
        private int _audioFrameIndex = 0;
        private int _frequency = 0;
        private int _channels = 0;

        private AudioEncodeCfg _audioCfg = null;
        private bool _isFirstKeyFrame = true;

        public AacEnc()
        {

            _faacImp = new FaacImp(1, 32000, 32000);
            //_faacImp = new FaacImp(1, 32000, 32000);
        }




        public byte[] Enc_AAC(byte[] buffer)
        {
            return _faacImp.Encode(buffer);
        }


        protected virtual void Dispose(bool disposeNative)
        {
            if (disposeNative)
            {
                _faacImp?.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
