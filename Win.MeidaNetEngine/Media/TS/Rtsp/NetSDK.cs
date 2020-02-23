namespace Win.MediaServer.Media.TS.Rtsp
{
    using System;
    using System.Runtime.InteropServices;
    public class NetSDK
    {
        private static bool _sdkInitFlag;
        public const int PLAY_COUNT = 0x80;

        public static void InitNetSdk()
        {
            if (!_sdkInitFlag)
            {
                RtspLiveInit();
                _sdkInitFlag = true;
            }
        }

        [DllImport("live555.dll")]
        public static extern void RtspLiveClose(uint hd);
        [DllImport("live555.dll")]
        public static extern bool RtspLiveInit();
        [DllImport("live555.dll")]
        public static extern uint RtspLiveOpen(string streamUrl, string userName, string password, bool useTcp, fCallBack x, IntPtr cbParam);
        [DllImport("live555.dll")]
        public static extern void RtspLiveSetRevertPort(short basePort, short count);
        [DllImport("live555.dll")]
        public static extern void RtspLiveUninit();
    }
}

