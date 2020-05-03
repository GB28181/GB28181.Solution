using DirectShowLib;
using Helpers;
using Common.Generic;
using GB28181.WinTool.Mixer.Video;
using StreamingKit;
using SS.WPFClient.DShow;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading;
using GB28181.Logger4Net.DebugEx;

namespace GB28181.WinTool.Codec
{
    public class CameraCapturer : IDisposable
    {
        private GenFilterGraphEx _graph = null;//图表
        private BaseFilterEx _filterCamera = null;//摄像头
        private BaseFilterEx _filterGrabber = null;//回调器
        private BaseFilterEx _filterNullRenderer = null;//null渲染器

        private SampleGrabberCB _grabberCB = null;//数据回调委托
        public IBasicVideo _basicVideo = null;

        private bool _isworking = false;
        private bool _isDisoseing = false;
        private bool _isDisosed = false;
        private int _camera;
        private int _width;
        private int _height;

        private FFScale _ffscale = null;
        private Action<byte[]> _callBack = null;
        public CameraCapturer()
        {
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="control"></param>
        /// <param name="camera"></param>
        /// <param name="size"></param>
        /// <param name="callBack">输出为YUV420P的数据</param>
        public CameraCapturer(int camera, int width, int height, Action<byte[]> callBack)
        {
            _camera = camera;
            _width = width;
            _height = height;
            _callBack = callBack;
        }

        public virtual void Start()
        {

            if (_isDisoseing || _isDisosed)
                throw new Exception("对象已经释放或正在释放");

            if (_isworking)
                return;


            _isworking = true;
            try
            {
                GraphSetup();
                var hr = _graph.Run(); ;//执行播放
            }
            catch (Exception ex)
            {
                _DebugEx.Trace("Capturer", ex.ToString());
                throw;
            }
        }

        public virtual void Stop()
        {
            if (!_isworking)
                return;
            _isworking = false;
            try
            {
                if (_graph != null)
                    _graph.Stop();
            }
            catch (Exception e)
            {
            }
        }

        private void GraphSetup()
        {
            try
            {
                _graph = new GenFilterGraphEx();

                _filterCamera = _graph.AddCapture(_camera);//增加摄像头


                _filterGrabber = _graph.AddSampleGrabber();//添加数据回调

                _filterNullRenderer = _graph.AddNullRenderer();//

                var mt = DShowHelper.FindAMMediaType(_filterCamera.DefOutPin, _width, _height);

                (_filterCamera.DefOutPin.Pin as IAMStreamConfig).SetFormat(mt);

                if (mt == null)
                {
                    throw new Exception();
                }

                var hr = _graph.ConnencAll(mt ?? _filterCamera.DefOutPin.MediaTypes[0]);//连接图表

                if (!hr)
                {
                    throw new Exception();
                }
                else
                {
                    FFScaleSetup(mt);
                }
            }
            catch (Exception e)
            {
                throw;
            }
            if (_filterGrabber != null)
                GrabberSetup((ISampleGrabber)_filterGrabber.BaseFilter);//设置数据回调
        }

        private void GrabberSetup(ISampleGrabber grabber)
        {
            var SampleGrabber = (ISampleGrabber)grabber;
            _grabberCB = new SampleGrabberCB(new Action<double, IntPtr, int>(SampleGrabber_Callback));
            var hr = SampleGrabber.SetCallback(_grabberCB, 0);
            DsError.ThrowExceptionForHR(hr);
        }

        private void FFScaleSetup(AMMediaType mt)
        {
            //e436eb7d-524f-11ce-9f53-0020af0ba770  RGB24
            //32595559-0000-0010-8000-00aa00389b71  YUY2
            // 
            if (mt.subType.ToString() == "32595559-0000-0010-8000-00aa00389b71")
            {
                _ffscale = new FFScale(_width, _height, 1, 12, _width, _height, 0, 12);
            }
            else if (mt.subType.ToString() == "e436eb7d-524f-11ce-9f53-0020af0ba770")
            {
                _ffscale = new FFScale(_width, _height, 2, 24, _width, _height, 0, 12);
            }
            else
            {
                throw new Exception("FFScaleSetup Error");
            }
        }

        //回调函数
        private void SampleGrabber_Callback(double SampleTime, IntPtr pBuf, int len)
        {

            if (_isDisoseing || _isDisosed)
                return;

            if (!_isworking)
                return;

            var buf = FunctionEx.IntPtrToBytes(pBuf, 0, len);

            buf = _ffscale.Convert(buf);

            if (_callBack != null)
                _callBack(buf);
        }

        public void Dispose()
        {

            _isDisoseing = true;
            try
            {
                _graph.GraphBuilder.Abort();

                if (_graph.MediaControl != null)
                    _graph.MediaControl.StopWhenReady();
                // Stop receiving events
                if (_graph.MediaEventEx != null)
                    _graph.MediaEventEx.SetNotifyWindow(IntPtr.Zero, 0x8000 + 1, IntPtr.Zero);
                if (_graph.VideoWindow != null)
                {
                    _graph.VideoWindow.put_Visible(OABool.False);
                    _graph.VideoWindow.put_Owner(IntPtr.Zero);
                }

                if (_graph.MediaControl != null)
                    Marshal.ReleaseComObject(_graph.MediaControl);
                if (_graph.MediaEventEx != null)
                    Marshal.ReleaseComObject(_graph.MediaEventEx);
                if (_graph.VideoWindow != null)
                    Marshal.ReleaseComObject(_graph.VideoWindow);
                if (_graph.GraphBuilder != null)
                    Marshal.ReleaseComObject(_graph.GraphBuilder);

            }
            catch
            {
            }
            finally
            {
                _isDisoseing = false;
                _isDisosed = true;
            }
        }

    }
    public class CameraAForge : CameraCapturer
    {

