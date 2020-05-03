using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GB28181.WinTool.Mixer.Video;
using System.Drawing;
using System.Collections;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Threading;
using GB28181.WinTool.Codec;
using System.Drawing.Imaging;
using GB28181.WinTool.Media;

namespace GB28181.WinTool.Mixer
{
    public sealed class MediaMixerCanvas
    {
        public MixerVideoEncoder MixerVideoEncoder { get; set; }
        public int Width { get; private set; }
        public int Height { get; private set; }

        private const int MAXVIDEO_NUM = 25;

        public Canvas Canvas;

        public VideoCanvasPlay[] monitor = new VideoCanvasPlay[MAXVIDEO_NUM];

        private Hashtable mapRects = new Hashtable(MAXVIDEO_NUM);

        private static int SplitRect(Rectangle rcOrigin, int rows, int cols, ref Hashtable mapRects)
        {
            if (mapRects == null)
                mapRects = new Hashtable(MAXVIDEO_NUM);

            if ((cols < 1) || (rows < 1) || rcOrigin.IsEmpty)
            {
                return 0;
            }

            int origin_left = rcOrigin.Left;
            int origin_top = rcOrigin.Top;

            double sub_height = rcOrigin.Height * 1.0 / (double)rows;
            double sub_width = rcOrigin.Width * 1.0 / (double)cols;

            int count = cols * rows;
            int index;

            for (index = 0; index < count; index++)
            {
                int r = index / cols;
                int c = index % cols;

                ;

                int left = (int)(c * sub_width + origin_left - 1);
                int top = (int)(r * sub_height + origin_top - 1);
                int width = (int)(sub_width + 0.5 - 2);
                int height = (int)(sub_height + 0.5 - 2);

                Rectangle rect = new Rectangle(left, top, width, height);

                mapRects[index] = rect;
            }

            return count;
        }
        private static int SplitRect(Rectangle rcOrigin, int count, ref Hashtable mapRects)
        {
            if (mapRects == null)
                mapRects = new Hashtable(MAXVIDEO_NUM);

            mapRects.Clear();

            if ((count < 1) || rcOrigin.IsEmpty)
            {
                return 0;
            }

            switch (count)
            {
                case 1:
                    {
                        mapRects[0] = rcOrigin;

                        break;
                    }
                case 2:
                    {
                        const int rows = 2;
                        const int cols = 2;

                        Hashtable rects = new Hashtable(MAXVIDEO_NUM);
                        if (SplitRect(rcOrigin, rows, cols, ref rects) != (rows * cols))
                        {
                            return 0;
                        }

                        double sub_height = rcOrigin.Height * 1.0 / (double)rows;
                        double sub_width = rcOrigin.Width * 1.0 / (double)cols;

                        Size size = new Size((int)(sub_width - 2), (int)(2.0 * sub_height + 0.5 - 2));
                        Point point = ((Rectangle)rects[0]).Location;
                        Point point1 = ((Rectangle)rects[1]).Location;

                        mapRects[0] = new Rectangle(point, size);
                        mapRects[1] = new Rectangle(point1, size);


                        break;
                    }
                case 3:
                    {
                        const int rows = 2;
                        const int cols = 2;

                        Hashtable rects = new Hashtable(MAXVIDEO_NUM);
                        if (SplitRect(rcOrigin, rows, cols, ref rects) != (rows * cols))
                        {
                            return 0;
                        }

                        double sub_height = rcOrigin.Height * 1.0 / (double)rows;
                        double sub_width = rcOrigin.Width * 1.0 / (double)cols;

                        mapRects[0] = rects[0];
                        mapRects[1] = rects[1];

                        Size size = new Size((int)(2.0 * sub_width + 0.5 - 2), (int)(sub_height));
                        Point point = ((Rectangle)rects[2]).Location;
                        mapRects[2] = new Rectangle(point, size);

                        break;
                    }
                case 5:
                    {
                        const int rows = 3;
                        const int cols = 3;

                        Hashtable rects = new Hashtable(MAXVIDEO_NUM);
                        if (SplitRect(rcOrigin, rows, cols, ref rects) != (rows * cols))
                        {
                            return 0;
                        }

                        double sub_height = rcOrigin.Height * 1.0 / (double)rows;
                        double sub_width = rcOrigin.Width * 1.0 / (double)cols;

                        Size size = new Size((int)(2.0 * sub_width + 0.5 - 2), (int)(2.0 * sub_height + 0.5 - 2));
                        Point point = ((Rectangle)rects[0]).Location;

                        mapRects[0] = new Rectangle(point, size);
                        mapRects[1] = rects[2];
                        mapRects[2] = rects[5];

                        Size size1 = new Size((int)(2.0 * sub_width + 0.5 - 2), (int)(sub_height));
                        Point point1 = ((Rectangle)rects[6]).Location;
                        mapRects[3] = new Rectangle(point1, size1);

                        mapRects[4] = rects[8];

                        break;
                    }
                case 6:
                    {
                        const int rows = 3;
                        const int cols = 3;

                        Hashtable rects = new Hashtable(MAXVIDEO_NUM);
                        if (SplitRect(rcOrigin, rows, cols, ref rects) != (rows * cols))
                        {
                            return 0;
                        }

                        double sub_height = rcOrigin.Height * 1.0 / (double)rows;
                        double sub_width = rcOrigin.Width * 1.0 / (double)cols;

                        Size size = new Size((int)(2.0 * sub_width + 0.5 - 2), (int)(2.0 * sub_height + 0.5 - 2));
                        Point point = ((Rectangle)rects[0]).Location;

                        mapRects[0] = new Rectangle(point, size);
                        mapRects[1] = rects[2];
                        mapRects[2] = rects[5];
                        mapRects[3] = rects[6];
                        mapRects[4] = rects[7];
                        mapRects[5] = rects[8];

                        break;
                    }

                case 7:
                    {
                        const int rows = 2;
                        const int cols = 2;

                        Hashtable rects = new Hashtable(MAXVIDEO_NUM);
                        if (SplitRect(rcOrigin, rows, cols, ref rects) != (rows * cols))
                        {
                            return 0;
                        }

                        mapRects[0] = rects[0];
                        mapRects[5] = rects[2];
                        mapRects[6] = rects[3];

                        if (SplitRect((Rectangle)rects[1], 2, 2, ref rects) != 4)
                        {
                            mapRects.Clear();
                            return 0;
                        }

                        Rectangle rect0 = (Rectangle)rects[0];
                        Rectangle rect1 = (Rectangle)rects[1];
                        Rectangle rect2 = (Rectangle)rects[2];
                        Rectangle rect3 = (Rectangle)rects[3];


                        rect1.Width += 3;
                        rect2.Height += 3;
                        rect3.Height += 3;
                        rect3.Width += 3;


                        mapRects[1] = rect0;
                        mapRects[2] = rect1;
                        mapRects[3] = rect2;
                        mapRects[4] = rect3;

                        break;
                    }

                case 8:
                    {
                        const int rows = 4;
                        const int cols = 4;

                        Hashtable rects = new Hashtable(MAXVIDEO_NUM);
                        if (SplitRect(rcOrigin, rows, cols, ref rects) != (rows * cols))
                        {
                            return 0;
                        }

                        double sub_height = rcOrigin.Height * 1.0 / (double)rows;
                        double sub_width = rcOrigin.Width * 1.0 / (double)cols;


                        Size size = new Size((int)(3.0 * sub_width + 0.5 - 2), (int)(3.0 * sub_height + 0.5 - 2));
                        Point point = ((Rectangle)rects[0]).Location;

                        mapRects[0] = new Rectangle(point, size);

                        mapRects[1] = rects[3];
                        mapRects[2] = rects[7];
                        mapRects[3] = rects[11];
                        mapRects[4] = rects[12];
                        mapRects[5] = rects[13];
                        mapRects[6] = rects[14];
                        mapRects[7] = rects[15];

                        break;
                    }

                case 10:
                    {
                        const int rows = 4;
                        const int cols = 4;

                        Hashtable rects = new Hashtable(MAXVIDEO_NUM);
                        if (SplitRect(rcOrigin, rows, cols, ref rects) != (rows * cols))
                        {
                            return 0;
                        }

                        double sub_height = rcOrigin.Height * 1.0 / (double)rows;
                        double sub_width = rcOrigin.Width * 1.0 / (double)cols;


                        Size size = new Size((int)(2.0 * sub_width + 0.5 - 2), (int)(2.0 * sub_height + 0.5 - 2));
                        Point point1 = ((Rectangle)rects[0]).Location;
                        Point point2 = ((Rectangle)rects[2]).Location;

                        mapRects[0] = new Rectangle(point1, size);
                        mapRects[1] = new Rectangle(point2, size);

                        mapRects[2] = rects[8];
                        mapRects[3] = rects[9];
                        mapRects[4] = rects[10];
                        mapRects[5] = rects[11];
                        mapRects[6] = rects[12];
                        mapRects[7] = rects[13];
                        mapRects[8] = rects[14];
                        mapRects[9] = rects[15];

                        break;
                    }

                case 11:
                case 12:
                case 13:
                    {
                        const int rows = 4;
                        const int cols = 4;

                        Hashtable rects = new Hashtable(MAXVIDEO_NUM);
                        if (SplitRect(rcOrigin, rows, cols, ref rects) != (rows * cols))
                        {
                            return 0;
                        }

                        double sub_height = rcOrigin.Height * 1.0 / (double)rows;
                        double sub_width = rcOrigin.Width * 1.0 / (double)cols;


                        Size size = new Size((int)(2.0 * sub_width + 0.5 - 2), (int)(2.0 * sub_height + 0.5 - 2));
                        Point point = ((Rectangle)rects[5]).Location;
                        mapRects[5] = new Rectangle(point, size);

                        mapRects[0] = rects[0];
                        mapRects[1] = rects[1];
                        mapRects[2] = rects[2];
                        mapRects[3] = rects[3];
                        mapRects[4] = rects[4];

                        mapRects[6] = rects[7];
                        mapRects[7] = rects[8];
                        mapRects[8] = rects[11];
                        mapRects[9] = rects[12];
                        mapRects[10] = rects[13];
                        mapRects[11] = rects[14];
                        mapRects[12] = rects[15];

                        break;
                    }

                default:
                    {
                        double dfRows = Math.Sqrt(count * 1.0);
                        int lRows = (int)dfRows;

                        if ((dfRows - lRows) > 0.0)
                        {
                            lRows += 1;
                        }

                        return SplitRect(rcOrigin, lRows, lRows, ref mapRects);
                    }
            }

            return mapRects.Count;
        }
        private void CreateRect(int count)
        {
            if (count == 0)
                return;
            Rectangle rect = new Rectangle(new System.Drawing.Point(0, 0), new Size(this.Width, this.Height));
            SplitRect(rect, count, ref mapRects);
        }
        public void Dispaly(int count, int width, int height)
        {

            this.Width = width;
            this.Height = height;
            Canvas = new Canvas(System.Drawing.Point.Empty, new Size(width, height));
            System.Drawing.SolidBrush brush = new SolidBrush(Color.Red);
            System.Drawing.Font font = new System.Drawing.Font("宋体", 22.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));

