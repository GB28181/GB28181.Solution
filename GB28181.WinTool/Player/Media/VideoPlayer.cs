using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using StreamingKit;
using GB28181.WinTool.Codec;
using System.Threading;
using Helpers;
using Common.Generic;

namespace GB28181.WinTool.Media {
    public class VideoPlayer : IDisposable {
        private IYUVDraw _yuvDraw = null;
        private FFImp _ffimp = null;
        private Control _control = null;
        private bool _inited = false;
        private bool _isworking = false;
        private bool _isPaused = false;
        private Thread _playThread = null;
        private long _curPlayMediaTimetick = 0;//当前播放帧的媒体时间戳
        private long _lastPlayMediaTimetick = 0;//最后播放帧的媒体时间戳
        private long _lastPlaySystemTime = 0;//最后一帧播放的系统时间戳
        private long _syncPlayTime = 0;
        private byte[] _yuvDataBuffer = null;
        private bool _isHasBFrame = false;
        private int _width = -1;
        private int _height = -1;
        private Action<byte[]> _drawHandle = null;
        private Stack<MediaFrame> _stackRewindFrame = null;
        private AQueue<Stack<MediaFrame>> _queueRewindStack = new AQueue<Stack<MediaFrame>>();
        private bool _isForwardPlay = true;
        private Thread _PlayBackwardThread = null;
        private bool _PlayBackwardResetPos = false;
        public bool ForwardPlay { get { return _isForwardPlay; } set { _isForwardPlay = value; } }
        private AQueue<MediaFrame> _queue = new AQueue<MediaFrame>();
 
        public Boolean BufferEmpty { get { return _queue.Count == 0; } }

        public bool IsPlaying { get { return _inited && _isworking; } }

        public long Position { get { return _curPlayMediaTimetick; } }
        private float _speed = 1f;
        //播放速度，1为正常播放
        public float Speed { get { return _speed; } set { _speed = value; } }

        public VideoPlayer(Control control) {
            _control = control;
        }

        public VideoPlayer(IYUVDraw draw) {
            _yuvDraw = draw;
        }

        private void Init(MediaFrame frame, bool reinit = false) {
            if (_inited && !reinit)
                return;

            if (_ffimp != null) {
                _ffimp.Release();
            }
 
            _inited = true;
            _width = frame.Width;
            _height = frame.Height;
            _ffimp = new FFImp(AVCodecCfg.CreateVideo(_width, _height), true,true);
            if (_yuvDraw == null && _control != null) {
                _yuvDraw = new YUVGDIDraw(_control);
            }
            _yuvDraw.SetSize(_width, _height);

            _yuvDataBuffer = new byte[_width * _height * 3 / 2];

            _drawHandle = new Action<byte[]>(Draw);
        }

        private bool CheckInit(MediaFrame frame, bool reinit = false) {
 
            if (!_inited) {
                if (frame.IsKeyFrame == 1)
                    Init(frame);
            }

            if (!_inited)
                return false;
            if (frame.IsCommandMediaFrame() && frame.GetCommandType() == MediaFrameCommandType.ResetCodec) {
                _inited = false;
                return false;
            } else if (frame.IsKeyFrame == 1) {
                if (_width != -1 && _height != -1 && (frame.Width != _width || frame.Height != _height)) {
                    Init(frame, true);
                }
            }
            return true;
        }
 
        public void Start() {
            if (_isworking)
                return;
            _isworking = true;
            ResetPosition();
            _curPlayMediaTimetick = 0;
            _playThread = ThreadEx.ThreadCall(PlayThread);
            _yuvDraw.Start();
        }

        public void Stop() {
            if (!_isworking)
                return;
            _isworking = false;
            ThreadEx.ThreadStop(_playThread);
            ThreadEx.ThreadStop(_PlayBackwardThread);
            _queue.Clear();
            _curPlayMediaTimetick = 0;
            _yuvDraw.Stop();
        }

        public void Pause() {
            _isPaused = true;
        }

        public void Continue() {
            _isPaused = false;
        }

        public virtual void SyncPlayTime(long time) {
            _syncPlayTime = time;
        }

