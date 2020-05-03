using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace GB28181.WinTool.Mixer.Video
{
    public abstract class GraphicsBase : IDisposable
    {
        public abstract void DrawString(string s, Font font, Brush brush, PointF point);

        public abstract void DrawImage(Image image, Point point);
        public abstract SizeF MeasureString(string text, Font font);
        public virtual void Dispose()
        {

        }

    }
    public class GraphicsGDIPuls : GraphicsBase
    {
        Graphics g;

        private GraphicsGDIPuls(Graphics gdi)
        {
            this.g = gdi;
        }
        public static GraphicsGDIPuls FromImage(Image image)
        {
            return new GraphicsGDIPuls(Graphics.FromImage(image));
        }
        public static GraphicsGDIPuls FromHwnd(IntPtr hwnd)
        {
            return new GraphicsGDIPuls(Graphics.FromHwnd(hwnd));
        }
        public override void DrawString(string s, Font font, Brush brush, PointF point)
        {
            g.DrawString(s, font, brush, point);
        }
        public override SizeF MeasureString(string text, Font font)
        {
           return g.MeasureString(text, font);
        }

        public override void DrawImage(Image image, Point point)
        {
            g.DrawImage(image, point);
        }

        public override void Dispose()
        {
            if (g != null)
                g.Dispose();
        }
        public static Bitmap KiResizeImage(Image image, int newW, int newH)
        {
            try
            {
                Bitmap b = new Bitmap(newW, newH);
                Graphics g = Graphics.FromImage(b);
                // 插值算法的质量
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(image, new Rectangle(0, 0, newW, newH), new Rectangle(0, 0, image.Width, image.Height), GraphicsUnit.Pixel);
                g.Dispose();
                return b;
            }
            catch
            {
                return null;
            }
        }
    }
}
