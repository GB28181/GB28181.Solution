using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Helpers;
using System.IO;

namespace GB28181.WinTool.Codec {
    public class FaadImp : IDisposable {
         private IntPtr _handle = IntPtr.Zero;
        private Boolean _inited = false;
        static object _lock = new object();
        public FaadImp()
        {
            lock (_lock)
            {
                _handle = NeAACDecOpen();
            }
        }
        public byte[] Decode(byte[] data) {
            if (!_inited) {
                uint samplerate = 0;
                byte channels = 0;
                lock (_lock)
                {
                    var r = NeAACDecInit(_handle, data, (uint)data.Length, ref samplerate, ref channels);
                    if (r == 0)
                    {
                        _inited = true;
                    }
                }
               
            }
            if (_inited)
            {
                NeAACDecFrameInfo info = new NeAACDecFrameInfo();
               var pcm = NeAACDecDecode(_handle,  ref info , data, data.Length);
               try
               {
                   int bufferLenth = info.samples * info.channels;
                   if (bufferLenth > 0&&bufferLenth<=4096)
                   {
                       byte[] pcm_data = FunctionEx.IntPtrToBytes(pcm, 0, (info.samples * info.channels));
                       byte[] frame_mono = new byte[2048];
                       if (info.channels == 1)
                           return pcm_data;
                       else if (info.channels == 2)
                       {
                           //从双声道的数据中提取单通道  
                           for (int i = 0, j = 0; i < 4096 && j < 2048; i += 4, j += 2)
                           {
                               frame_mono[j] = pcm_data[i];
                               frame_mono[j + 1] = pcm_data[i + 1];
                           }
                           return frame_mono;
                       }

                   }
               }
               catch (Exception ex)
               {

               }
            }
            return new byte[0];
        }

        const string DLLFile = @"libfaad2.dll";
        [DllImport(DLLFile, CallingConvention = CallingConvention.Cdecl)]
        //NeAACDecHandle NEAACDECAPI NeAACDecOpen(void);
        private extern static IntPtr NeAACDecOpen();

        [DllImport(DLLFile, CallingConvention = CallingConvention.Cdecl)]
        //NeAACDecConfigurationPtr NEAACDECAPI NeAACDecGetCurrentConfiguration(NeAACDecHandle hDecoder);
        private extern static IntPtr NeAACDecGetCurrentConfiguration(IntPtr hDecoder);

        [DllImport(DLLFile, CallingConvention = CallingConvention.Cdecl)]
        //unsigned char NEAACDECAPI NeAACDecSetConfiguration(NeAACDecHandle hDecoder,  NeAACDecConfigurationPtr config);
        private extern static IntPtr NeAACDecSetConfiguration(IntPtr hDecoder, IntPtr config);

      

        //long NEAACDECAPI NeAACDecInit(NeAACDecHandle hDecoder,
        //                      unsigned char *buffer,
        //                      unsigned long buffer_size,
        //                      unsigned long *samplerate,
        //                      unsigned char *channels);
        [DllImport(DLLFile, CallingConvention = CallingConvention.Cdecl)]
        private extern static int NeAACDecInit(IntPtr hDecoder,
                             byte[] buffer,
                             uint buffer_size,
                             ref uint samplerate,
                              ref byte channels);


        [DllImport(DLLFile, CallingConvention = CallingConvention.Cdecl)]
        //char NEAACDECAPI NeAACDecInit2(NeAACDecHandle hDecoder,
        //                       unsigned char *pBuffer,
        //                       unsigned long SizeOfDecoderSpecificInfo,
        //                       unsigned long *samplerate,
        //                       unsigned char *channels);
        private extern static int NeAACDecInit2(IntPtr hDecoder,
                             byte[] buffer,
                             uint SizeOfDecoderSpecificInfo,
                             ref uint samplerate,
                             ref byte channels);

        [DllImport(DLLFile, CallingConvention = CallingConvention.Cdecl)]

        //void* NEAACDECAPI NeAACDecDecode(NeAACDecHandle hDecoder,
        //                         NeAACDecFrameInfo *hInfo,
        //                         unsigned char *buffer,
        //                         unsigned long buffer_size);
        private extern static IntPtr NeAACDecDecode(IntPtr hDecoder,
                                 ref NeAACDecFrameInfo hInfo,
                                 byte[] buffer,
                                 int buffer_size);

        [DllImport(DLLFile, CallingConvention = CallingConvention.Cdecl)]
        //void NEAACDECAPI NeAACDecClose(NeAACDecHandle hDecoder);
        private extern static void NeAACDecClose(IntPtr hDecoder);
 
        public void Dispose() {
            if (_handle != IntPtr.Zero)
            {
                NeAACDecClose(_handle);
                _handle = IntPtr.Zero;
            }
            
        }
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct NeAACDecFrameInfo {

            public int bytesconsumed;
            public int samples;
            public byte channels;
            public byte error;
            public int samplerate;

            /* SBR: 0: off, 1: on; upsample, 2: on; downsampled, 3: off; upsampled */
            public byte sbr;

            /* MPEG-4 ObjectType */
            public byte object_type;

            /* AAC header type; MP4 will be signalled as RAW also */
            public byte header_type;

            /* multichannel configuration */
            public byte num_front_channels;
            public byte num_side_channels;
            public byte num_back_channels;
            public byte num_lfe_channels;

            [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.I1, SizeConst = 64)]
            public byte[] channel_position;

            /* PS: 0: off, 1: on */
            public byte ps;
        }
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct NeAACDecConfiguration
        {
            public byte defObjectType;
            public byte defSampleRate;
            public byte outputFormat;
            public byte downMatrix;
            public byte useOldADTSFormat;
            public byte dontUpSampleImplicitSBR;
        }

//        typedef struct NeAACDecFrameInfo
//{
//    unsigned long bytesconsumed;
//    unsigned long samples;
//    unsigned char channels;
//    unsigned char error;
//    unsigned long samplerate;

//    /* SBR: 0: off, 1: on; upsample, 2: on; downsampled, 3: off; upsampled */
//    unsigned char sbr;

//    /* MPEG-4 ObjectType */
//    unsigned char object_type;

//    /* AAC header type; MP4 will be signalled as RAW also */
//    unsigned char header_type;

//    /* multichannel configuration */
//    unsigned char num_front_channels;
//    unsigned char num_side_channels;
//    unsigned char num_back_channels;
//    unsigned char num_lfe_channels;
//    unsigned char channel_position[64];

//    /* PS: 0: off, 1: on */
//    unsigned char ps;
//} NeAACDecFrameInfo;

    }

}