        public virtual long CurrentPlayTime() {
            return _curPlayMediaTimetick;
        }
 
        public void Play(MediaFrame frame) {
            if (!_isworking)
                return;
            lock (_queue) {
                _queue.Enqueue(frame);
            }
        }

        long coutnnn = 0;
        int i = 0;
        int b = 40;
        int c = 40;
        int j = 0;
        long bb = 0;
        long ccc = 0;
        private void PlayForward(MediaFrame frame)
        {
            if (frame != null)
            {
                if (frame.NTimetick < _curPlayMediaTimetick)
                {
                    _isHasBFrame = true;
                }
                if (_curPlayMediaTimetick < frame.NTimetick)
                    _curPlayMediaTimetick = frame.NTimetick;
            }
            if (frame != null)
            {
                if (!_isHasBFrame)
                {
                    if (Speed == 1)
                    {
                        long a = frame.NTimetick - ccc;

                        if (a <150)
                        {
                            coutnnn += a-(int)bb;
                            i++;
                            if (i == 20)
                            {
                                b = (int)coutnnn /20 ;
                                i = 0;
                                coutnnn = 0;
                            }
                        }
                        if (max == 0)
                            max = b;
                        if (min == 0)
                            min = b;
                        int sleep = (int)b ;
                        
                        if (sleep > max)
                            max = sleep;
                        if (sleep < min)
                            min = sleep;
                        if (_queue.Count <4)
                        {
                            //int mid = (max + min) / 2;
                            //sleep += 1;
                            //b += 1;
                            if (min != 0 && max != 0 && min != max)
                            {
                                sleep = (min + max) / 2+ (min + max) % 2;
                                b = sleep;
                                min = 0;
                                max = 0;
                            }
                            else
                            {
                                sleep += 1;
                                b += 1;
                            }
                            //max = mid;
                            //min = mid;
                        }
                        if (_queue.Count > 6)
                        {
                            if (min != 0 && max != 0 && min != max)
                            {
                                sleep = (min + max) / 2;
                                b = sleep;
                                min = 0;
                                max = 0;
                            }
                            else
                            {
                                sleep -= 1;
                                b--;
                            }
                           
                        }
                        if (sleep <= 0)
                            sleep = 10;
                        //var sleep = (int)(_syncPlayTime);
                        //if (sleep > 200)
                        //    sleep = 40;
                        //if (sleep < 0)
                        //    sleep = 10;
                        //  Console.WriteLine("tick:" +a+ " cachecount:" + _queue.Count);

                        ccc = frame.NTimetick;
                       
                       // Console.WriteLine("tick:" + sleep + " cachecount:" + _queue.Count);
                        Thread.Sleep(sleep);
                        
                    }
                    else
                    {
                        var sysSpan = Environment.TickCount - _lastPlaySystemTime;
                        var sleep = (int)((frame.NTimetick - _lastPlayMediaTimetick - sysSpan) / Speed);
                        if (sleep > 200 || sleep < 0)
                            sleep = 40;
                        Thread.Sleep(sleep);
                        _lastPlayMediaTimetick = frame.NTimetick;
                    }
                }
                long dd = DateTime.Now.Ticks;
                if (!CheckInit(frame))
                    return;

                byte[] yuv = _ffimp.VideoDec(frame.GetData(), _yuvDataBuffer);

                //_drawHandle.BeginInvoke(frame.Data, null, null);
                DateTime db = DateTime.Now;
                Draw(yuv);
                bb =( DateTime.Now.Ticks - dd)/10000;

                Console.WriteLine(DateTime.Now.TimeOfDay + "  Consume: " + (DateTime.Now-db).TotalMilliseconds);

            }
            else
            {
                ThreadEx.Sleep(10);
            }

        }
        int errorsleep = 0;
        int iptvdefaultsleep = 40;
        int min = 0;
        int max = 0;
        int decsleep = 0;
        int lastcount = 1;
        int queueCount = 0;
        //private void PlayForward(MediaFrame frame)
        //{
        //    if (frame != null)
        //    {
        //        if (frame.nTimetick < _curPlayMediaTimetick)
        //        {
        //            _isHasBFrame = true;
        //        }
        //        if (_curPlayMediaTimetick < frame.nTimetick)
        //            _curPlayMediaTimetick = frame.nTimetick;
        //    }