        private AForge.Video.DirectShow.VideoCaptureDevice videoDevice;
        private AForge.Video.DirectShow.FilterInfoCollection videoDevices;
        private FFScale _ffscale = null;
        private Action<byte[]> _callBack = null;
        public CameraAForge(int camera, int width, int height, Action<byte[]> callBack)
        {
            _callBack = callBack;
            _ffscale = new FFScale(width, height, 3, 24, width, height, 0, 12);
            videoDevices = new AForge.Video.DirectShow.FilterInfoCollection(FilterCategory.VideoInputDevice);
            videoDevice = new AForge.Video.DirectShow.VideoCaptureDevice(videoDevices[camera].MonikerString);
            foreach (var item in videoDevice.VideoCapabilities)
            {
                if (item.FrameSize.Width == width && item.FrameSize.Height == height)
                    videoDevice.VideoResolution = item;
            }
            if (videoDevice.VideoResolution == null)
                throw new Exception(string.Format("摄像头不支持{0}*{1}分辨率", width, height));

            videoDevice.NewFrame += NewFrame;
            videoDevice.RGBRawFrame += YUVFrame;

        }

        public override void Start()
        {
            videoDevice.Start();
        }

        public override void Stop()
        {
            videoDevice.Stop();
        }

        private void NewFrame(object sender, AForge.Video.NewFrameEventArgs e)
        {
        }

        private void YUVFrame(object sender, AForge.Video.DirectShow.RGBRawFrameEventArgs e)
        {
            byte[] buffer = FunctionEx.IntPtrToBytes(e.Buffer, 0, e.Len);
            buffer = _ffscale.Convert(buffer);

            if (_callBack != null)
                _callBack(buffer);
        }



        public static string[] GetVideoInputs()
        {
            var devices = new AForge.Video.DirectShow.FilterInfoCollection(FilterCategory.VideoInputDevice);
            List<string> list = new List<string>();
            for (int i = 0; i < devices.Count; i++)
            {
                list.Add(devices[i].Name);
            }
            return list.ToArray();
        }

        public static bool CheckSupportSize(int cameraId, int width, int height)
        {
            var videoDevices = new AForge.Video.DirectShow.FilterInfoCollection(FilterCategory.VideoInputDevice);
            var videoDevice = new AForge.Video.DirectShow.VideoCaptureDevice(videoDevices[cameraId].MonikerString);
            foreach (var item in videoDevice.VideoCapabilities)
            {
                if (item.FrameSize.Width == width && item.FrameSize.Height == height)
                    videoDevice.VideoResolution = item;
            }
            if (videoDevice.VideoResolution == null)
                return false;
            else
                return true;
        }
    }
    public class ScreenCapturer : CameraCapturer
    {
        private FFScale _ffscale = null;
        private Action<byte[]> _callBack = null;
        private bool _isworking = false;
        private Thread _CaptureThread = null;
        private int _width;
        private int _height;
        private int _fps;
        public ScreenCapturer(int width, int height, int fps, Action<byte[]> callBack)
        {
            _width = width;
            _height = height;
            _fps = fps;
            _callBack = callBack;
            _ffscale = new FFScale(width, height, 3, 24, width, height, 0, 12);
        }

