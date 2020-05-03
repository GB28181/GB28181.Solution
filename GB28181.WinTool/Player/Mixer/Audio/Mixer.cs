using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
namespace GB28181.WinTool.Mixer.Audio
{
    public partial class MixerAudio:IDisposable
    {
        /// <summary>
        /// 混音器编码ID
        /// </summary>
        public int MixerID { private set; get; }

        private Action<int,byte[]> _callBack;

        Dictionary<int, DecoderLine> _dicSource = new Dictionary<int, DecoderLine>();

         AacEnc _Encorder=null ;
        public MixerAudio(Action<int,byte[]> callBack)
        {
            _callBack = callBack;
            if(_Encorder==null)
                _Encorder = new AacEnc();
            //thread = new Thread(new ThreadStart(MixThread));
            //thread.Start();
        }
        protected readonly object _syncRoot = new object();
     

        public void Write(int audioID, int audioType, byte[] srcBuf)
        {
            if (srcBuf.Length == 0)
            {
                Console.WriteLine("eree" + DateTime.Now.ToString());
                return;
            }
            if (srcBuf.Length < 7)
            {
                Console.WriteLine(srcBuf.Length);
                return;
            }
            lock (_syncRoot)
            {

                if (!_dicSource.ContainsKey(audioID))
                {
                    _dicSource[audioID] = DecoderLine.Create(audioType);
                    //_dicSource[audioID].Dec(srcBuf);
                }
                else
                {
                    if (_dicSource[audioID].QueueBuffer.Count < 30)
                    {
                        //_dicSource[audioID].Dec(srcBuf);
                    }
                    else
                    {
                        foreach(var item in _dicSource.Values)
                        {
                            item.Close();
                            Console.WriteLine("-------------------decode close:{0}----------------------------",_dicSource.Count);
                        }
                        _dicSource = new Dictionary<int, DecoderLine>();
                        _dicSource[audioID] = DecoderLine.Create(audioType);
                        //_dicSource[audioID].Dec(srcBuf);
                    }

                }
            }

            _dicSource[audioID].Dec(srcBuf);

            byte[] mixbuf;
            if (_dicSource.Count >= 1)
            {
                mixbuf = this.mix();
                if (mixbuf == null)
                    return;
                var aacbuf = _Encorder.Enc_AAC(mixbuf);
                if (_callBack != null)
                {
                   
                    _callBack(MixerID, aacbuf);
                    //if(wa.ElapsedMilliseconds>0)
                    //{
                    //    Console.WriteLine(wa.ElapsedMilliseconds);
                    //}
                }
                  
            }
        }

        private byte[] mix()
        {
            List<DecoderLine> srcList = new List<DecoderLine>();
            lock (_syncRoot)
            {
                foreach (var item in _dicSource.Values)
                {
                    if (item.QueueBuffer.Count > 0)
                        srcList.Add(item);
                }
            }

            //if (srcList.Count<= 1)
            //{
            //    if (srcList.Count == 1&& srcList[0].QueueBuffer.Count>10)
            //    {
            //        return srcList[0].QueueBuffer.Dequeue();
            //        //srcList[0].QueueBuffer.Clear();
            //    }
            //    return null;

            //}
            //else 
            if (srcList.Count<_dicSource.Count)
            {
                return null;
            }

            int mixed = srcList.Count;

            for (int mixIndex = 0; mixIndex < mixed; mixIndex++)
            {
                srcList[mixIndex].Buffer= srcList[mixIndex].QueueBuffer.Dequeue();
               
            }
            int firstLen = srcList[0].Buffer.Length;
            byte[] dstbuf = new byte[firstLen];
            int pcmsize = dstbuf.Length;

            for (int i = 0; i < pcmsize; i = i + 2)
            {
                int newPoint = new int();
                for (int mixIndex = 0; mixIndex < mixed; mixIndex++)
                {
                    if (srcList[mixIndex].Buffer.Length != 0)
                    {
                        newPoint += (short)(srcList[mixIndex].Buffer.ReadShort(i, Endianity.Small) / mixed);
                    }
                    if (newPoint < -32767)
                        newPoint = -32767;
                    if (newPoint > 32767)
                        newPoint = 32767;
                }
                dstbuf.Write(i, newPoint, Endianity.Small);
            }
            foreach (var item in srcList)
            {
                item.Buffer = null;
            }
            return dstbuf;

        }

        /// <summary>
        /// 重置混音器
        /// </summary>
        public void Reset()
        {
            lock (_syncRoot)
            {
                foreach (var item in _dicSource.Values)
                {
                    item.Close();
                }
                _dicSource.Clear();
            }
        }

        public void Dispose()
        {
            _Encorder.Dispose();
            foreach (var item in _dicSource.Values)
            {
                if (item != null)
                    item.Close();
            }
        }
    }
    public partial class MixerAudio
    {
        public static object _syncObj = new object();

        private static Dictionary<int, MixerAudio> _dicMixer = new Dictionary<int, MixerAudio>();
        public static MixerAudio Create(int id,Action<int,byte[]> callBack)
        {
            MixerAudio mixer;
            lock (_syncObj)
            {
                int index = _dicMixer.Count + 1;
                _dicMixer[index] = new MixerAudio(callBack);
                _dicMixer[index].MixerID = id;
                mixer = _dicMixer[index];
            }
            return mixer;
        }
        /// <summary>
        /// 移除混音器
        /// </summary>
        /// <param name="mixerID">混音器ID</param>
        public static void Remove(int mixerID)
        {
            lock (_syncObj)
            {
                if (_dicMixer.ContainsKey(mixerID))
                    _dicMixer.Remove(mixerID);
            }
        }

    }
}