        //    if (frame != null)
        //    {
        //        if (!_isHasBFrame)
        //        {
        //            if (Speed == 1)
        //            {
        //                var sleep = (int)(_syncPlayTime);
        //                //if (_queue.Count > (4 + errorsleep))
        //                //{
        //                //    errorsleep +=1;
        //                //    iptvdefaultsleep -= 4;
        //                //}

        //                if (queueCount>6)
        //                {
                            
        //                   // errorsleep = decsleep / 2 + decsleep % 2;
        //                    //if (decsleep != 0)
        //                    //    iptvdefaultsleep -= decsleep / 2 ;
        //                    //else
        //                        iptvdefaultsleep -= 1;// +errorsleep % 2;
        //                    min = iptvdefaultsleep;
        //                    decsleep = 0;
        //                    errorsleep += 1;
        //                }

        //                if (_queue.Count <3)
        //                {
        //                    // decsleep = errorsleep / 2 + errorsleep % 2;
        //                    if (errorsleep != 0)
        //                    {
        //                        iptvdefaultsleep += errorsleep / 2;
        //                    }
        //                    else
        //                        iptvdefaultsleep += 1;// +decsleep % 2; ;
        //                    max = iptvdefaultsleep;
                           
        //                    errorsleep = 0;
        //                    decsleep++;
        //                }
                       
        //                if (iptvdefaultsleep > 100)
        //                {
        //                    iptvdefaultsleep = 100;
        //                }
                       
        //                if (iptvdefaultsleep * queueCount > 1000)
        //                {
        //                    iptvdefaultsleep -= 1;
        //                }

        //                if (iptvdefaultsleep <= 0)
        //                    iptvdefaultsleep = (min + max) / 2 <= 0 ? 40 : (min + max) / 2;
        //                //if (iptvdefaultsleep>40)
        //                //    iptvdefaultsleep = 40;
        //                Thread.Sleep(iptvdefaultsleep);
        //                //Console.Clear();
        //                //Console.WriteLine("tick:" + iptvdefaultsleep + " cachecount:" + queueCount + " lastcount:" + lastcount + " sleep:" + sleep);
        //                lastcount = queueCount;
        //                _lastPlayMediaTimetick = frame.nTimetick;

        //                //}
        //                //else
        //                //{
        //                //    var sleep = (int)((frame.nTimetick - _syncPlayTime));
        //                //    if (sleep > 200)
        //                //        sleep = 40;
        //                //    if (sleep < 0)
        //                //        sleep = 0;
        //                //    Thread.Sleep(sleep);
        //                //    _lastPlayMediaTimetick = frame.nTimetick;
        //                //}
        //            }
        //            else
        //            {
        //                var sysSpan = Environment.TickCount - _lastPlaySystemTime;
        //                var sleep = (int)((frame.nTimetick - _lastPlayMediaTimetick - sysSpan) / Speed);
        //                if (sleep > 200 || sleep < 0)
        //                    sleep = 40;
        //                Thread.Sleep(sleep);
        //                _lastPlayMediaTimetick = frame.nTimetick;
        //            }
        //        }
        //        if (!CheckInit(frame))
        //            return;

        //        _lastPlaySystemTime = Environment.TickCount;

        //        byte[] yuv = _ffimp.VideoDec(frame.Data, _yuvDataBuffer);

        //        //_drawHandle.BeginInvoke(frame.Data, null, null);

        //        Draw(yuv);

        //    }
        //    else
        //    {
        //        ThreadEx.Sleep(10);
        //    }

        //}

        private void PlayBackward(MediaFrame frame) {
  
            if (frame != null) {
                if (frame.IsKeyFrame == 1) {
                    if (_stackRewindFrame != null) {
                        _stackRewindFrame.Push(frame);
                        PlayBackward(_stackRewindFrame);
                    }
                    _stackRewindFrame = new Stack<MediaFrame>();

                } else {
                    if (_stackRewindFrame == null)
                        _stackRewindFrame = new Stack<MediaFrame>();
                    _stackRewindFrame.Push(frame);
                }
                _PlayBackwardResetPos = false;
            } else {
                ThreadEx.Sleep(10);
            }
        }


