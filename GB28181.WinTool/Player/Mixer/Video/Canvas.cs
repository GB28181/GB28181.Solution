using System;
using System.Collections.Generic;
using System.Drawing;

namespace GB28181.WinTool.Mixer.Video
{
    public class Canvas : IDisposable
    {
        private int _layer = -99;
        /// <summary>
        /// 层（0，表示最顶层，其他都是负的）
        /// </summary>
        public int Layer { get { return _layer; } set { _layer = value; } }
        /// <summary>
        /// CanvasID
        /// </summary>
        public int ID { get; set; }
        /// <summary>
        /// 相对于父画布的位置
        /// </summary>
        public Point Location { get; set; }
        /// <summary>
        /// 大小
        /// </summary>
        public Size Size { get; set; }
       
        /// <summary>
        /// 
        /// </summary>
        private CanvasStyle _style = CanvasStyle.Empty;
        public CanvasStyle CanvasStyle { get { return _style; } set { _style = value; } }

        private Dictionary<int, Canvas> _dic = new Dictionary<int, Canvas>(20);

        protected Dictionary<int, Canvas> DicCnavas

        {
            get { return _dic; }
            set { _dic = value; }
        }
        public Canvas(Point location, Size size)
        {
            this.Location = location;
            this.Size = size;
        }
        public void Add(StringCanvas canvas)
        {
            _dic.Add(canvas.ID, canvas);
        }
        public void Add(ImageCanvas canvas)
        {
            _dic.Add(canvas.ID, canvas);
        }
        public void Add(VideoCanvas canvas)
        {
            _dic.Add(canvas.ID, canvas);
        }
        public void Add(MarqueeCanvas canvas)
        {
            _dic.Add(canvas.ID, canvas);
        }
        public void Add(TimeCanvas canvas)
        {
            _dic.Add(canvas.ID, canvas);
        }
        public void Remove(int id)
        {
            _dic.Remove(id);
        }
        public virtual void Draw(GraphicsBase g)
        {
            foreach (var item in _dic.Values)
            {
                item.Draw(g);
            }
        }

        public virtual void Reset()
        {

        }

        public virtual void Dispose()
        {

        }
    }
}