        public override void Start()
        {
            if (_isworking)
                return;
            _isworking = true;

            _CaptureThread = ThreadEx.ThreadCall(CaptureThread);

        }

        public override void Stop()
        {
            if (!_isworking)
                return;
            _isworking = false;
            ThreadEx.ThreadStop(_CaptureThread);
        }

        private void CaptureThread()
        {
            while (_isworking)
            {
                var dt = DateTime.Now;
                var yuv = Capture();

                if (_callBack != null)
                {
                    _callBack(yuv);
                }

                var sleep = (int)(1000 / _fps - (DateTime.Now - dt).TotalMilliseconds);
                if (sleep > 0)
                    Thread.Sleep(sleep);
                Console.WriteLine((DateTime.Now - dt).TotalMilliseconds);
            }
        }

        #region 抓取带鼠标的桌面图片
        private int _X, _Y;
        [StructLayout(LayoutKind.Sequential)]
        private struct ICONINFO
        {
            public bool fIcon;
            public Int32 xHotspot;
            public Int32 yHotspot;
            public IntPtr hbmMask;
            public IntPtr hbmColor;
        }
        [StructLayout(LayoutKind.Sequential)]
        private struct CURSORINFO
        {
            public Int32 cbSize;
            public Int32 flags;
            public IntPtr hCursor;
            public Point ptScreenPos;
        }
        [DllImport("user32.dll", EntryPoint = "GetSystemMetrics")]
        private static extern int GetSystemMetrics(int mVal);
        [DllImport("user32.dll", EntryPoint = "GetCursorInfo")]
        private static extern bool GetCursorInfo(ref CURSORINFO cInfo);
        [DllImport("user32.dll", EntryPoint = "CopyIcon")]
        private static extern IntPtr CopyIcon(IntPtr hIcon);
        [DllImport("user32.dll", EntryPoint = "GetIconInfo")]
        private static extern bool GetIconInfo(IntPtr hIcon, out ICONINFO iInfo);