        private void PlayBackward(Stack<MediaFrame> stack) {
            Stack<MediaFrame> yuvStack = new Stack<MediaFrame>();
            while (_queueRewindStack.Count > 3)
                Thread.Sleep(10);

            if (_PlayBackwardResetPos)
                return;

            while (stack != null && stack.Count > 0 && !_PlayBackwardResetPos) {
                var frame = _stackRewindFrame.Pop();
                if (!CheckInit(frame))
                    continue;
                var bufferData = new byte[_yuvDataBuffer.Length];
                byte[] yuv = _ffimp.VideoDec(frame.GetData(), bufferData);
                if (yuv != null) {
                    var mf = new MediaFrame()
                    {
                        NTimetick = frame.NTimetick,
                        Size = yuv.Length,
                    };
                    mf.SetData(yuv);
                    yuvStack.Push(mf);
                }
            }
            if (!_PlayBackwardResetPos)
                _queueRewindStack.Enqueue(yuvStack);

        }


        private void PlayBackwardThread() {
            long lastTick = 0;
            while (_isworking) {
                Stack<MediaFrame> stack = null;
                lock (_queue) {
                    if (_queueRewindStack.Count > 0) {
                        stack = _queueRewindStack.Dequeue();
                    }
                }
                if (stack != null) {
                    while (stack.Count > 0 && !_PlayBackwardResetPos) {
                        var frame = stack.Pop();
                        //_drawHandle.BeginInvoke(frame.Data, null, null);
                        _drawHandle(frame.GetData());
                        if (lastTick == 0)
                            lastTick = frame.NTimetick;
                        int sleep = (int)(lastTick - frame.NTimetick);
                        if (sleep < 0 || sleep >= 100)
                            sleep = 40;
                        ThreadEx.Sleep(sleep);
                        //Console.WriteLine(sleep + "   " + (lastTick - frame.nTimetick)+"    "+(Environment.TickCount - sysTick));
                        lock (_queue) {
                            if (!_PlayBackwardResetPos)
                                _curPlayMediaTimetick = lastTick = frame.NTimetick;
                        }
                        lock (_queue) {
                            _lastPlaySystemTime = Environment.TickCount;
                        }
                    }
                    GC.Collect();
                } else {
                    ThreadEx.Sleep(10);
                }
            }
        }


        private void PlayThread() {
            _PlayBackwardThread = ThreadEx.ThreadCall(PlayBackwardThread);
            while (_isworking) {
                MediaFrame frame = null;
                if (_isForwardPlay) {
                    lock (_queue) {
                        if (_queue.Count > 0 && !_isPaused)
                            frame = _queue.Dequeue();
                        else
                            frame = null;
                    }
                    if (frame != null)
                        PlayForward(frame);
                    else
                        ThreadEx.Sleep(10);
                } else {
                    lock (_queue) {
                        if (_queue.Count > 0 && !_isPaused)
                            frame = _queue.Dequeue();
                        else
                            frame = null;
                    }
                    if (frame != null)
                        PlayBackward(frame);
                    else
                        ThreadEx.Sleep(10);
                }
            }
        }

    

        private void Draw(byte[] yuv) {
            if (yuv != null && yuv.Length > 0)
                _yuvDraw.Draw(yuv);
        }

        public void Clean() {
            _yuvDraw.Clean();
        }

        public void ResetPosition() {
            lock (_queue) {
                _queue.Clear();
           
          
                _PlayBackwardResetPos = true;
                _queueRewindStack = new AQueue<Stack<MediaFrame>>();
                _stackRewindFrame = new Stack<MediaFrame>();
                _curPlayMediaTimetick = 0;
            }

        }

        public void Dispose() {
            Stop();
            if (_ffimp != null)
                _ffimp.Release();
            if (_yuvDraw != null)
                _yuvDraw.Release();

            _yuvDataBuffer = null;
        }

    }
}
