using System;
using System.Drawing;

namespace GB28181.WinTool.Mixer.Video
{
    public class TimeCanvas : StringCanvas
    {
        private string _formate = "yyyy年MM月HH日 hh:mm:ss";
        public string Formate { get { return _formate; }set { _formate = value; } }
        public TimeCanvas(Point location, Size size, Point padding, string content, System.Drawing.Font font, System.Drawing.Brush brush, string formate) : base(location, size, padding, content, font, brush)
        {
            if (!string.IsNullOrEmpty(formate))
                this.Formate = formate;
        }
        public override void Draw(GraphicsBase g)
        {
            this.Content = DateTime.Now.ToString(this.Formate);
            base.Draw(g);
        }
    }
}
