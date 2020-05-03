using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Helpers;
using Common.Generic;
using GB28181.WinTool.Codec;
using GB28181.WinTool.Media;
using StreamingKit;

namespace GB28181.WinTool.Media {
    public class MediaPlayer : IDisposable {

        private WaveAudioPlayer _ap;
        private VideoPlayer _vp;
        private bool _isworking = false;
        private Thread _playThread = null;
        private bool _IsAudioPlay = true;
        private bool _IsVideoPlay = true;
        private long _lastReceiveFrameTick = 0;
        private long _startPlayTick = 0;//开始播放的系统时间
        private long _lastPausedTick = 0;
        private bool _isPaused = false;
        private bool _IgnoreVideoPlay = false;


        private AQueue<MediaFrame> _queue = new AQueue<MediaFrame>();
        private List<MediaFrame> _cache = new List<MediaFrame>();
        //是否为实时播放
        public bool IsReadPlay { get;   set; }
        //是否播放音频
        public bool IsAudioPlay { get { return _IsAudioPlay; } set { _IsAudioPlay = value; } }
        //是否播放视频
        public bool IsVideoPlay { get { return _IsVideoPlay; } set { _IsVideoPlay = value; } }
        //是否正在播放中
        public bool IsPlaying { get { return _vp != null && _vp.IsPlaying; } }
        //缓冲播放时长，该值暂时只在第一次播放进行缓冲
        public int BuffTime { get; set; }
        //当前播放缓冲区是否为空
        public Boolean BufferEmpty { get { return _ap.BufferEmpty && _vp.BufferEmpty; } }
        //当前播放位置,对应当前音频或视频的最后一个播放帧的时间戳
        public long Position {
            get {
                if (_ap.Position > _vp.Position)
                    return _ap.Position;
                else
                    return _vp.Position;
            }
            set {
                lock (_queue) {
                    if (IsReadPlay) {
                        var curPos = Position;
                        _ap.ResetPosition();
                        _vp.ResetPosition();
                        _queue.Clear();
                        _firstVideoFrameTime = 0;
                        _firstAudioFrameTime = 0;
                        _firstFrameTime = 0;
                        _startPlayTick = 0;
                    } else {
                        var curPos = Position;
                        if (curPos > value) {
                            _ap.ResetPosition();
                            _vp.ResetPosition();
                            _queue.Clear();
                            _firstVideoFrameTime = 0;
                            _firstAudioFrameTime = 0;
                            _firstFrameTime = 0;
                            _startPlayTick = 0;
                            bool _videoIFrameAdded = false;
                            foreach (var item in _cache) {
                                if (item.NTimetick >= value) {
                                    if (_videoIFrameAdded || (item.IsKeyFrame == 1 && item.IsAudio == 0)) {
                                        _queue.Enqueue(item);
                                        _videoIFrameAdded = true;
                                    }
                                }
                            }
                        } else {
                            _ap.ResetPosition();
                            _vp.ResetPosition();
                            _queue.Clear();
                            _firstVideoFrameTime = 0;
                            _firstAudioFrameTime = 0;
                            _firstFrameTime = 0;
                            _startPlayTick = 0;
                            bool _videoIFrameAdded = false;
                            foreach (var item in _cache) {
                                if (item.NTimetick >= value) {
                                    if (_videoIFrameAdded || (item.IsKeyFrame == 1 && item.IsAudio == 0)) {
                                        _queue.Enqueue(item);
                                        _videoIFrameAdded = true;
                                    }
                                }
                            }
                        }
                    }

                }

                var tick = Environment.TickCount;
                while (Environment.TickCount - tick < 1000 && _vp.Position == 0)
                    ThreadEx.Sleep(10);

            }
        }

        public long VideoPosition { get { return _vp.Position; } }

        private float _speed = 1f;
        //播放速度，1为正常播放，该值处理不正确，暂不要使用
        public float Speed { get { return _speed; } set { _speed = value; _ap.Speed = value; _vp.Speed = value; ; } }

        //最大播放时长
        public long Length { get { return _lastReceiveFrameTick; } }
        //向前播放
        public bool ForwardPlay { get { return _vp.ForwardPlay; } set { _vp.ForwardPlay = value; } }

        public int Volume { get { return _ap.Volume; } set { _ap.Volume = value; } }

        //异常事件
        public event EventHandler<EventArgsEx<Exception>> Error;

