using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace StreamingKit.Wave.Wave.Native
{
    /// <summary>
    /// This class represents WAVEOUTCAPS structure.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct WAVEOUTCAPS
    {
        /// <summary>
        /// Manufacturer identifier for the device driver for the device.
        /// </summary>
        public ushort wMid;
        /// <summary>
        /// Product identifier for the device.
        /// </summary>
        public ushort wPid;
        /// <summary>
        /// Version number of the device driver for the device.
        /// </summary>
        public uint vDriverVersion;
        /// <summary>
        /// Product name in a null-terminated string.
        /// </summary>
        [MarshalAs(UnmanagedType.ByValTStr,SizeConst = 32)]
        public string szPname;
        /// <summary>
        /// Standard formats that are supported.
        /// </summary>
        public uint dwFormats;
        /// <summary>
        /// Number specifying whether the device supports mono (1) or stereo (2) output.
        /// </summary>
        public ushort wChannels;
        /// <summary>
        /// Packing.
        /// </summary>
        public ushort wReserved1;
        /// <summary>
        /// Optional functionality supported by the device.
        /// </summary>
        public uint dwSupport;
    }
}