        private byte[] Capture()
        {
            //if (AppConfig._D)
            //    return Capture_D();

            try
            {
                int _CX = 0, _CY = 0;
                var dt = DateTime.Now;

                Bitmap bmp = new Bitmap(GetSystemMetrics(0), GetSystemMetrics(1));
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.CopyFromScreen(0, 0, 0, 0, bmp.Size);
                    //var bmp_tmp = CaptureCursor(ref _CX, ref _CY);
                    //g.DrawImage(bmp_tmp, _CX, _CY);
                    g.Dispose();
                }
                var bmp1 = KiResizeImage(bmp, _width, _height);

                bmp.Dispose();

                bmp = bmp1;

                BitmapData imageData = bmp.LockBits(new Rectangle(0, 0, _width, _height), ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                byte[] bs = new byte[_width * _height * 3];
                Marshal.Copy(imageData.Scan0, bs, 0, bs.Length);
                bmp.UnlockBits(imageData);
                var @out = _ffscale.Convert(bs);
                return @out;
            }
            catch (Exception e)
            {

                //if (AppConfig._D)
                //    throw;

                return null;
            }
        }
        private Bitmap _bmp = null;
        private byte[] Capture_D()
        {
            try
            {
                int _CX = 0, _CY = 0;

                if (_bmp == null)
                {
                    var sw = GetSystemMetrics(0);
                    var sh = GetSystemMetrics(1);
                    if (sw > _width)
                        sw = _width;
                    if (sh > _height)
                        sh = _height;
                    _bmp = new Bitmap(sw, sh);
                }

                using (Graphics g = Graphics.FromImage(_bmp))
                {
                    g.CopyFromScreen(0, 0, 0, 0, _bmp.Size);
                    //var bmp_tmp = CaptureCursor(ref _CX, ref _CY);
                    //if (AppConfig._D)
                    //    g.DrawImage(bmp_tmp, _CX, _CY);
                    //else
                    //    g.DrawImage(bmp_tmp, _CX, _CY);

                    String str = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    unchecked
                    {
                        SolidBrush sbrush = new SolidBrush(Color.FromArgb((int)0xFF000000));
                        SolidBrush sbrush1 = new SolidBrush(Color.FromArgb((int)0xFFFFFFFF));
                        Font font = new Font("宋体", 14);
                        g.FillRectangle(new SolidBrush(Color.FromArgb((int)0x66FFFFFF)), new Rectangle(_width - 200, 10, 200, 26));

                        g.DrawString(str, font, sbrush, new PointF(_width - 195, 14));
                    }
                    g.Dispose();
                }



                //var bmp1 = KiResizeImage(_bmp, _width, _height);

                //_bmp.Dispose();

                //_bmp = bmp1;

                BitmapData imageData = _bmp.LockBits(new Rectangle(0, 0, _width, _height), ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                byte[] bs = new byte[_width * _height * 3];
                Marshal.Copy(imageData.Scan0, bs, 0, bs.Length);
                _bmp.UnlockBits(imageData);
                var @out = _ffscale.Convert(bs);

                return @out;
            }
            catch
            {



                return null;
            }
        }
        private Bitmap CaptureDesktop()//
        {
            try
            {
                int _CX = 0, _CY = 0;
                Bitmap _Source = new Bitmap(GetSystemMetrics(0), GetSystemMetrics(1));
                using (Graphics g = Graphics.FromImage(_Source))
                {

                    g.CopyFromScreen(0, 0, 0, 0, _Source.Size);
                    g.DrawImage(CaptureCursor(ref _CX, ref _CY), _CX, _CY);
                    g.Dispose();
                }
                _X = (800 - _Source.Width) / 2;
                _Y = (600 - _Source.Height) / 2;
                return _Source;
            }
            catch
            {
                return null;
            }
        }

        private Bitmap CaptureNoCursor()//抓取没有鼠标的桌面
        {
            Bitmap _Source = new Bitmap(GetSystemMetrics(0), GetSystemMetrics(1));
            using (Graphics g = Graphics.FromImage(_Source))
            {
                g.CopyFromScreen(0, 0, 0, 0, _Source.Size);
                g.Dispose();
            }
            return _Source;
        }
        private CURSORINFO _lastCursorInfo;
        private Bitmap CaptureCursor(ref int _CX, ref int _CY)
        {
            try
            {
                IntPtr _Icon;
                CURSORINFO _CursorInfo = new CURSORINFO();
                ICONINFO _IconInfo;
                _CursorInfo.cbSize = Marshal.SizeOf(_CursorInfo);
                if (GetCursorInfo(ref _CursorInfo))
                {
                    if (_CursorInfo.flags == 0x00000001)
                    {
                        _lastCursorInfo = _CursorInfo;
                        _Icon = CopyIcon(_CursorInfo.hCursor);
                        if (GetIconInfo(_Icon, out _IconInfo))
                        {
                            _CX = _CursorInfo.ptScreenPos.X - _IconInfo.xHotspot;
                            _CY = _CursorInfo.ptScreenPos.Y - _IconInfo.yHotspot;
                            return Icon.FromHandle(_Icon).ToBitmap();
                        }
                    }
                    else
                    {
                        _CursorInfo = _lastCursorInfo;
                        if (_CursorInfo.flags == 0x00000001)
                        {
                            _Icon = CopyIcon(_CursorInfo.hCursor);

                            if (GetIconInfo(_Icon, out _IconInfo))
                            {
                                _CX = _CursorInfo.ptScreenPos.X - _IconInfo.xHotspot;
                                _CY = _CursorInfo.ptScreenPos.Y - _IconInfo.yHotspot;
                                return Icon.FromHandle(_Icon).ToBitmap();
                            }
                        }
                    }

                }
                return null;
            }
            catch (Exception e)
            {
                throw;
            }

        }

        public static Bitmap KiResizeImage(Bitmap bmp, int newW, int newH)
        {
            try
            {
                Bitmap b = new Bitmap(newW, newH);
                Graphics g = Graphics.FromImage(b);
                // 插值算法的质量
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(bmp, new Rectangle(0, 0, newW, newH), new Rectangle(0, 0, bmp.Width, bmp.Height), GraphicsUnit.Pixel);
                g.Dispose();
                return b;
            }
            catch
            {
                return null;
            }
        }


        #endregion
    }
    public class MixerVideoCapturer : CameraCapturer
    {
        private FFScale _ffscale = null;
        private Action<byte[]> _callBack = null;
        private bool _isworking = false;
        private Thread _CaptureThread = null;
        private int _width;
        private int _height;
        private int _fps;
        private Mixer.Video.Canvas Canvas { get; set; }
        public MixerVideoCapturer(Mixer.Video.Canvas canvas, int width, int height, int fps, Action<byte[]> callBack)
        {
            _width = width;
            _height = height;
            _fps = fps;
            _callBack = callBack;
            this.Canvas = canvas;
            _ffscale = new FFScale(width, height, 3, 24, width, height, 0, 12);
        }

