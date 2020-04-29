using Common.Generic;
using GB28181.Logger4Net.DebugEx;
using GB28181.WinTool.Codec;
using StreamingKit;
using System;


namespace GB28181.WinTool.Media
{

    public class MediaCapturer : IDisposable
    {
        private bool _isRuning = false;
        private MicEncoder _me = null;
        private CameraEncoder _ce = null;
        private VideoEncodeCfg _vCfg = null;
        private AudioEncodeCfg _aCfg = null;
        private long _lastTick = 0;
        public bool IsVideoPub { get; set; }
        public bool IsAudioPub { get; set; }
        public VideoEncodeCfg VideoEncodeConfig { get { return _vCfg; } }
        public AudioEncodeCfg AudioEncodeConfig { get { return _aCfg; } }
        public int MinVBR = 6 * 8;
        public event EventHandler<EventArgsEx<Exception>> Error;
        public event EventHandler<EventArgsEx<MediaFrame>> Captured;

        public MediaCapturer(VideoEncodeCfg cfgVideo, AudioEncodeCfg cfgAudio)
        {
            IsVideoPub = true;
            IsAudioPub = true;
            _vCfg = cfgVideo;
            _aCfg = cfgAudio;
            Init();
        }

        private void Init()
        {
            if (_vCfg.Params.ContainsKey("ScreenEncode") && (int)_vCfg.Params["ScreenEncode"] == 1)
            {
                _ce = new ScreenaEncoder(_vCfg, OnCaptured);
            }
            else
            {
                _ce = new CameraEncoder(_vCfg, OnCaptured);
            }
            _ce.Error += OnError;

            _me = new MicEncoder(_aCfg, OnCaptured);
            _me.Error += OnError;
        }

        public void Start()
        {
            if (_isRuning)
                return;
            _isRuning = true;
            try
            {
                if (_me != null && IsAudioPub)
                {
                    _me.Start();
                }
                if (_ce != null && IsVideoPub)
                {
                    _ce.Start();
                }
            }
            catch (Exception ex)
            {
                _DebugEx.Trace("Capturer", ex.ToString());
                OnError(ex);
            }
        }

        public void Stop()
        {
            if (!_isRuning)
                return;
            _isRuning = false;
            try
            {
                if (_me != null)
                    _me.Stop();
                if (_ce != null)
                    _ce.Stop();
            }
            catch (Exception e)
            {
            }
        }

        public void ChangeCfg(VideoEncodeCfg vCfg, AudioEncodeCfg aCfg)
        {
            try
            {
                _vCfg = vCfg;
                _aCfg = aCfg;

                if (_ce != null)
                {
                    _ce.Error -= OnError;
                    _ce.Stop();
                    _ce.Dispose();
                    _ce = null;
                }
                if (_me != null)
                {
                    _me.Error -= OnError;
                    _me.Stop();
                    _me.Dispose();
                    _me = null;
                }
                Init();

                if (_isRuning)
                {
                    if (IsVideoPub)
                        _ce.Start();
                    if (IsAudioPub)
                        _me.Start();
                }
            }
            catch (Exception e)
            {
                OnError(e);
                Stop();
            }
        }

        public void ChangeVideoCfg(VideoEncodeCfg vCfg)
        {
            try
            {
                _vCfg = vCfg;
                if (_ce != null)
                {
                    _ce.Error -= OnError;
                    _ce.Stop();
                    _ce.Dispose();
                    _ce = null;
                }
                if (_vCfg.Params.ContainsKey("ScreenEncode") && (int)_vCfg.Params["ScreenEncode"] == 1)
                {
                    _ce = new ScreenaEncoder(_vCfg, OnCaptured);
                }
                else
                {
                    _ce = new CameraEncoder(_vCfg, OnCaptured);
                }
                _ce.Error += OnError;
                if (_isRuning)
                {
                    if (IsVideoPub)
                        _ce.Start();
                }
            }
            catch (Exception e)
            {
                OnError(e);
                Stop();
            }
        }

        public void ChangeAudioCfg(AudioEncodeCfg aCfg)
        {
            try
            {
                _aCfg = aCfg;
                if (_me != null)
                {
                    _me.Error -= OnError;
                    _me.Stop();
                    _me.Dispose();
                    _me = null;
                }
                _me = new MicEncoder(_aCfg, OnCaptured);
                _me.Error += OnError;
                if (_isRuning)
                {
                    if (IsAudioPub)
                        _me.Start();
                }
            }
            catch (Exception e)
            {
                OnError(e);
                Stop();
            }
        }

        public void SetVBR(int bit_k)
        {
            //这是10是预算去掉音频,还有一个描述包信息等
            if (bit_k > MinVBR && _ce != null)
            {
                _ce.SetVBR(bit_k - MinVBR);
            }
        }



        private void OnCaptured(MediaFrame mf)
        {
            lock (this)
            {
                if (!mf.IsCommandMediaFrame())
                {
                    if (mf.NTimetick == _lastTick)
                        mf.NTimetick++;
                    if (mf.NTimetick > _lastTick)
                        _lastTick = mf.NTimetick;
                }
            }
            if (!IsAudioPub && mf.IsAudio == 1)
                return;
            if (!IsVideoPub && mf.IsAudio == 0)
                return;

            if (Captured != null)
                Captured(this, new EventArgsEx<MediaFrame>(mf));
        }

        private void OnError(Exception ex)
        {
            if (Error != null) Error(this, new EventArgsEx<Exception>(ex));
        }

        private void OnError(object sender, EventArgsEx<Exception> e)
        {
            OnError(e.Arg);
        }

        public void Dispose()
        {
            Stop();
            if (_me != null)
                _me.Dispose();
            if (_ce != null)
                _ce.Dispose();
        }

    }

    public class FileCapturer : IDisposable
    {
        private bool _isRuning = false;

        //private FileTSStreamInput _tsInput = null;
        public event EventHandler<EventArgsEx<Exception>> Error;
        public event EventHandler<EventArgsEx<MediaFrame>> Captured;


        public FileCapturer()
        {
        }

        public FileCapturer(String file)
        {
            SetFile(file);
        }

        public void SetFile(String file)
        {
            //_tsInput = new FileTSStreamInput(file);
            //_tsInput.ReceiveFrame += OnCaptured;
            //_tsInput.End += OnEnd;
        }

        public void Start()
        {
            //if (_tsInput == null)
            //    return;
            //if (_isRuning)
            //    return;
            //_isRuning = true;
            //try
            //{
            //    _tsInput.Start();
            //}
            //catch (Exception ex)
            //{
            //    _DebugEx.Trace("Capturer", ex.ToString());
            //    OnError(ex);
            //}
        }
        public void Stop()
        {
            if (!_isRuning)
                return;
            _isRuning = false;
            try
            {
              //  _tsInput.Stop();
            }
            catch (Exception e)
            {

            }
            //_tsInput = null;
        }
        private void OnEnd(object sender, EventArgs e)
        {
           // _tsInput.Restart();
        }
        private void OnCaptured(MediaFrame mf)
        {
            if (Captured != null)
                Captured(this, new EventArgsEx<MediaFrame>(mf));
        }
        private void OnError(Exception ex)
        {
            if (Error != null)
                Error(this, new EventArgsEx<Exception>(ex));
        }
        public void Dispose()
        {
            Stop();
           // _tsInput.Dispose();
        }
    }
 


}
