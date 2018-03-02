using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Runtime.InteropServices;

using SLW.Media.Wave.Wave.Native;

namespace SLW.Media.Wave.Wave {
    #region Delegates Implementation

    /// <summary>
    /// Represents the method that will handle the <b>WavRecorder.BufferFull</b> event.
    /// </summary>
    /// <param name="buffer">Recorded data.</param>
    public delegate void BufferFullHandler(byte[] buffer);

    #endregion

    /// <summary>
    /// This class implements streaming microphone wav data receiver.
    /// </summary>
    public class WaveIn {
        #region class BufferItem

        /// <summary>
        /// This class holds queued recording buffer.
        /// </summary>
        private class BufferItem {
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
            public BufferItem(ref GCHandle headerHandle, ref GCHandle dataHandle, int dataSize) {
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
            /// Gets wav header data.
            /// </summary>
            public byte[] Data {
                get { return (byte[])m_DataHandle.Target; }
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

        private WavInDevice m_pInDevice = null;
        private int m_SamplesPerSec = 8000;
        private int m_BitsPerSample = 8;
        private int m_Channels = 1;
        private int m_BufferSize = 400;
        private IntPtr m_pWavDevHandle = IntPtr.Zero;
        private int m_BlockSize = 0;
        private List<BufferItem> m_pBuffers = null;
        private waveInProc m_pWaveInProc = null;
        private bool m_IsRecording = false;
        private bool m_IsStoping = false;
        private bool m_IsDisposed = false;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="outputDevice">Input device.</param>
        /// <param name="samplesPerSec">Sample rate, in samples per second (hertz). For PCM common values are 
        /// 8.0 kHz, 11.025 kHz, 22.05 kHz, and 44.1 kHz.</param>
        /// <param name="bitsPerSample">Bits per sample. For PCM 8 or 16 are the only valid values.</param>
        /// <param name="channels">Number of channels.</param>
        /// <param name="bufferSize">Specifies recording buffer size.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>outputDevice</b> is null.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the aruments has invalid value.</exception>
        public WaveIn(WavInDevice device, int samplesPerSec, int bitsPerSample, int channels, int bufferSize) {
            if (device == null) {
                throw new ArgumentNullException("device");
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

            m_pInDevice = device;
            m_SamplesPerSec = samplesPerSec;
            m_BitsPerSample = bitsPerSample;
            m_Channels = channels;
            m_BufferSize = bufferSize;
            m_BlockSize = m_Channels * (m_BitsPerSample / 8);
            m_pBuffers = new List<BufferItem>();

            // Try to open wav device.            
            WAVEFORMATEX format = new WAVEFORMATEX();
            format.wFormatTag = WavFormat.PCM;
            format.nChannels = (ushort)m_Channels;
            format.nSamplesPerSec = (uint)samplesPerSec;
            format.nAvgBytesPerSec = (uint)(m_SamplesPerSec * m_Channels * (m_BitsPerSample / 8));
            format.nBlockAlign = (ushort)m_BlockSize;
            format.wBitsPerSample = (ushort)m_BitsPerSample;
            format.cbSize = 0;
            // We must delegate reference, otherwise GC will collect it.
            m_pWaveInProc = new waveInProc(this.OnWaveInProc);
            int result = WavMethods.waveInOpen(out m_pWavDevHandle, m_pInDevice.Index, format, m_pWaveInProc, 0, WavConstants.CALLBACK_FUNCTION);
            if (result != MMSYSERR.NOERROR) {
                throw new Exception("Failed to open wav device, error: " + result.ToString() + ".");
            }

            EnsureBuffers();
        }

        /// <summary>
        /// Default destructor.
        /// </summary>
        ~WaveIn() {
            Dispose();
        }

        #region method Dispose

        /// <summary>
        /// Cleans up any resources being used.
        /// </summary>
        public void Dispose() {
            if (m_IsDisposed) {
                return;
            }
            m_IsDisposed = true;

            // Release events.
            this.BufferFull = null;

            try {
                // If recording, we need to reset wav device first.
                WavMethods.waveInReset(m_pWavDevHandle);

                // If there are unprepared wav headers, we need to unprepare these.
                foreach (BufferItem item in m_pBuffers) {
                    WavMethods.waveInUnprepareHeader(m_pWavDevHandle, item.HeaderHandle.AddrOfPinnedObject(), Marshal.SizeOf(item.Header));
                    item.Dispose();
                }

                // Close input device.
                WavMethods.waveInClose(m_pWavDevHandle);

                m_pInDevice = null;
                m_pWavDevHandle = IntPtr.Zero;
            } catch {
            }
        }

        #endregion


        #region method Start

        /// <summary>
        /// Starts recording.
        /// </summary>
        public void Start() {
            if (m_IsRecording) {
                return;
            }
            m_IsRecording = true;
            m_IsStoping = false;
            int result = WavMethods.waveInStart(m_pWavDevHandle);
            if (result != MMSYSERR.NOERROR) {
                throw new Exception("Failed to start wav device, error: " + result + ".");
            }
        }

        #endregion

        #region method Stop

        /// <summary>
        /// Stops recording.
        /// </summary>
        public void Stop() {


            if (!m_IsRecording) {
                return;
            }
            m_IsRecording = false;
            m_IsStoping = true;
            int result = WavMethods.waveInStop(m_pWavDevHandle);

            if (result != MMSYSERR.NOERROR) {
                throw new Exception("Failed to stop wav device, error: " + result + ".");
            }
        }

        #endregion


        #region method OnWaveInProc

        /// <summary>
        /// This method is called when wav device generates some event.
        /// </summary>
        /// <param name="hdrvr">Handle to the waveform-audio device associated with the callback.</param>
        /// <param name="uMsg">Waveform-audio input message.</param>
        /// <param name="dwUser">User-instance data specified with waveOutOpen.</param>
        /// <param name="dwParam1">Message parameter.</param>
        /// <param name="dwParam2">Message parameter.</param>
        private void OnWaveInProc(IntPtr hdrvr, int uMsg, int dwUser, int dwParam1, int dwParam2) {
            // NOTE: MSDN warns, we may not call any wav related methods here.

            try {
                if (m_IsDisposed)
                    return;
                if (uMsg == WavConstants.MM_WIM_DATA) {
                    ThreadPool.QueueUserWorkItem(new WaitCallback(this.ProcessFirstBuffer));
                }
            } catch {
            }
        }

        #endregion

        #region method ProcessFirstBuffer

        /// <summary>
        /// Processes first first filled buffer in queue and disposes it if done.
        /// </summary>
        /// <param name="state">User data.</param>
        private void ProcessFirstBuffer(object state) {
            try {

                lock (m_pBuffers) {
                    if (m_IsDisposed)
                        return;
                    BufferItem item = m_pBuffers[0];

                    // Raise BufferFull event.
                    OnBufferFull(item.Data);
                    if (m_IsDisposed)
                        return;
                    // Clean up.
                    WavMethods.waveInUnprepareHeader(m_pWavDevHandle, item.HeaderHandle.AddrOfPinnedObject(), Marshal.SizeOf(item.Header));
                    m_pBuffers.Remove(item);
                    item.Dispose();
                }

                EnsureBuffers();
            } catch {
            }
        }

        #endregion

        #region method EnsureBuffers

        /// <summary>
        /// Fills recording buffers.
        /// </summary>
        private void EnsureBuffers() {
            // We keep 3 x buffer.
            lock (m_pBuffers) {
                while (m_pBuffers.Count < 3 && !m_IsStoping) {
                    byte[] data = new byte[m_BufferSize];
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
                    result = WavMethods.waveInPrepareHeader(m_pWavDevHandle, headerHandle.AddrOfPinnedObject(), Marshal.SizeOf(wavHeader));
                    if (result == MMSYSERR.NOERROR) {
                        m_pBuffers.Add(new BufferItem(ref headerHandle, ref dataHandle, m_BufferSize));

                        result = WavMethods.waveInAddBuffer(m_pWavDevHandle, headerHandle.AddrOfPinnedObject(), Marshal.SizeOf(wavHeader));
                        if (result != MMSYSERR.NOERROR) {
                            throw new Exception("Error adding wave in buffer, error: " + result + ".");
                        }
                    } else if (result == MMSYSERR.NODRIVER) {
                        Stop();
                    }
                }
            }
        }

        #endregion


        #region Properties Implementation

        /// <summary>
        /// Gets all available input audio devices.
        /// </summary>
        public static WavInDevice[] Devices {
            get {
                List<WavInDevice> retVal = new List<WavInDevice>();
                // Get all available output devices and their info.                
                int devicesCount = WavMethods.waveInGetNumDevs();
                for (int i = 0; i < devicesCount; i++) {
                    WAVEOUTCAPS pwoc = new WAVEOUTCAPS();
                    if (WavMethods.waveInGetDevCaps((uint)i, ref pwoc, Marshal.SizeOf(pwoc)) == MMSYSERR.NOERROR) {
                        retVal.Add(new WavInDevice(i, pwoc.szPname, pwoc.wChannels));
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
        /// Gets current input device.
        /// </summary>
        /// <exception cref="">Is raised when this object is disposed and this property is accessed.</exception>
        public WavInDevice InputDevice {
            get {
                if (m_IsDisposed) {
                    throw new ObjectDisposedException("WavRecorder");
                }

                return m_pInDevice;
            }
        }

        /// <summary>
        /// Gets number of samples per second.
        /// </summary>
        /// <exception cref="">Is raised when this object is disposed and this property is accessed.</exception>
        public int SamplesPerSec {
            get {
                if (m_IsDisposed) {
                    throw new ObjectDisposedException("WavRecorder");
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
                    throw new ObjectDisposedException("WavRecorder");
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
                    throw new ObjectDisposedException("WavRecorder");
                }

                return m_Channels;
            }
        }

        /// <summary>
        /// Gets recording buffer size.
        /// </summary>
        /// <exception cref="">Is raised when this object is disposed and this property is accessed.</exception>
        public int BufferSize {
            get {
                if (m_IsDisposed) {
                    throw new ObjectDisposedException("WavRecorder");
                }

                return m_BufferSize;
            }
        }

        // <summary>
        /// Gets one smaple block size in bytes.
        /// </summary>
        /// <exception cref="">Is raised when this object is disposed and this property is accessed.</exception>
        public int BlockSize {
            get {
                if (m_IsDisposed) {
                    throw new ObjectDisposedException("WavRecorder");
                }

                return m_BlockSize;
            }
        }

        #endregion

        #region Events Implementation

        /// <summary>
        /// This event is raised when record buffer is full and application should process it.
        /// </summary>
        public event BufferFullHandler BufferFull = null;

        /// <summary>
        /// This method raises event <b>BufferFull</b> event.
        /// </summary>
        /// <param name="buffer">Receive buffer.</param>
        private void OnBufferFull(byte[] buffer) {
            if (this.BufferFull != null) {
                this.BufferFull(buffer);
            }
        }

        #endregion

    }
}
