using System.Drawing;
using System.Drawing.Drawing2D;

namespace GB28181.WinTool.Mixer.Video
{
    public class ImageCanvas : Canvas
    {
        public System.Drawing.Image Image;

        public Point padding;
        public ImageCanvas(Point location, Size size, Point padding, System.Drawing.Image image)
            : base(location, size)
        {
            this.Location = location;
            this.Size = size;
            this.CanvasStyle = CanvasStyle.Image;
            this.Image = image;
        }
 
        public override void Draw(GraphicsBase g)
        {
            if (this.Image != null)
                g.DrawImage(this.Image, this.Location);
            
            //画图片
            base.Draw(g);
        }
        public override void Reset()
        {

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
