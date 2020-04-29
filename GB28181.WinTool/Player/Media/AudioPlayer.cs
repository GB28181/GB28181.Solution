using Common.Generic;
using Helpers;
using GB28181.WinTool.Codec;
using StreamingKit;
using StreamingKit.Wave.Wave;
using StreamingKit.Media;
using System;
using System.IO;
using System.Threading;

namespace GB28181.WinTool.Media
{
    /// <summary>
    /// 音频播放器
    /// </summary>
    public abstract class AudioPlayer
    {
        protected long _syncPlayTime = 0;
        protected long _curPlayTime = 0;
        protected long _lastMediaFrameTime = 0;
        public long Position { get { return _curPlayTime; } }
        public virtual void Start() { 

        }
        public virtual void Stop() { 
        }

        public virtual void SyncPlayTime(long time) {
            _syncPlayTime = time;
        }

        public virtual long CurrentPlayTime() {
            return _curPlayTime;
        }
        /// <summary>
        /// 播放一帧
        /// </summary>
        /// <param name="frame"></param>
        public virtual void Play( MediaFrame frame) { }

        public virtual void Dispose() { }
    }
 
    /// <summary>
    /// Wave输出音频播放器
    /// </summary>
    public class WaveAudioPlayer : AudioPlayer, IDisposable {
        protected bool _isDisoseing = false;
        protected bool _isDisosed = false;
        protected bool _inited = false;
        protected bool _isworking = false;
        protected bool _isPaused = false;
        protected Thread _playThread = null;
        protected AQueue<MediaFrame> _queue = new AQueue<MediaFrame>();
 
        protected byte[] _outBuffer = new byte[1024 * 32];
        protected WaveOut _wave = null;
        protected Speex _speex = null;
        protected FFImp _aac = null;
        protected bool _reseting = false;
        public bool IsPlaying { get { return _inited; } }
        public Boolean BufferEmpty { get { return _queue.Count == 0; } }
        private float _speed = 1f;
      
        //播放速度，1为正常播放
        public float Speed { get { return _speed; } set { _speed = value; } }

        public int Volume {
            get {
                if (_wave == null) {
                    if (WaveOut.Devices == null || WaveOut.Devices.Length == 0)
                        return 100;

                    var wave = new WaveOut(WaveOut.Devices[0], 8000, 16, 1);
                    var result = wave.Volume;
                    return result;
                }else{

                    return _wave.Volume;
                }
            }
            set {
                if (_wave == null) {
                    if (WaveOut.Devices == null || WaveOut.Devices.Length == 0) return;
                    var wave = new WaveOut(WaveOut.Devices[0], 8000, 16, 1);
                    wave.Volume = value;
                } else {
                    _wave.Volume = value;
                }
            }
        }

        public event EventHandler<EventArgsEx<Exception>> Error;

        public WaveAudioPlayer() {


        }

        protected virtual void Init(MediaFrame frame) {
            if (!_inited && frame.IsKeyFrame == 1 && frame.IsAudio == 1) {
                if (frame.Encoder == MediaFrame.AAC_Encoder)
                    _aac = new FFImp(AVCodecCfg.CreateAudio(frame.Channel, frame.Frequency, (int)AVCode.CODEC_ID_AAC), true,false);
                if (frame.Encoder == MediaFrame.SPEXEncoder)
                    _speex = _speex ?? new Speex(4, 160);
                if (WaveOut.Devices == null || WaveOut.Devices.Length == 0) {

                } else {
                    _wave = new WaveOut(WaveOut.Devices[0], frame.Frequency, 16, frame.Channel);
                }
               
                _inited = true;
            }
        }
 
        public override void Start() {
            if (_isworking)
                return;
            _isworking = true;
            ResetPosition();
            _curPlayTime = 0;
            _playThread = ThreadEx.ThreadCall(PlayThread);
        }

        public override void Stop() {
            if (!_isworking)
                return;
            _isworking = false;
            ThreadEx.ThreadStop(_playThread);
            _queue.Clear();
            _curPlayTime = 0;
 
        }

        public void Pause() {
            _isPaused = true;
        }

        public void Continue() {
            _isPaused = false;
        }

        public void ResetPosition() {
            lock (_queue) {
                _queue.Clear();
                _curPlayTime = 0;
                _reseting = true;
            }
        }

