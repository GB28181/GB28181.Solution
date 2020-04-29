using System.Drawing;

namespace GB28181.WinTool.Mixer.Video
{
    public class StringCanvas : Canvas
    {
        /// <summary>
        /// 内容显示的相对位置
        /// </summary>
        public Point Padding { get; set; }
        public string Content { get; set; }
        public Font Font { get; set; }
        public Brush Brush { get; set; }
        public StringCanvas(Point location, Size size, Point padding, string content, System.Drawing.Font font, System.Drawing.Brush brush) : base(location, size)
        {
            this.Location = location;
            this.Size = size;
            this.Content = content;
            this.Font = font;
            this.Brush = brush;
            this.Padding = padding;
            this.CanvasStyle = CanvasStyle.String;
        }
      
        public  void Write(string content)
        {
            this.Content = content;
        }

        public override void Draw(GraphicsBase g)
        {       
            g.DrawString(this.Content, this.Font, this.Brush, new PointF(Location.X, Location.Y));
        }
        public override void Reset()
        {
            this.Content = string.Empty;
        }


    }


}
