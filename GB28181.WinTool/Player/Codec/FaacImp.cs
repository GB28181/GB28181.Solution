using Helpers;
using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace GB28181.WinTool.Codec
{
    public class FaacImp : IDisposable {
        private IntPtr _handle = IntPtr.Zero;
        private int _channel = 0;
        private int _sample = 0;
        private int _bitrate = 0;
        private int _inputSamples = 0;
        private int _maxOutputBytes = 0;
        private int _maxInputBytes = 0;
        private byte[] _outputBytes = null;
       // IntPtr inputSample=IntPtr.Zero;
       // IntPtr maxOutputBytes = IntPtr.Zero;

        byte[] inputSample = new byte[4];
        byte[] maxOutputBytes = new byte[4];
        private ReaderWriterLock rwLock = new ReaderWriterLock();
        public static FaacImp LastFaacImp = null;//该变量只在IE OCX上面使用,其他地方不要使用
        public FaacImp(int channel, int sample, int bitrate) {
            _channel = channel;
            _sample = sample;
            _bitrate = bitrate;
            _handle = faacEncOpen(_sample, _channel, inputSample, maxOutputBytes);
            _inputSamples =BitConverter.ToInt32(inputSample,0);
            _maxOutputBytes =BitConverter.ToInt32(maxOutputBytes,0);
            _maxInputBytes = _inputSamples * 16 / 8;
            InitConfiguration();
            _outputBytes = new byte[_maxOutputBytes];
            LastFaacImp = this;
        }

        private void InitConfiguration() {
            var ptr = faacEncGetCurrentConfiguration(_handle);
            var pConfiguration = FunctionEx.IntPtrToStruct<faacEncConfiguration>(ptr);
            pConfiguration.inputFormat = 1;
            pConfiguration.outputFormat = 1;
            pConfiguration.useTns = 1;
         
            pConfiguration.useLfe = 0;
            pConfiguration.aacObjectType = 2;
            pConfiguration.shortctl = 0;
            pConfiguration.quantqual = 80;
            pConfiguration.bandWidth = 0;
            pConfiguration.bitRate = (uint)_bitrate;
            FunctionEx.IntPtrSetValue(ptr, pConfiguration);
            faacEncSetConfiguration(_handle, ptr);
        }

        public byte[] Encode(byte[] bytes) {
            try
            {
                int len = 0;
                len = faacEncEncode(_handle, bytes, bytes.Length / 2, _outputBytes, _outputBytes.Length);
                byte[] output = new byte[len];
                if (len == 0)
                    return output;
                Array.Copy(_outputBytes, 0, output, 0, len);

                return output;
            }
            catch (Exception e)
            {
                throw;
            }
        }

        public void Dispose()
        {
            if (_handle != IntPtr.Zero)
            {
                faacEncClose(_handle);
                _handle = IntPtr.Zero;
            }
        }


        //libfaac用来处理音频，比如压缩pcm为aac
        const string DLLFile = @"libfaac.dll";

        [DllImport(DLLFile, CallingConvention = CallingConvention.StdCall)]
        //int FAACAPI faacEncGetVersion(char **faac_id_string, char **faac_copyright_string);
        private extern static int faacEncGetVersion(ref IntPtr faac_id_string, ref IntPtr faac_copyright_string);



        [DllImport(DLLFile, CallingConvention = CallingConvention.Cdecl)]
        //faacEncConfigurationPtr FAACAPI faacEncGetCurrentConfiguration(faacEncHandle hEncoder);
        private extern static IntPtr faacEncGetCurrentConfiguration(IntPtr hEncoder);


        [DllImport(DLLFile, CallingConvention = CallingConvention.Cdecl)]
        //int FAACAPI faacEncSetConfiguration(faacEncHandle hEncoder,faacEncConfigurationPtr config);
        private extern static IntPtr faacEncSetConfiguration(IntPtr hEncoder, IntPtr config);

        [DllImport(DLLFile, CallingConvention = CallingConvention.Cdecl)]
        //faacEncHandle FAACAPI faacEncOpen(unsigned long sampleRate, unsigned int numChannels, unsigned long *inputSamples, unsigned long *maxOutputBytes);
        private extern static IntPtr faacEncOpen(int sampleRate, int numChannels,byte[] inputSamples,byte[] maxOutputBytes);


        [DllImport(DLLFile, CallingConvention = CallingConvention.Cdecl)]
        //int FAACAPI faacEncGetDecoderSpecificInfo(faacEncHandle hEncoder, unsigned char **ppBuffer,unsigned long *pSizeOfDecoderSpecificInfo);
        private extern static IntPtr faacEncGetDecoderSpecificInfo(IntPtr hEncoder, ref IntPtr ppBuffer, ref int pSizeOfDecoderSpecificInfo);


        [DllImport(DLLFile, CallingConvention = CallingConvention.Cdecl)]
        //int FAACAPI faacEncEncode(faacEncHandle hEncoder, int32_t * inputBuffer, unsigned int samplesInput, unsigned char *outputBuffer, unsigned int bufferSize);
        private extern static int faacEncEncode(IntPtr hEncoder, IntPtr inputBuffer, int samplesInput, IntPtr outputBuffer, int bufferSize);
        [DllImport(DLLFile, CallingConvention = CallingConvention.Cdecl)]
        private extern static int faacEncEncode(IntPtr hEncoder, byte[] inputBuffer, int samplesInput, byte[] outputBuffer, int bufferSize);



        [DllImport(DLLFile, CallingConvention = CallingConvention.Cdecl)]
        //int FAACAPI faacEncClose(faacEncHandle hEncoder);
        private extern static IntPtr faacEncClose(IntPtr hEncoder);

        #region 配置结构
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct faacEncConfiguration {
            /* config version */

            public int version;

            /* library version */
            public IntPtr name;

            /* copyright string */
            public IntPtr copyright;

            /* MPEG version, 2 or 4 */
            public uint mpegVersion;

            /* AAC object type
             *  #define MAIN 1
                #define LOW  2
                #define SSR  3
                #define LTP  4
             * */

            public uint aacObjectType;

            /* Allow mid/side coding */
            public uint allowMidside;

            /* Use one of the channels as LFE channel */
            public uint useLfe;

            /* Use Temporal Noise Shaping */
            public uint useTns;

            /* bitrate / channel of AAC file */
            public uint bitRate;

            /* AAC file frequency bandwidth */
            public uint bandWidth;

            /* Quantizer quality */
            public uint quantqual;

            /* Bitstream output format (0 = Raw; 1 = ADTS) */
            public int outputFormat;

            /* psychoacoustic model list */
            public IntPtr psymodellist;

            /* selected index in psymodellist */
            public int psymodelidx;

            /*
                PCM Sample Input Format
                0	FAAC_INPUT_NULL			invalid, signifies a misconfigured config
                1	FAAC_INPUT_16BIT		native endian 16bit
                2	FAAC_INPUT_24BIT		native endian 24bit in 24 bits		(not implemented)
                3	FAAC_INPUT_32BIT		native endian 24bit in 32 bits		(DEFAULT)
                4	FAAC_INPUT_FLOAT		32bit floating point
            */
            public int inputFormat;

            /* block type enforcing (SHORTCTL_NORMAL/SHORTCTL_NOSHORT/SHORTCTL_NOLONG) */
            // #define FAAC_INPUT_NULL    0
            //#define FAAC_INPUT_16BIT   1
            //#define FAAC_INPUT_24BIT   2
            //#define FAAC_INPUT_32BIT   3
            //#define FAAC_INPUT_FLOAT   4

            //#define SHORTCTL_NORMAL    0
            //#define SHORTCTL_NOSHORT   1
            //#define SHORTCTL_NOLONG    2
            public int shortctl;

            /*
                Channel Remapping

                Default			0, 1, 2, 3 ... 63  (64 is MAX_CHANNELS in coder.h)

                WAVE 4.0		2, 0, 1, 3
                WAVE 5.0		2, 0, 1, 3, 4
                WAVE 5.1		2, 0, 1, 4, 5, 3
                AIFF 5.1		2, 0, 3, 1, 4, 5 
            */
            [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.I4, SizeConst = 64)]
            public int[] channel_map;

        }
        #endregion
    }

}
