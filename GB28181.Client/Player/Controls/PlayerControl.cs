using SLW.ClientBase.Codec;
using SLW.ClientBase.Media;
using SLW.Media;
using System.Windows.Forms;

namespace SLW.ClientBase
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
