using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GB28181.WinTool.Mixer.Audio
{
    internal abstract class DecoderLine
    {

        public int ID;
        /// <summary>
        /// 
        /// </summary>
        public int AudioType;
        private byte[] _Buffer = new byte[0];
        public byte[] Buffer
        {
            get { return _Buffer; }
            set
            {
                if (value == null)
                    _Buffer = new byte[0];
                else
                    _Buffer = value;
            }
        }

        public Queue<byte[]> QueueBuffer
        {
            get
            {
                return queueBuffer;
            }

            set
            {
                queueBuffer = value;
            }
        }

        private Queue<byte[]> queueBuffer = new Queue<byte[]>();

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