        public override void Start()
        {
            if (_isworking)
                return;
            _isworking = true;

            _CaptureThread = ThreadEx.ThreadCall(CaptureThread);

        }

        public override void Stop()
        {
            if (!_isworking)
                return;
            _isworking = false;
            ThreadEx.ThreadStop(_CaptureThread);
        }

        private void CaptureThread()
        {
            while (_isworking)
            {
                var dt = DateTime.Now;

                var yuv = Capture();

                if (_callBack != null)
                {
                    _callBack(yuv);
                }
                var sleep = (int)(1000 / _fps - (DateTime.Now - dt).TotalMilliseconds);
                if (sleep > 0)
                    Thread.Sleep(sleep);
                Console.WriteLine((DateTime.Now - dt).TotalMilliseconds);
            }
        }
        private byte[] Capture()
        {
            try
            {
                Bitmap bitmap = new Bitmap(this.Canvas.Size.Width, this.Canvas.Size.Height);
                using (GraphicsBase g = GraphicsGDIPuls.FromImage(bitmap))
                {
                    this.Canvas.Draw(g);
                }
                BitmapData imageData = bitmap.LockBits(new Rectangle(0, 0, _width, _height), ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                byte[] bs = new byte[_width * _height * 3];
                Marshal.Copy(imageData.Scan0, bs, 0, bs.Length);
                bitmap.UnlockBits(imageData);
                bitmap.Dispose();
                var @out = _ffscale.Convert(bs);
                return @out;
            }
            catch (Exception e)
            {

                //if (AppConfig._D)
                //    throw;

                return null;
            }
        }
    }
    public class MixerVideoEncoder : CameraEncoder
    {

        public MixerVideoEncoder(Mixer.Video.Canvas canvas, VideoEncodeCfg cfgVideo, Action<MediaFrame> callBack)
            : base(cfgVideo, callBack, canvas)
        {
            base.Canvas = canvas;
        }
        protected override CameraCapturer CreateCapturer()
        {
            return new MixerVideoCapturer(base.Canvas, _cfgVideo.Width, _cfgVideo.Height, _cfgVideo.FrameRate, CameraCapturer_CallBack);
        }
    }
    public class ScreenaEncoder : CameraEncoder
    {
        public ScreenaEncoder(VideoEncodeCfg cfgVideo, Action<MediaFrame> callBack)
            : base(cfgVideo, callBack)
        {
        }
        protected override CameraCapturer CreateCapturer()
        {
            return new ScreenCapturer(_cfgVideo.Width, _cfgVideo.Height, _cfgVideo.FrameRate, CameraCapturer_CallBack);
        }
    }
    public class CameraEncoder : IDisposable
    {
        public static int CameraCapturerMode = System.Configuration.ConfigurationManager.AppSettings["CameraCapturerMode"] == "1" ? 1 : 0;
        protected bool _isworking = false;
        protected CameraCapturer _capturer = null;
        protected X264Native _x264;
        protected FFScale _ffscale = null;

        protected Action<MediaFrame> _callBack = null;
        protected IYUVDraw _draw = null;
        protected int _fps = 0;
        protected long _lastTick = 0;
        protected VideoEncodeCfg _cfgVideo = null;
        private bool _isFirstKeyFrame = true;
        private bool _needForceIDRFrame = false;
        private bool _needClearVideoTransportBuffer = true;
        public event EventHandler<EventArgsEx<Exception>> Error;
        public Canvas Canvas { get; set; }
        public CameraEncoder(VideoEncodeCfg cfgVideo, Action<MediaFrame> callBack, Canvas canvas = null)
        {
            _cfgVideo = cfgVideo;
            _fps = cfgVideo.FrameRate;
            this.Canvas = canvas;
            _capturer = CreateCapturer();
            var @params = new X264Params(_cfgVideo.Width, _cfgVideo.Height, _fps, cfgVideo.VideoBitRate);
            if (cfgVideo.Params.ContainsKey("X264Encode"))
                @params.method = (int)cfgVideo.Params["X264Encode"];

            if (cfgVideo.Params.ContainsKey("KeyFrameRate"))
                @params.key_frame_max = (int)cfgVideo.Params["KeyFrameRate"];


            _x264 = new X264Native(@params);
            _x264.Init();
            _ffscale = new FFScale(_cfgVideo.Width, _cfgVideo.Height, 0, 12, _cfgVideo.Width, _cfgVideo.Height, 12, 12);
            _draw = cfgVideo.Draw;
            _draw.SetSize(_cfgVideo.Width, _cfgVideo.Height);
            _callBack = callBack;
        }

