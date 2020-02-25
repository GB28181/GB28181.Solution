using System;
using System.Collections.Generic;
using System.Text;

namespace SS.Media.Wave.Wave
{
    /// <summary>
    /// This class represents wav output device.
    /// </summary>
    public class WavOutDevice
    {
        private int    m_Index    = 0;
        private string m_Name     = "";
        private int    m_Channels = 1;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="index">Device index in devices.</param>
        /// <param name="name">Device name.</param>
        /// <param name="channels">Number of audio channels.</param>
        internal WavOutDevice(int index,string name,int channels)
        {
            m_Index    = index;
            m_Name     = name;
            m_Channels = channels;
        }


        #region Properties Implementation

        /// <summary>
        /// Gets device name.
        /// </summary>
        public string Name
        {
            get{ return m_Name; }
        }

        /// <summary>
        /// Gets number of output channels(mono,stereo,...) supported.
        /// </summary>
        public int Channels
        {
            get{ return m_Channels; }
        }


        /// <summary>
        /// Gets device index in devices.
        /// </summary>
        internal int Index
        {
            get{ return m_Index; }
        }

        #endregion

    }
}
