using System;
using System.Collections.Generic;

namespace SLW.ClientBase.Mixer.Audio
{
    internal abstract class DecoderLine
    {
        private byte[] _Buffer = Array.Empty<byte>();
        public byte[] Buffer
        {
            get { return _Buffer; }
            set => _Buffer = value ?? Array.Empty<byte>();
        }

        public Queue<byte[]> QueueBuffer { get; set; } = new Queue<byte[]>();

        public int AudioType { get; set; }
        public int ID { get; set; }

        public abstract void Dec(byte[] src);

        public static DecoderLine Create(int audioType)
        {
            return new AacDec();
        }

        public virtual void Close()
        {

        }

    }
}
