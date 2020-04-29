using Common.Streams;
using System;
using System.Collections.Generic;
using System.IO;

namespace GB28181.WinTool.Mixer.Audio
{

    public class AAC_ADTS {

        public int Syncword;                         //12bit all bits must be 1 
        public byte MPEG_version;                       //1bit 0 for MPEG-4, 1 for MPEG-2 
        public byte Layer;                              // 2bit always 0 
        public byte Protection;                         // 1bit Absent 1 et to 1 if there is no CRC and 0 if there is CRC 
        public byte Profile;                            // 2bit the MPEG-4 Audio Object Type minus 1 
        public byte MPEG_4_Sampling_Frequency_Index;    // 4bit MPEG-4 Sampling Frequency Index (15 is forbidden) 
        public byte Private_Stream;                     // 1bit set to 0 when encoding, ignore when decoding 
        public byte MPEG_4_Channel_Configuration;       // 3bit MPEG-4 Channel Configuration (in the case of 0, the channel configuration is sent via an inband PCE) 
        public byte Originality;                        // 1bit set to 0 when encoding, ignore when decoding 
        public byte Home;                               // 1bit set to 0 when encoding, ignore when decoding 
        public byte Copyrighted_Stream;                 // 1bit set to 0 when encoding, ignore when decoding 
        public byte Copyrighted_Start;                  // 1bit set to 0 when encoding, ignore when decoding 
        public ushort Frame_Length;                     // 13bit this value must include 7 or 9 bytes of header length: FrameLength = (ProtectionAbsent == 1 ? 7 : 9) + size(AACFrame) 
        public ushort Buffer_Fullness;                  // 11bit buffer fullness 
        public byte Number_of_AAC_Frames;               // 2bit number of AAC frames (RDBs) in ADTS frame minus 1, for maximum compatibility always use 1 AAC frame per ADTS frame 
        public byte[] CRC;                              // 16bit CRC if protection absent is 0 
        public ushort AACDataLen {
            get {
                return (ushort)(Frame_Length - (Protection == 0 ? 9 : 7));
            }
        }
        public byte[] AACData { get; private set; }
        public byte[] ADTSData { get; private set; }
        public byte[] FrameData {
            get {
                var r = new byte[AACData.Length + ADTSData.Length];

                Array.Copy(ADTSData, 0, r, 0, ADTSData.Length);
                Array.Copy(AACData, 0, r, ADTSData.Length, AACData.Length);
                return r;

            }
        }
        public AAC_ADTS(byte[] bs)
            : this(new MemoryStream(bs)) {
        }

        public AAC_ADTS(Stream ms) {
            var br = new System.IO.BinaryReader(ms);
            var bs = br.ReadBytes(7);
            //SetHead(bs);
            init(bs);
            if (Protection == 0)//还有2byte校验
            {
                CRC = br.ReadBytes(2);
                ADTSData = new byte[9];
                Array.Copy(bs, ADTSData, bs.Length);
                ADTSData[7] = CRC[0];
                ADTSData[8] = CRC[1];
            } else {
                ADTSData = bs;
            }
            AACData = br.ReadBytes(AACDataLen);

        }

