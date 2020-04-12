using System;
using System.Collections.Generic;
using System.Text;

namespace StreamingKit.Wave.Wave.Native
{
    /// <summary>
    /// This class provides most used wav constants.
    /// </summary>
    internal class WavConstants
    {
        public const int MM_WOM_OPEN = 0x3BB;
		public const int MM_WOM_CLOSE = 0x3BC;
		public const int MM_WOM_DONE = 0x3BD;

        public const int MM_WIM_OPEN = 0x3BE;   
        public const int MM_WIM_CLOSE = 0x3BF;
        public const int MM_WIM_DATA = 0x3C0;


		public const int CALLBACK_FUNCTION = 0x00030000;

        public const int WAVERR_STILLPLAYING = 0x21;

        public const int WHDR_DONE = 0x00000001;
        public const int WHDR_PREPARED = 0x00000002;
        public const int WHDR_BEGINLOOP = 0x00000004;
        public const int WHDR_ENDLOOP = 0x00000008;
        public const int WHDR_INQUEUE = 0x00000010;

    }
}
