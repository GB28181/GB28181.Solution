using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Runtime.InteropServices;

using SS.Media.Wave.Wave.Native;

namespace SS.Media.Wave.Wave
{
    /// <summary>
    /// This class implements streaming wav data player.
    /// </summary>
    public class WaveOut : IDisposable {
        #region class PlayItem

        /// <summary>
        /// This class holds queued wav play item.
        /// </summary>
        internal class PlayItem {
            private GCHandle m_HeaderHandle;
            private GCHandle m_DataHandle;
            private int m_DataSize = 0;

            /// <summary>
            /// Default constructor.
            /// </summary>
            /// <param name="headerHandle">Header handle.</param>
            /// <param name="header">Wav header.</param>
            /// <param name="dataHandle">Wav header data handle.</param>
            /// <param name="dataSize">Data size in bytes.</param>
            public PlayItem(ref GCHandle headerHandle, ref GCHandle dataHandle, int dataSize) {
                m_HeaderHandle = headerHandle;
                m_DataHandle = dataHandle;
                m_DataSize = dataSize;
            }

            #region method Dispose

            /// <summary>
            /// Cleans up any resources being used.
            /// </summary>
            public void Dispose() {
                m_HeaderHandle.Free();
                m_DataHandle.Free();
            }

            #endregion


            #region Properties Implementation

            /// <summary>
            /// Gets header handle.
            /// </summary>
            public GCHandle HeaderHandle {
                get { return m_HeaderHandle; }
            }

            /// <summary>
            /// Gets header.
            /// </summary>
            public WAVEHDR Header {
                get { return (WAVEHDR)m_HeaderHandle.Target; }
            }

            /// <summary>
            /// Gets wav header data pointer handle.
            /// </summary>
            public GCHandle DataHandle {
                get { return m_DataHandle; }
            }

            /// <summary>
            /// Gets wav header data size in bytes.
            /// </summary>
            public int DataSize {
                get { return m_DataSize; }
            }

            #endregion

        }

        #endregion

        private WavOutDevice m_pOutDevice = null;
        private int m_SamplesPerSec = 8000;
        private int m_BitsPerSample = 16;
        private int m_Channels = 1;
        private int m_MinBuffer = 1200;
        private IntPtr m_pWavDevHandle = IntPtr.Zero;
        private int m_BlockSize = 0;
        private int m_BytesBuffered = 0;
        private bool m_IsBuffing = false;
        private bool m_IsPaused = true;
        private List<PlayItem> m_pPlayItems = null;
        private waveOutProc m_pWaveOutProc = null;
        private bool m_IsDisposed = false;


