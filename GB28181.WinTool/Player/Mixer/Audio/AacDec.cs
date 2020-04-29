using GB28181.WinTool.Codec;
using StreamingKit;
using System;
using System.IO;

namespace GB28181.WinTool.Mixer.Audio
{
    internal class AacDec : DecoderLine
    {
        private bool _inited = false;
        private byte[] buffer;
        FFImp _aac;
       // FaadImp _aac;
        public override void  Dec(byte[] src)
        {
            this.Init(src);
            if (_aac != null)
            {
                buffer = DecMultiAAC(src);
                if (buffer == null)
                    return;
               
                if (buffer.Length == 0)
                    Console.WriteLine(Buffer.Length + DateTime.Now.ToString());
                if (this.buffer.Length != 0)
                {
                    this.QueueBuffer.Enqueue(buffer);
                }
            }
        }
        private byte[] DecMultiAAC(byte[] buffer)
        {
            AAC_ADTS[] aacs = null;
            try
            {
                aacs = AAC_ADTS.GetMultiAAC(buffer);
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex);
                aacs = null;
            }
            if (aacs != null)
            {
                if (aacs.Length == 1)
                {
                    return DecAAC(buffer);
                }
                else
                {
                    return DecMultiAAC(aacs);
                }
            }
            else
            {
                return null;
            }
        }
        private byte[] DecMultiAAC(AAC_ADTS[] aacs)
        {
            var ms = new MemoryStream();
            foreach (var item in aacs)
            {
                if (item != null)
                {
                    var bytes = DecAAC(item.FrameData);
                    ms.Write(bytes, 0, bytes.Length);
                }
            }
            return ms.ToArray();
        }

        private byte[] DecAAC(byte[] buffer)
        {
            byte[] @out = new byte[0];
          // return _aac.Decode(buffer);
            int len = _aac.AudioDec(buffer, ref @out);
            if (len > 0)
            {
                if (len != @out.Length)
                    Array.Resize<byte>(ref @out, len);
                return @out;
            }
            else
            {
                return new byte[0];
            }
        }
        private void Init(byte[] buffer)
        {
            if (!_inited)
            {
                AAC_ADTS[] adtss = AAC_ADTS.GetMultiAAC(buffer);

                if (adtss != null)
                {
                    int channels = adtss[0].MPEG_4_Channel_Configuration;
                    int frequency = adtss[0].Frequency;
                    //_aac = new FaadImp();
                    _aac = new FFImp(AVCodecCfg.CreateAudio(channels, frequency, (int)AVCode.CODEC_ID_AAC), true,false);
                    _inited = true;
                }
            }
        }

        public  override void Close()
        {
            if (_aac != null)
            {
               // _aac.Dispose();
                _aac.Release();
            }
        }
    }
}