        public MediaPlayer(IYUVDraw yuvDraw, bool isRealPlay = true,bool isCachePlay=false) {
            //if (System.Configuration.ConfigurationSettings.AppSettings["AudioPlayMode"] == "SDL")
            //    _ap = new SDLAudioPlayer();
            //else
            _ap = new WaveAudioPlayer();
            _vp = new VideoPlayer(yuvDraw);
            IsReadPlay = isRealPlay;
            BuffTime = 0;
            if (isCachePlay)
                _IgnoreVideoPlay = true;
        }
        bool hasKey = false;
        public void Play(MediaFrame frame) {
            if (!_isworking)
                return;
            lock (_queue) {

                if (_lastReceiveFrameTick < frame.NTimetick)
                    _lastReceiveFrameTick = frame.NTimetick;

                if (frame.IsAudio == 1 && IsAudioPlay)
                    _queue.Enqueue(frame);

                if (frame.IsAudio == 0 && IsVideoPlay)
                {
                    if (hasKey||frame.IsKeyFrame==1)
                    {
                        _queue.Enqueue(frame);
                        hasKey = true;
                    }
                    
                }

                if (!IsReadPlay)
                    _cache.Add(frame);

            }
        }

        public void Start() {
            if (_isworking)
                return;
            _isworking = true;
            _firstVideoFrameTime = 0;
            _firstAudioFrameTime = 0;
            _firstFrameTime = 0;
            _startPlayTick = 0;
            _queue.Clear();
            _cache.Clear();
            _vp.Start();
            _ap.Start();
            _playThread = ThreadEx.ThreadCall(PlayThread);
        }

        public void Stop() {
            if (!_isworking)
                return;
            _isworking = false;
            _vp.Stop();
            _ap.Stop();
            ThreadEx.ThreadStop(_playThread);
            _queue = new AQueue<MediaFrame>();
            _cache = new List<MediaFrame>();
        }

        public void Pause() {
            if (_isPaused)
                return;
            _isPaused = true;
            _lastPausedTick = Environment.TickCount;
            _vp.Pause();
            _ap.Pause();
        }

        public void Continue() {
            if (!_isPaused)
                return;
            _isPaused = false;

            _startPlayTick += (Environment.TickCount - _lastPausedTick);

            _vp.Continue();
            _ap.Continue();
        }

        

        private long _firstVideoFrameTime = 0;
        private long _firstAudioFrameTime = 0;
        private long _firstFrameTime = 0;
        private void PlayThread() {

            bool _needBuffer = BuffTime > 0;
            bool _needSleep = false;
            while (_isworking) {
                lock (_queue) {
                    if (_queue.Count > 0 && !_needBuffer && !_isPaused) {
                        var frame = _queue.Dequeue();
                        if (_firstFrameTime == 0)
                            _firstFrameTime = frame.NTimetick;
                        if (_startPlayTick == 0)
                            _startPlayTick = Environment.TickCount;

                        if (frame.IsAudio == 0) {
                            if (_firstVideoFrameTime == 0)
                                _firstVideoFrameTime = frame.NTimetick;
                        } else if (frame.IsAudio == 1 && (_vp.IsPlaying || _IgnoreVideoPlay) && ForwardPlay) {
                            if (_firstAudioFrameTime == 0)
                                _firstAudioFrameTime = frame.NTimetick;
                        }

                        if (_firstVideoFrameTime != 0)
                            _vp.SyncPlayTime(_firstVideoFrameTime + (int)((Environment.TickCount - _startPlayTick) * 1));

                        if (_firstAudioFrameTime != 0)
                            _ap.SyncPlayTime(_firstAudioFrameTime + (int)((Environment.TickCount - _startPlayTick) * 1));

                        if (frame.IsAudio == 0) {
                            _vp.Play(frame);
                        } else if (frame.IsAudio == 1 && (_vp.IsPlaying || _IgnoreVideoPlay) && ForwardPlay) {
                            _ap.Play(frame);
                        }
                        _needSleep = false;
                    } else {
                        if (!_isPaused) {
                            if (_firstVideoFrameTime != 0)
                                _vp.SyncPlayTime(_firstVideoFrameTime + (int)((Environment.TickCount - _startPlayTick) * 1));

                            if (_firstAudioFrameTime != 0)
                                _ap.SyncPlayTime(_firstAudioFrameTime + (int)((Environment.TickCount - _startPlayTick) * 1));
                            if (_queue.Count > 0 && _needBuffer) {
                                var mf = _queue.Peek();
                                if (_lastReceiveFrameTick - mf.NTimetick > BuffTime * 1000) {
                                    _needBuffer = false;
                                }
                            }
                        }
                        _needSleep = true;
                    }
                }
                if (_needSleep)
                    Thread.Sleep(10);
            }
        }

        protected void OnError(Exception e) {
            if (Error != null)
                Error(this, new EventArgsEx<Exception>(e));
        }

        public void Dispose() {
            Stop();
            if (_ap != null)
                _ap.Dispose();
            if (_vp != null)
                _vp.Dispose();
           
        }
    }
}