            if (count < 0 || count > MAXVIDEO_NUM)
                return;
            Image image = Image.FromFile(@"D:\Mixer\SlWClient\SS.WPFClient\Images\bg_module_call.png");
            mapRects.Clear();
            CreateRect(count);
            System.Drawing.Font logFont = new System.Drawing.Font("宋体", 22.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));

            for (int i = 0; i < count; i++)
            {
                if (mapRects[i] != null)
                {
                    Rectangle rect = (Rectangle)mapRects[i];
                    var vc = new VideoCanvas(rect.Location, rect.Size, image);
                    monitor[i] = new VideoCanvasPlay(vc);
                    MarqueeCanvas logmarCanvas = new MarqueeCanvas(vc.Location, vc.Size, new Point(20, 30), "_log", logFont, brush, 60, Direction.Down);
                    logmarCanvas.ID = 35;
                    vc.Add(logmarCanvas);
                    vc.Start();
                    vc.ID = i;
                    Canvas.Add(vc);
                }
            }
            TimeCanvas timecanvas = new TimeCanvas(Point.Empty, new Size(100, 100), Point.Empty, "", font, brush, "");
            timecanvas.ID = 29;
            Canvas.Add(timecanvas);
            System.Drawing.Font font1 = new System.Drawing.Font("宋体", 42.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));

            MarqueeCanvas marCanvas = new MarqueeCanvas(new Point(0, height - 200), new Size(width, 200), new Point(100, 40), "你好，欢迎光临视联动力^*^^*^^*^^*^^*^^*^^*^^*^^*^^*^^*^^*^??!!!!!!!!!!!!!!!!!!!!!!!!!!", font1, brush, 40, Direction.Left);

            ImageCanvas imgCanvas = new ImageCanvas(new Point(width - 150, 0), new Size(300, 400), Point.Empty, Image.FromFile(@"D:\VisionVera\视联动力图标.png"));
            imgCanvas.ID = 30;
            Canvas.Add(imgCanvas);

            marCanvas.ID = 31;
            Canvas.Add(marCanvas);



        }

        public MediaMixerCanvas()
        {


        }
        public MediaMixerCanvas(MixerVideoEncoder me)
        {
            this.MixerVideoEncoder = me;

        }
        public void SetMappingCanvas(int StreamID, int canvasID)
        {
            foreach (var item in monitor)
            {
                if (item.CanvasID == canvasID)
                    item.StreamID = StreamID;
            }
        }
        public void Play(StreamingKit.MediaFrame frame)
        {
            //if(frame.StreamID)
            monitor[0].Play(frame);
            monitor[1].Play(frame);
            monitor[2].Play(frame);
            monitor[3].Play(frame);
        }
        public void Start()
        {
            if (this.MixerVideoEncoder != null)
                this.MixerVideoEncoder.Start();
            foreach (var item in monitor)
            {
                if (item != null)
                    item.Start();
            }
        }
        public void Stop()
        {
            if (this.MixerVideoEncoder != null)
                this.MixerVideoEncoder.Stop();
            foreach (var item in monitor)
            {
                if (item != null)
                    item.Stop();
            }
        }
        public void Dispose()
        {
            foreach (var item in this.monitor)
            {
                if (item != null)
                    item.Dispose();
            }
            if (this.Canvas != null)
                Canvas.Dispose();
            if (this.MixerVideoEncoder != null)
                this.MixerVideoEncoder.Dispose();

        }


    }

    public class VideoCanvasPlay : VideoPlayer
    {
        public int StreamID { get; set; }

        public int CanvasID { get { return canvas.ID; } }

        public VideoCanvas canvas;

        public VideoCanvasPlay(VideoCanvas canvas) : base(canvas)
        {

        }

    }

}
