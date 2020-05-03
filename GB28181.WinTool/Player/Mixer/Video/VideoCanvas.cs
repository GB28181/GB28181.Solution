using GB28181.WinTool.Codec;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace GB28181.WinTool.Mixer.Video
{
    public class VideoCanvas : Canvas, IYUVDraw
    {
        public Bitmap Background;

     
        public int srcWidth;
        public int srcHeight;
        private object _lockObj = new object();

        private FFScale _scale = null;

    
        private byte[] _rgbBuffer = null;
 
   
        public VideoCanvas(Point location, Size size, System.Drawing.Image backgroud)
            : base(location, size)
        {
            this.Location = location;
            this.Size = size;
            this.Background = GraphicsGDIPuls.KiResizeImage(backgroud, size.Width, size.Height);
            this.CanvasStyle = CanvasStyle.Video;
           
        }
       

        public override void Draw(GraphicsBase g)
        {
            lock (_lockObj)
            {
                if (this.Background != null)
                    g.DrawImage(Background, this.Location);
                //画图片
                base.Draw(g);
            }

        }
        public void SetSize(int width, int height)
        {
            lock (_lockObj)
            {
                if (_scale != null)
                {
                    _scale.Release();
                }
                
                _scale = new FFScale(width, height, 0, 12, width, height, 3, 24);
                _rgbBuffer = new byte[width * height * 3];
                this.srcWidth = width;this.srcHeight = height;
                
               
            }
        }

        public override void Reset()
        {

        }

        public void Start()
        {
             
        }

        public void Stop()
        {
             
        }

        string _log = string.Empty;

        public bool  Draw(byte[] buffer)
        {
            _log += DateTime.Now.TimeOfDay + " \r\n";
          ;
            var rgb = _scale.Convert(buffer);
            lock (_lockObj)
            {
                Bitmap bitmap = new Bitmap(this.srcWidth, this.srcHeight, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
               
                BitmapData imageData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

                Marshal.Copy(rgb, 0, imageData.Scan0, rgb.Length);

                bitmap.UnlockBits(imageData);
                var bmp = GraphicsGDIPuls.KiResizeImage(bitmap, this.Size.Width, this.Size.Height);
                bitmap.Dispose();
                using (Graphics g = Graphics.FromImage(this.Background))
                {
                    g.DrawImage(bmp,PointF.Empty);
                    LogOsd(g);
                }
                bmp.Dispose();
                
            }
            return true;
        }
        private void LogOsd(Graphics g)
        {
            foreach (var item in DicCnavas.Values)
            {
                if (item.CanvasStyle == CanvasStyle.String)
                {
                    var temp = (StringCanvas)item;
                    temp.Write(_log);
                }
            }
        }

        public bool  Draw(IntPtr buffer)
        {
            return false;
        }
       
        public void Clean()
        {
            
        }

        public void Release()
        {
            if (this._scale != null)
                this._scale.Release();
            this._scale = null;
        }
        public override void Dispose()
        {
            this.Release();
            if (this.Background != null)
                this.Background.Dispose();
            this.Background = null;
            base.Dispose(); 
        }
    }
}
