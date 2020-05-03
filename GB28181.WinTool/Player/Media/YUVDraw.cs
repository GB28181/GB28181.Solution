using Helpers;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace GB28181.WinTool.Codec
{

    public interface IYUVDraw {
        void Start();
        void Stop();
        void SetSize(int width, int height);
        bool Draw(byte[] buffer);
        bool Draw(IntPtr buffer);
        void Clean();
        void Release();
    }

    public partial class YUVDirectDraw : IYUVDraw {
        private IntPtr _obj = IntPtr.Zero;
        private IntPtr _hwnd = IntPtr.Zero;
        private int _width;
        private int _height;
        private bool _isWorking = false;
        private bool _DDMODE = true;
        public YUVDirectDraw(Control control) {
            _hwnd = control.Handle;
        }
        public YUVDirectDraw(IntPtr hwnd) {
            _hwnd = hwnd;
        }
        public void Start() {
            if (_isWorking)
                return;
            _isWorking = true;

        }
        public void Stop() {
            if (!_isWorking)
                return;
            _isWorking = false;
            Clean();
        }
        public void SetSize(int width, int height) {
            if (_obj != IntPtr.Zero) {
                if (_DDMODE) {
                    if (_obj != IntPtr.Zero)
                        DD_Uninitialize(_obj);
                } else {
                    if (_obj != IntPtr.Zero)
                        ReleaseDirectDraw(_obj);
                }
                //throw new Exception("Reset Size");
            }
            _width = width;
            _height = height;
            if (_DDMODE) {
                DD_Initialize(ref _obj);
            } else {
                int r = 1;
                try {
                    _obj = New();
                    r = InitDirectDraw(_obj, _hwnd, width, height);
                } catch (Exception e) {
                    throw;
                }
            }
        }
        public unsafe virtual bool Draw(byte[] buffer) {
            if (_DDMODE) {
                fixed (void* pBuff = buffer) {
                    IntPtr p = new IntPtr(pBuff);
                    ImageProperties a = new ImageProperties() {
                        dwWidth = _width,
                        dwHeight = _height,
                        dwImageFormat = 0,
                    };
                    a.lpY = (uint)p.ToInt32();
                    a.lpU = (uint)(a.lpY + (_width * _height));
                    a.lpV = (uint)(a.lpU + (_width * _height >> 2));
                    try {
                        var aaa = DD_Draw(_obj, _hwnd, a);
                    } catch (Exception xe) {
                    }
                    return true;
                }

            } else {
                IntPtr ptr = FunctionEx.BytesToIntPtr(buffer);
                try {
                    bool result = DirectDraw(_obj, _hwnd, ptr, buffer.Length);
                    return result;
                } finally {
                    Marshal.FreeHGlobal(ptr);
                }
            }
        }
        public virtual bool Draw(IntPtr buffer) {
            return false;
        }
        public virtual void Clean() {


        }
        public virtual void Release() {
            Stop();
            if (_DDMODE) {
                if (_obj != IntPtr.Zero)
                    DD_Uninitialize(_obj);
            } else {
                if (_obj != IntPtr.Zero)
                    ReleaseDirectDraw(_obj);
            }
            _obj = IntPtr.Zero;
        }
        ~YUVDirectDraw() {
            if (_DDMODE) {
                if (_obj != IntPtr.Zero)
                    DD_Uninitialize(_obj);
            } else {
                if (_obj != IntPtr.Zero)
                    ReleaseDirectDraw(_obj);
            }
            _obj = IntPtr.Zero;
        }
        const string DLLFile = @"YUVDirectDraw.dll";
        [DllImport(DLLFile, CallingConvention = CallingConvention.Cdecl)]
        private extern static IntPtr New();
        [DllImport(DLLFile, CallingConvention = CallingConvention.Cdecl)]
        private extern static int InitDirectDraw(IntPtr obj, IntPtr hwnd, int width, int height);
        [DllImport(DLLFile, CallingConvention = CallingConvention.Cdecl)]
        private extern static bool DirectDraw(IntPtr obj, IntPtr hwnd, IntPtr buffer, int buffer_len);
        [DllImport(DLLFile, CallingConvention = CallingConvention.Cdecl)]
        private extern static void ReleaseDirectDraw(IntPtr obj);
        [DllImport(DLLFile, CallingConvention = CallingConvention.Cdecl)]
        private extern static int DD_Initialize(ref IntPtr handle);
        [DllImport(DLLFile, CallingConvention = CallingConvention.Cdecl)]
        private extern static int DD_Draw(IntPtr handle, IntPtr pBuffer, ImageProperties properties);
        [DllImport(DLLFile, CallingConvention = CallingConvention.Cdecl)]
        private extern static int DD_Uninitialize(IntPtr handle);



        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class ImageProperties {
            [MarshalAs(UnmanagedType.U4)]
            public int dwImageFormat;
            [MarshalAs(UnmanagedType.U4)]
            public int dwWidth;
            [MarshalAs(UnmanagedType.U4)]
            public int dwHeight;
            [MarshalAs(UnmanagedType.U4)]
            public uint lpY;
            [MarshalAs(UnmanagedType.U4)]
            public uint lpU;
            [MarshalAs(UnmanagedType.U4)]
            public uint lpV;
            [MarshalAs(UnmanagedType.U4)]
            public uint lpReserve;
        }


    }

    public partial class YUVGDIDraw : IYUVDraw {
        private Control _control = null;
        private FFScale _scale = null;
        private int _width;
        private int _height;
        private bool _isWorking = false;
        private System.Drawing.Bitmap _image = null;
        private object _sync = new object();
        private byte[] _buffer = null;
        private Thread _thread = null;
        private long _tick = 0;
        public YUVGDIDraw(Control control) {
            _control = control;
        }

        public void Start() {
            if (_isWorking)
                return;
            _isWorking = true;
            _thread = new Thread(DrawThread);
            _thread.Start();
        }

        public void Stop() {
            if (!_isWorking)
                return;
            _isWorking = false;
            Clean();
            _thread.Abort();
            _thread = null;
        }

        public void SetSize(int width, int height) {
            _width = width;
            _height = height;
            _scale = new FFScale(width, height, 0, 12, width, height, 3, 24);
            if (_image != null)
                _image.Dispose();
            _image = null;
            lock (_sync) {
                _buffer = null;
            }
        }

        public virtual bool Draw(byte[] buffer) {
            if (!_isWorking)
                return true;
            lock (_sync) {
                if (_buffer == null) {
                    _buffer = new byte[buffer.Length];
                    buffer.CopyTo(_buffer, 0);
                } else {
                    buffer.CopyTo(_buffer, 0);
                }
                _tick = Environment.TickCount;
            }
            return true;
        }

        private bool _Draw(byte[] buffer) {

            var rgb = _scale.Convert(buffer);
            if (_image == null)
                _image = new Bitmap(_width, _height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

            BitmapData imageData = _image.LockBits(new Rectangle(0, 0, _width, _height), ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

            Marshal.Copy(rgb, 0, imageData.Scan0, rgb.Length);

            _image.UnlockBits(imageData);
            try {
                if (_control.IsHandleCreated && !_control.IsDisposed) {
                    var g = _control.CreateGraphics();
                    g.DrawImage(_image, new Rectangle(0, 0, _control.Width, _control.Height));
                    g.Dispose();
          
                }
            } catch (Exception e) {
                if (_control.IsHandleCreated && !_control.IsDisposed) {
                } else {
                    throw;
                }
            }
            return true;

        }

        public virtual bool Draw(IntPtr buffer) {
            return false;
        }

        private void DrawThread() {
            long tick = 0;
            while (true) {
                lock (_sync) {
                    if (_buffer != null && tick != _tick) {
                        _Draw(_buffer);
                        tick = _tick;
                    }
                }
                Thread.Sleep(5);
            }
        }

        public virtual void Clean() {
            if (_control.IsHandleCreated && !_control.IsDisposed) {
                var g = _control.CreateGraphics();
                g.Clear(Color.Black);
                g.Dispose();
            }
        }

        public void Release() {
            Stop();
            if (_scale != null)
                _scale.Release();
        }

        ~YUVGDIDraw() {
            Release();
        }

    }


}