        public override void Play(MediaFrame frame) {
            if (!_isworking)
                return;
            lock (_queue) {
                _queue.Enqueue(frame);
            }
            _lastMediaFrameTime = frame.NTimetick;
        }
        //System.IO.BinaryWriter bw = new BinaryWriter(new System.IO.FileStream(@"D:\\aac.aac", FileMode.Create));
        protected virtual void _Play(MediaFrame frame) {
            if (_isDisoseing || _isDisosed)
                return;

            if (!_inited && frame.IsKeyFrame == 1)
                Init(frame);
            if (!_inited)
                return;
            if (frame.IsCommandMediaFrame() && frame.GetCommandType() == MediaFrameCommandType.ResetCodec) {
                //在这里重置解码器,这里因为音频格式一般不会发生变化,所以暂时没有做处理
                return;
            }

            //string b = DateTime.Now.TimeOfDay + " begin  end:";
         
         
            byte[] buf = null;
            if (_aac != null) {
                buf = DecMultiAAC(frame.GetData());
            }
            if (_speex != null) {
                buf = DecSPEX(frame.GetData());
            }
            if (buf != null && buf.Length > 0) {
                _Play(buf);
            } else {
            }
       
            //Console.WriteLine(b + DateTime.Now.TimeOfDay + " : " + frame.Data.Length + " aac:" + buf.Length);
        }

        protected virtual void _Play(byte[] pcm) {
            if(_wave!=null)
                _wave.Play(pcm, 0, pcm.Length);
        }

        protected virtual void PlayThread() {
            MediaFrame frame = null;
            while (_isworking) {
                lock (_queue) {
                    if (_queue.Count > 0 && !_isPaused) {
                        frame = _queue.Dequeue();
                    } else {
                         frame  =null;
                    }
                }
                if (frame != null) {
                    if (Speed == 1) {
                        var sleep = (int)(frame.NTimetick - _syncPlayTime);

                        if (sleep < -3000)
                        {
                            lock (_queue)
                            {
                                _queue.Clear();
                            }
                        }
                        if (sleep > 0 && !_reseting)
                            Thread.Sleep(sleep);
                    } else {
                        var sleep = (int)(frame.NTimetick - _syncPlayTime);
                        if (sleep > 0 && !_reseting)
                            Thread.Sleep(sleep);
                    }
                    if (!_reseting) {
                        _Play(frame);
                        lock (_queue) {
                            if (_curPlayTime < frame.NTimetick && !_reseting)
                                _curPlayTime = frame.NTimetick;
                        }
                    }
                    _reseting = false;
                } else {
                    ThreadEx.Sleep(10);
                }
            }
        }

        private byte[] DecSPEX(byte[] buffer) {
            return _speex.Decode(buffer);
        }

        private byte[] DecMultiAAC(byte[] buffer) {
            var aacs = AAC_ADTS.GetMultiAAC(buffer);
            if (aacs != null) {
                if (aacs.Length == 1) {
                    return DecAAC(buffer);
                } else {
                    return DecMultiAAC(aacs);
                }
            } else {
                return new byte[0];
            }
        }
        
        private byte[] DecMultiAAC(AAC_ADTS[] aacs) {
            var ms = new MemoryStream();
            foreach (var item in aacs) {
                var bytes = DecAAC(item.GetAACData());
                ms.Write(bytes, 0, bytes.Length);
            }
            return ms.ToArray();
        }

        private byte[] DecAAC(byte[] buffer) {
            byte[] @out = new byte[0];
            int len = _aac.AudioDec(buffer, ref @out);
            if (len > 0) {
                if (len != @out.Length)
                    Array.Resize<byte>(ref @out, len);
                return @out;
            } else {
                return new byte[0];
            }
        }

        private void OnError(Exception ex) {
            if (Error != null) Error(this, new EventArgsEx<Exception>(ex));
        }

        public override void Dispose() {
            Stop();

            _isDisoseing = true;
            try {
                //if (_play != null)
                //    _play.Dispose();
                if (_speex != null)
                    _speex.Dispose();
                if (_aac != null)
                    _aac.Release();
                if (_wave != null)
                    _wave.Dispose();
            } catch {
            } finally {
                _isDisoseing = false;
                _isDisosed = true;
            }
        }
 
    }
 
}