        protected virtual CameraCapturer CreateCapturer()
        {
            if (CameraCapturerMode == 0)
                return new CameraCapturer(_cfgVideo.CameraId, _cfgVideo.Width, _cfgVideo.Height, CameraCapturer_CallBack);
            else
                return new CameraAForge(_cfgVideo.CameraId, _cfgVideo.Width, _cfgVideo.Height, CameraCapturer_CallBack);
        }


        public void Start()
        {
            if (_isworking)
                return;
            _isworking = true;
            _draw.Start();
            _capturer.Start();


        }

        public void Stop()
        {
            if (!_isworking)
                return;
            _isworking = false;

            _capturer.Stop();
            _draw.Stop();
        }

        public void SetVBR(int bit_k)
        {
            if (_x264 != null)
                _x264.SetBitrate(bit_k);
        }


        protected void CameraCapturer_CallBack(byte[] yuv)
        {
            if (!_isworking)
                return;
            try
            {
                Draw(yuv);

                if ((Environment.TickCount - _lastTick) < 1000 / _fps)
                {
                    return;
                }

                _lastTick = Environment.TickCount;

                var yv12 = _ffscale.Convert(yuv);

                if (_needForceIDRFrame)
                {
                    _needForceIDRFrame = false;
                    _x264.ForceIDRFrame();

                }

                var enc = _x264.Encode(yv12);



                var mf = new MediaFrame()
                {
                    Width = _cfgVideo.Width,
                    Height = _cfgVideo.Height,
                    Ex = 1,
                    IsAudio = 0,
                    Encoder = MediaFrame.H264Encoder,
                    IsKeyFrame = (byte)(_x264.IsKeyFrame() ? 1 : 0),
                    MediaFrameVersion = 0,
                    PPSLen = 0,
                    SPSLen = 0,
                    Size = enc.Length,
                    NTimetick = Environment.TickCount,
                };
                mf.SetData(enc);
                if (mf.IsKeyFrame == 1)
                {
                    var sps_pps = Media.MediaSteamConverter.GetSPS_PPS(enc);
                    mf.SPSLen = sps_pps[0].Length;
                    mf.PPSLen = sps_pps[1].Length;
                }
                mf.MediaFrameVersion = mf.IsKeyFrame;



                if (_needClearVideoTransportBuffer)
                {
                    _needClearVideoTransportBuffer = false;
                    var frame = CreateClearVideoTransportBufferMediaFrame(mf);
                    if (_callBack != null)
                        _callBack(frame);
                }

                if (_isFirstKeyFrame)
                {
                    if (mf.IsKeyFrame == 1)
                    {
                        mf.Ex = 0;
                    }
                    _isFirstKeyFrame = false;
                    var frame = CreateResetCodecMediaFrame(mf);
                    if (_callBack != null)
                        _callBack(frame);
                }

                if (_callBack != null)
                    _callBack(mf);
            }
            catch (Exception e)
            {
                if (_isworking)
                    throw;
            }
        }

        protected MediaFrame CreateClearVideoTransportBufferMediaFrame(MediaFrame mf)
        {

            var frame = MediaFrame.CreateCommandMediaFrame(false, MediaFrameCommandType.ClearVideoTransportBuffer);
            return frame;
        }

        protected MediaFrame CreateResetCodecMediaFrame(MediaFrame mf)
        {
            var infoMediaFrame = new MediaFrame()
            {
                Width = mf.Width,
                Height = mf.Height,
                Ex = mf.Ex,
                IsAudio = mf.IsAudio,
                Encoder = mf.Encoder,
                IsKeyFrame = mf.IsKeyFrame,
                MediaFrameVersion = mf.MediaFrameVersion,
                PPSLen = mf.PPSLen,
                SPSLen = mf.SPSLen,
                NTimetick = mf.NTimetick,
                Size = 0,
            };
            var resetCodecMediaFrame = MediaFrame.CreateCommandMediaFrame(false, MediaFrameCommandType.ResetCodec, infoMediaFrame.GetBytes());
            return resetCodecMediaFrame;
        }

        protected void Draw(byte[] yuv)
        {
            _draw.Draw(yuv);
        }

        public void Dispose()
        {
            try
            {
                if (_isworking)
                    Stop();
                _capturer.Dispose();
                _ffscale.Release();
                _x264.Release();
                _draw.Release();
            }
            catch (Exception e)
            {
                throw;
            }
        }

    }


}
