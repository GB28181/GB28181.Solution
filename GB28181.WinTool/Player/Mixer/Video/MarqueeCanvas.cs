using System;
using System.Drawing;

namespace GB28181.WinTool.Mixer.Video
{
    public class MarqueeCanvas : StringCanvas
    {
        /// <summary>
        /// 滚动延时
        /// </summary>
        public int ScrollDelay { get; set; }
        /// <summary>
        /// 滚动方向
        /// </summary>
        public Direction Direction { get; set; }

        private Point LastPoint;
        private SizeF FontSizeF = Size.Empty;
        private DateTime LastDrawTime = DateTime.MinValue;
        public MarqueeCanvas(Point location, Size size, Point padding, string content, System.Drawing.Font font, System.Drawing.Brush brush, int scrolldelay, Direction direction)
              : base(location, size, padding, content, font, brush)
        {
            this.ScrollDelay = scrolldelay;
            this.Direction = direction;
            this.LastPoint = new Point(location.X + padding.X, location.Y + padding.Y);
        }
        public override void Draw(GraphicsBase g)
        {
            if (LastDrawTime == DateTime.MinValue)
                LastDrawTime = DateTime.Now;
            Point newPoint = RePoint(g);
            g.DrawString(this.Content, this.Font, this.Brush, newPoint);
          
            LastPoint = newPoint;
            LastDrawTime = DateTime.Now;
        }
        public Point RePoint(GraphicsBase g)
        {
            if (this.FontSizeF == SizeF.Empty)
            {
                this.FontSizeF = g.MeasureString(this.Content, this.Font);
            }
            int offset = (int)((DateTime.Now - LastDrawTime).TotalMilliseconds / this.ScrollDelay) + 1;
            int x = (int)(LastPoint.X - ((Direction == Direction.Left) ? offset : 0));
            int y = (int)(LastPoint.Y - ((Direction == Direction.Down) ? offset : 0));
            if (x +this.FontSizeF.Width< this.Location.X)
                x = this.Location.X +this.Size.Width;
            if (y + this.FontSizeF.Height< this.Location.X)
                y = this.Location.Y + this.Size.Height;
            return new Point(x, y);
        }
    }

    public enum Direction : byte
    {
        Left,
        Down
    }
}
