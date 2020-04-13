using Helpers;
using StreamingKit.Media.TS;
using System;

namespace StreamingKit.Source.TS
{
    public class GB28181TSStreamInput : TSStreamInput
    {
        private long _firstTimeTick = 0;
        private long _lastTimeTick = 0;
        private long _startTimeTick = 0;
        public bool PlaySync = true;
        public event EventHandler End;

        public GB28181TSStreamInput()
            : base(null)
        {
        }

        protected override void OnStart()
        {

        }
        public void Init()
        {
            _startTimeTick = 0;
            _firstTimeTick = 0;
            _lastTimeTick = 0;
        }
        protected override void OnStop()
        {

        }

        protected virtual void OnEnd()
        {
            OnStop();
            if (End != null)
                End(this, new EventArgs());
        }

        public virtual void Restart()
        {
            OnStop();
            OnStart();

        }

        public void DataReceived(byte[] data)
        {
            OnDataReceived(data);
        }

        protected override void NewMediaFrame(MediaFrame frame)
        {
            if (_firstTimeTick == 0 && frame.IsKeyFrame == 1 && frame.IsAudio == 0)
            {
                _firstTimeTick = frame.NTimetick;
                _startTimeTick = DateTime.Now.Ticks / 10000;
            }
            if (_firstTimeTick == 0)
                return;
            if (_lastTimeTick <= frame.NTimetick)
            {
                _lastTimeTick = frame.NTimetick;
                var span = DateTime.Now.Ticks / 10000 - _startTimeTick;
                int sleep = (int)((_lastTimeTick - _firstTimeTick) - span);
                if (sleep > 40)
                    sleep = 40;
                if (PlaySync)
                {
                    if (sleep > 0)
                        ThreadEx.Sleep(sleep);
                }
            }
            base.NewMediaFrame(frame);
        }
    }
}