        public int Volume {
            get {
                ushort left = 0, right = 0;
                GetVolume(ref left, ref right);
                return left * 100 / 65535;
            }
            set {
                SetVolume((ushort)(value * 65535 / 100), (ushort)(value * 65535 / 100));
            }
        }


        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="outputDevice">Output device.</param>
        /// <param name="samplesPerSec">Sample rate, in samples per second (hertz). For PCM common values are 
        /// 8.0 kHz, 11.025 kHz, 22.05 kHz, and 44.1 kHz.</param>
        /// <param name="bitsPerSample">Bits per sample. For PCM 8 or 16 are the only valid values.</param>
        /// <param name="channels">Number of channels.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>outputDevice</b> is null.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the aruments has invalid value.</exception>
        public WaveOut(WavOutDevice outputDevice, int samplesPerSec, int bitsPerSample, int channels) {
            if (outputDevice == null) {
                throw new ArgumentNullException("outputDevice");
            }
            if (samplesPerSec < 8000) {
                throw new ArgumentException("Argument 'samplesPerSec' value must be >= 8000.");
            }
            if (bitsPerSample < 8) {
                throw new ArgumentException("Argument 'bitsPerSample' value must be >= 8.");
            }
            if (channels < 1) {
                throw new ArgumentException("Argument 'channels' value must be >= 1.");
            }

            m_pOutDevice = outputDevice;
            m_SamplesPerSec = samplesPerSec;
            m_BitsPerSample = bitsPerSample;
            m_Channels = channels;
            m_BlockSize = m_Channels * (m_BitsPerSample / 8);
            m_pPlayItems = new List<PlayItem>();

            // Try to open wav device.            
            WAVEFORMATEX format = new WAVEFORMATEX();
            format.wFormatTag = WavFormat.PCM;
            format.nChannels = (ushort)m_Channels;
            format.nSamplesPerSec = (uint)samplesPerSec ;
            //1秒钟数据量（字节/秒）= (采样频率（Hz）*采样位数（bit）*声道数)/ 8
            //换算成20ms的数据量则再除50
            format.nAvgBytesPerSec = (uint)((m_SamplesPerSec * m_Channels * (m_BitsPerSample / 8))) / 50;
        
            format.nBlockAlign = (ushort)m_BlockSize;
            format.wBitsPerSample = (ushort)m_BitsPerSample;
            format.cbSize = 0;

            m_MinBuffer = (int)format.nAvgBytesPerSec * 10;
            // We must delegate reference, otherwise GC will collect it.
            m_pWaveOutProc = new waveOutProc(this.OnWaveOutProc);
            int result = WavMethods.waveOutOpen(out m_pWavDevHandle, m_pOutDevice.Index, format, m_pWaveOutProc, 0, WavConstants.CALLBACK_FUNCTION);
            if (result != MMSYSERR.NOERROR) {
                throw new Exception("Failed to open wav device, error: " + result.ToString() + ".");
            }
        }

        /// <summary>
        /// Default destructor.
        /// </summary>
        ~WaveOut() {
            Dispose();
        }

        #region method Dispose

        #endregion


        #region method OnWaveOutProc

        /// <summary>
        /// This method is called when wav device generates some event.
        /// </summary>
        /// <param name="hdrvr">Handle to the waveform-audio device associated with the callback.</param>
        /// <param name="uMsg">Waveform-audio output message.</param>
        /// <param name="dwUser">User-instance data specified with waveOutOpen.</param>
        /// <param name="dwParam1">Message parameter.</param>
        /// <param name="dwParam2">Message parameter.</param>
        private void OnWaveOutProc(IntPtr hdrvr, int uMsg, int dwUser, int dwParam1, int dwParam2) {
            // NOTE: MSDN warns, we may not call any wav related methods here.

            try {
                if (uMsg == WavConstants.MM_WOM_DONE) {
                    ThreadPool.QueueUserWorkItem(new WaitCallback(this.OnCleanUpFirstBlock));
                }
            } catch {
            }
        }

        #endregion

        #region method OnCleanUpFirstBlock

        /// <summary>
        /// Cleans up the first data block in play queue.
        /// </summary>
        /// <param name="state">User data.</param>
        private void OnCleanUpFirstBlock(object state) {
            if (m_IsDisposed)
                return;
          
            try {
                lock (m_pPlayItems) {
                    PlayItem item = m_pPlayItems[0];
                    WavMethods.waveOutUnprepareHeader(m_pWavDevHandle, item.HeaderHandle.AddrOfPinnedObject(), Marshal.SizeOf(item.Header));
                    m_pPlayItems.Remove(item);
                    
                    m_BytesBuffered -= item.DataSize;
                    item.Dispose();
                }
            } catch {
            }
        }

        #endregion

        public void Pause() {
            if (m_IsPaused)
                return;
            lock (m_pPlayItems) {
                m_IsPaused = true;
                WavMethods.waveOutPause(m_pWavDevHandle);
            }
        }
        public void Continue() {
            if (!m_IsPaused)
                return;
            lock (m_pPlayItems) {
                m_IsPaused = false;
                WavMethods.waveOutRestart(m_pWavDevHandle);
            }
        }
        public void Play(byte[] audioData) {
            Play(audioData, 0, audioData.Length);
        }

