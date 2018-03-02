using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace SLW.Media.Wave.Wave.Native
{
    /// <summary>
    /// This class represents WAVEHDR structure.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct WAVEHDR
    {
        /// <summary>
        /// Long pointer to the address of the waveform buffer.
        /// </summary>
        public IntPtr lpData;
        /// <summary>
        /// Specifies the length, in bytes, of the buffer.
        /// </summary>
        public uint dwBufferLength;
        /// <summary>
        /// When the header is used in input, this member specifies how much data is in the buffer. 
        /// When the header is used in output, this member specifies the number of bytes played from the buffer.
        /// </summary>
        public uint dwBytesRecorded;
        /// <summary>
        /// Specifies user data.
        /// </summary>
        public IntPtr dwUser;
        /// <summary>
        /// Specifies information about the buffer.
        /// </summary>
        public uint dwFlags;
        /// <summary>
        /// Specifies the number of times to play the loop.
        /// </summary>
        public uint dwLoops;
        /// <summary>
        /// Reserved. This member is used within the audio driver to maintain a first-in, first-out linked list of headers awaiting playback.
        /// </summary>
        public IntPtr lpNext;
        /// <summary>
        /// Reserved.
        /// </summary>
        public uint reserved;
    }
}