        public void init(byte[] bs) {

            BitStreamReader sr = new BitStreamReader(bs);

            Protection = (byte)(bs[1] & 0x01);
            if (Protection == 0) {//ADTS有9byte

            } else {//ADTS有7byte

            }

            /*
                0: AAC Main
                1: AAC LC (Low Complexity)
                2: AAC SSR (Scalable Sample Rate)
                3: AAC LTP (Long Term Prediction)
            */
            //编码深度
            Profile = (byte)((bs[2] & 0xC0) >> 6);

            /*MPEG_4_Sampling_Frequency_Index 对应值
               0x0          96000 
               0x1          88200 
               0x2          64000 
               0x3          48000 
               0x4          44100 
               0x5          32000 
               0x6          24000 
               0x7          22050 
               0x8          16000 
               0x9          2000 
               0xa          11025 
               0xb          8000 
               0xc          reserved 
               0xd          reserved 
               0xe          reserved 
               0xf          reserved 
            */
            //采样率
            MPEG_4_Sampling_Frequency_Index = (byte)((bs[2] & 0x3C) >> 2);


            //1: 1 channel: front-center
            //2: 2 channels: front-left, front-right 
            //3: 3 channels: front-center, front-left, front-right 
            //4: 4 channels: front-center, front-left, front-right, back-center 
            //5: 5 channels: front-center, front-left, front-right, back-left, back-right 
            //6: 6 channels: front-center, front-left, front-right, back-left, back-right, LFE-channel 
            //7: 8 channels: front-center, front-left, front-right, side-left, side-right, back-left, back-right, LFE-channel 

            //通道数
            MPEG_4_Channel_Configuration = (byte)(((bs[2] & 0x01) << 1) | ((bs[3] & 0xC0) >> 6));

            //ADTS帧长度=ADTS头+AAC数据
            Frame_Length = (ushort)(((bs[3] & 0x03) << 11) | ((bs[4] & 0xFF) << 3) | ((bs[5] & 0xE0) >> 5));
        }
        public int Frequency
        {
            get { var frequencys = new int[] { 96000, 88200, 64000, 48000, 44100, 32000, 24000, 22050, 16000, 2000, 11025 };

                return (frequencys[this.MPEG_4_Sampling_Frequency_Index]);
                   }
            }
        public void SetHead(byte[] bs) {
            var adts = bs;
            Syncword = ((adts[0] << 4) | (adts[1] >> 4));
            MPEG_version = (byte)((adts[1] >> 3) & 0x1);
            Layer = (byte)((adts[1] >> 1) & 0x3);
            Protection = (byte)((adts[1]) & 0x1);
            Profile = (byte)((adts[2] >> 6) & 0x3);
            MPEG_4_Sampling_Frequency_Index = (byte)((adts[2] >> 2) & 0xF);
            Private_Stream = (byte)((adts[2] >> 1) & 0x1);
            MPEG_4_Channel_Configuration = (byte)(((adts[2] & 0x1) << 2) | ((adts[3] >> 6) & 0x3));
            Originality = (byte)((adts[3] >> 5) & 0x1);
            Home = (byte)((adts[3] >> 4) & 0x1);
            Copyrighted_Stream = (byte)((adts[3] >> 3) & 0x1);
            Copyrighted_Start = (byte)((adts[3] >> 2) & 0x1);
            Frame_Length = (ushort)((((adts[3]) & 0x3) << 11) | (adts[4]) << 3 | ((adts[5] >> 5) & 0x7));
            Buffer_Fullness = (ushort)(((adts[5] & 0x1F) << 6) | ((adts[6] >> 2) & 0x3F));
            Number_of_AAC_Frames = (byte)(adts[6] & 0x3);
        }

        public byte[] GetHead() {
        
            var adts = new byte[7];
            adts[0] = (byte)(this.Syncword >> 4);
            adts[1] = (byte)(((this.Syncword & 0xF) << 4) | (this.MPEG_version << 3) | (this.Layer << 1) | (this.Protection));
            adts[2] = (byte)((this.Profile << 6) | (this.MPEG_4_Sampling_Frequency_Index << 2) | (this.Private_Stream << 1) | ((this.MPEG_4_Channel_Configuration >> 2) & 0x1));
            adts[3] = (byte)(((this.MPEG_4_Channel_Configuration & 0x3) << 6) | (this.Originality << 5) | (this.Home << 4) | (this.Copyrighted_Stream << 3) | (this.Copyrighted_Start << 2) | ((this.Frame_Length >> 11) & 0x3));
            adts[4] = (byte)((this.Frame_Length >> 3) & 0xff);
            adts[5] = (byte)(((this.Frame_Length & 0x7) << 5) | ((this.Buffer_Fullness >> 6) & 0x1f));
            adts[6] = (byte)(((this.Buffer_Fullness & 0x3F) << 2) | (this.Number_of_AAC_Frames));
            return adts;
        }

        public static Boolean Check(int frequency, int channel, byte[] bs) {
            try {
                var aac = new AAC_ADTS(bs);
                if (aac.AACDataLen != aac.AACData.Length)
                    return false;
                var frequencys = new int[] { 96000, 88200, 64000, 48000, 44100, 32000, 24000, 22050, 16000, 2000, 11025 };

                if (frequencys[aac.MPEG_4_Sampling_Frequency_Index] != frequency)
                    return false;

                if (aac.MPEG_4_Channel_Configuration != channel)
                    return false;
                return true;
            } catch {
                return false;
            }

        }

        public static AAC_ADTS[] GetMultiAAC(byte[] bs) {
            var ms = new MemoryStream(bs);
            List<AAC_ADTS> aacs = new List<AAC_ADTS>();
            while (ms.Position + 7 < ms.Length) {
                var aac = new AAC_ADTS(ms);
                aacs.Add(aac);
            }
            return aacs.ToArray();
        }
    }
}