        public void Play(byte[] audioData, int offset, int count) {
            if (m_IsDisposed) {
                throw new ObjectDisposedException("WaveOut");
            }
            if (audioData == null) {
                throw new ArgumentNullException("audioData");
            }
            if ((count % m_BlockSize) != 0) {
                throw new ArgumentException("Audio data is not n * BlockSize.");
            }
            while (m_pPlayItems.Count > 10)
                Thread.Sleep(10);
            byte[] data = new byte[count];
            Array.Copy(audioData, offset, data, 0, count);
            GCHandle dataHandle = GCHandle.Alloc(data, GCHandleType.Pinned);

            WAVEHDR wavHeader = new WAVEHDR();
            wavHeader.lpData = dataHandle.AddrOfPinnedObject();

            wavHeader.dwBufferLength = (uint)data.Length;
            wavHeader.dwBytesRecorded = 0;
            wavHeader.dwUser = IntPtr.Zero;
            wavHeader.dwFlags = 0;
            wavHeader.dwLoops = 0;
            wavHeader.lpNext = IntPtr.Zero;
            wavHeader.reserved = 0;
            GCHandle headerHandle = GCHandle.Alloc(wavHeader, GCHandleType.Pinned);
            int result = 0;

            result = WavMethods.waveOutPrepareHeader(m_pWavDevHandle, headerHandle.AddrOfPinnedObject(), Marshal.SizeOf(wavHeader));
 
            if (result == MMSYSERR.NOERROR) {
                PlayItem item = new PlayItem(ref headerHandle, ref dataHandle, data.Length);
                lock (m_pPlayItems) {
                    m_pPlayItems.Add(item);
                    m_BytesBuffered += data.Length;

                    if (m_BytesBuffered < m_MinBuffer/10) {
                        if (!m_IsBuffing) {
                            WavMethods.waveOutPause(m_pWavDevHandle);
                            m_IsBuffing = true;
                        }
                    } else if (m_IsBuffing && m_BytesBuffered > m_MinBuffer) {
                        WavMethods.waveOutRestart(m_pWavDevHandle);
                        m_IsBuffing = false;
                    }

                   

                    result = WavMethods.waveOutWrite(m_pWavDevHandle, headerHandle.AddrOfPinnedObject(), Marshal.SizeOf(wavHeader));
                }
            } else {
                dataHandle.Free();
                headerHandle.Free();
            }
            //--------------------------------------------------------------------------------------------------
        }

        #region method GetVolume

        /// <summary>
        /// Gets audio output volume.
        /// </summary>
        /// <param name="left">Left channel volume level.</param>
        /// <param name="right">Right channel volume level.</param>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this method is accessed.</exception>
        public void GetVolume(ref ushort left, ref ushort right) {
            if (m_IsDisposed) {
                throw new ObjectDisposedException("WaveOut");
            }

            int volume = 0;
            WavMethods.waveOutGetVolume(m_pWavDevHandle, out volume);

            left = (ushort)(volume & 0x0000ffff);
            right = (ushort)(volume >> 16);
        }

        


        #endregion

        #region method SetVolume

        /// <summary>
        /// Sets audio output volume.
        /// </summary>
        /// <param name="left">Left channel volume level.</param>
        /// <param name="right">Right channel volume level.</param>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this method is accessed.</exception>
        public void SetVolume(ushort left, ushort right) {
            if (m_IsDisposed) {
                throw new ObjectDisposedException("WaveOut");
            }

            WavMethods.waveOutSetVolume(m_pWavDevHandle, (right << 16 | left & 0xFFFF));
        }

        #endregion


        #region Properties Implementation

