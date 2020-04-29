using GB28181.WinTool.Codec;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SS.ClientBase.Mixer.Audio
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

            _faacImp = new FaacImp(1, 32000,32000);
            //_faacImp = new FaacImp(1, 32000, 32000);
        }




        public byte[] Enc_AAC(byte[] buffer)
        {
            return _faacImp.Encode(buffer);
        }


        public void Dispose()
        {
            try
            {
                if (_faacImp != null)
                    _faacImp.Dispose();
            }
            catch (Exception e)
            {
            }
        }
    }
}
