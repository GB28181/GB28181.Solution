using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using GB28181.WinTool.Codec;
using GB28181.WinTool.Media;
using StreamingKit;

namespace GB28181.WinTool
{
    public partial class PlayerControl : UserControl
    {
        private MediaPlayer _player = null;
        public PlayerControl()
        {
            InitializeComponent(); 
        }

        public void Play(MediaFrame frame)
        {
            _player.Play(frame);

        }
        public void Start()
        {
            YUVGDIDraw draw = new YUVGDIDraw(this);
            _player = new MediaPlayer(draw, false);
            _player.Start();
        }
        public void Stop()
        {
            _player.Stop();
        }
    }
}