        /// <summary>
        /// Gets all available output audio devices.
        /// </summary>
        public static WavOutDevice[] Devices {
            get {
                List<WavOutDevice> retVal = new List<WavOutDevice>();
                // Get all available output devices and their info.
                int devicesCount = WavMethods.waveOutGetNumDevs();
                for (int i = 0; i < devicesCount; i++) {
                    WAVEOUTCAPS pwoc = new WAVEOUTCAPS();
                    if (WavMethods.waveOutGetDevCaps((uint)i, ref pwoc, Marshal.SizeOf(pwoc)) == MMSYSERR.NOERROR) {
                        retVal.Add(new WavOutDevice(i, pwoc.szPname, pwoc.wChannels));
                    }
                }

                return retVal.ToArray();
            }
        }


        /// <summary>
        /// Gets if this object is disposed.
        /// </summary>
        public bool IsDisposed {
            get { return m_IsDisposed; }
        }

        /// <summary>
        /// Gets current output device.
        /// </summary>
        /// <exception cref="">Is raised when this object is disposed and this property is accessed.</exception>
        public WavOutDevice OutputDevice {
            get {
                if (m_IsDisposed) {
                    throw new ObjectDisposedException("WaveOut");
                }

                return m_pOutDevice;
            }
        }

        /// <summary>
        /// Gets number of samples per second.
        /// </summary>
        /// <exception cref="">Is raised when this object is disposed and this property is accessed.</exception>
        public int SamplesPerSec {
            get {
                if (m_IsDisposed) {
                    throw new ObjectDisposedException("WaveOut");
                }

                return m_SamplesPerSec;
            }
        }

        /// <summary>
        /// Gets number of buts per sample.
        /// </summary>
        /// <exception cref="">Is raised when this object is disposed and this property is accessed.</exception>
        public int BitsPerSample {
            get {
                if (m_IsDisposed) {
                    throw new ObjectDisposedException("WaveOut");
                }

                return m_BitsPerSample;
            }
        }

        /// <summary>
        /// Gets number of channels.
        /// </summary>
        /// <exception cref="">Is raised when this object is disposed and this property is accessed.</exception>
        public int Channels {
            get {
                if (m_IsDisposed) {
                    throw new ObjectDisposedException("WaveOut");
                }

                return m_Channels;
            }
        }

        /// <summary>
        /// Gets one smaple block size in bytes.
        /// </summary>
        /// <exception cref="">Is raised when this object is disposed and this property is accessed.</exception>
        public int BlockSize {
            get {
                if (m_IsDisposed) {
                    throw new ObjectDisposedException("WaveOut");
                }

                return m_BlockSize;
            }
        }

        /// <summary>
        /// Gets if wav player is currently playing something.
        /// </summary>
        /// <exception cref="">Is raised when this object is disposed and this property is accessed.</exception>
        public bool IsPlaying {
            get {
                if (m_IsDisposed) {
                    throw new ObjectDisposedException("WaveOut");
                }

                if (m_pPlayItems.Count > 0) {
                    return true;
                } else {
                    return false;
                }
            }
        }

        #endregion



        /// <summary>
        /// Cleans up any resources being used.
        /// </summary>
        public void Dispose() {
            if (m_IsDisposed) {
                return;
            }
            m_IsDisposed = true;
            lock (m_pPlayItems) {
                try {
                    // If playing, we need to reset wav device first.
                    WavMethods.waveOutReset(m_pWavDevHandle);
                    // If there are unprepared wav headers, we need to unprepare these.
                    foreach (PlayItem item in m_pPlayItems) {
                        WavMethods.waveOutUnprepareHeader(m_pWavDevHandle, item.HeaderHandle.AddrOfPinnedObject(), Marshal.SizeOf(item.Header));
                        item.Dispose();
                    }
                    // Close output device.
                    WavMethods.waveOutClose(m_pWavDevHandle);
                    m_pOutDevice = null;
                    m_pWavDevHandle = IntPtr.Zero;
                    m_pPlayItems = null;
                    m_pWaveOutProc = null;
                } catch {
                }
            }
        }



    }
}
