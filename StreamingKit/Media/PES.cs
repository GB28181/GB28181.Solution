using Common.Networks;
using Common.Streams;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace StreamingKit.Media.TS
{
    //http://www.360doc.com/content/13/0512/11/532901_284774233.shtml

    //http://blog.csdn.net/ccskyer/article/details/7899991


    //http://blog.csdn.net/feixiaku/article/details/39119453
    /*TS流，是基于packet的位流格式，每个packet是188个字节或者204个字节（一般是188字节，204字节格式是在188字节的packet后面加上16字节的CRC数据，其他格式相同），
    解析TS流，先解析每个packet ，然后从一个packet中，解析出PAT的PID，根据PID找到PAT包，
    然后从PAT包中解析出PMT的PID，根据PID找到PMT包，在从PMT包中解析出Video和Audio（也有的包括Teletext和EPG）的PID。然后根据PID找出相应的包。*/

    //http://blog.163.com/benben_168/blog/static/185277057201152125757560/


    public partial class PESPacket : IByteObj {

        public byte[] Packet_Start_Code_Prefix;     // 3 bytes
        public byte Stream_ID;					    // 1 byte
        public ushort PES_Packet_Length;			// 2 bytes

        public byte[] PES_Header_Flags;			    // 2 bytes = "10"(2 biti) + PES_H_F(14 biti)
        public byte PES_Header_Length;			    // 1 byte
        public byte[] PES_Header_Fields;			// Variable length

        public byte[] PES_Packet_Data;			    // Variable length
        private MediaFrame _MediaFrame = null;
        private bool _isVideo = false;

        public unsafe MediaFrame MediaFrame
        {
            get { return _MediaFrame; }
            set {
                try {
                    _MediaFrame = value;
                    if (_MediaFrame == null)
                        throw new Exception();

                } catch (Exception)
                {
                    throw;
                }
            }
        }

        public PESPacket() {
            Packet_Start_Code_Prefix = new byte[] { 0x00, 0x00, 0x01 };
            //以几项写死，可能有些情况不能写死
            PES_Header_Flags = new byte[2] { 0x81, 0xC0 };//PTS_DTS_flags 11 头扩展信息中包含 pts和dts
            PES_Header_Length = 10;           //信息区长度
            PES_Header_Fields = new byte[10];
        }

        public unsafe PESPacket(byte[] packetData, bool IsVideo, long timetick) {
            _isVideo = IsVideo;

            Packet_Start_Code_Prefix = new byte[] { 0x00, 0x00, 0x01 };
            //以几项写死，可能有些情况不能写死
            PES_Header_Flags = new byte[2] { 0x81, 0xC0 };//PTS_DTS_flags 11 头扩展信息中包含 pts和dts

            PES_Header_Length = 10;                         //信息区长度
            PES_Header_Fields = new byte[10];
            if (!IsVideo && false) {
                PES_Header_Flags = new byte[2] { 0x80, 0x80 };//PTS_DTS_flags 11 头扩展信息中包含 pts和dts
                PES_Header_Length = 5;
                PES_Header_Fields = new byte[5];
            }
            Stream_ID = (byte)(IsVideo ? 0xE0 : 0xC0);
            SetTimetick(PES_Header_Fields, timetick, timetick);
            PES_Packet_Data = packetData;
            int packet_len = (3 + PES_Header_Length + PES_Packet_Data.Length);
            PES_Packet_Length = (ushort)packet_len;
            if (packet_len > 65526) {
                //如果h264的包大于 65535  的话，可以设置PES_packet_length为0 ，具体参见ISO/ICE 13818-1.pdf  49 / 174 中关于PES_packet_length的描述
                //打包PES, 直接读取一帧h264的内容, 此时我们设置PES_packet_length的值为0000
                //表示不指定PES包的长度,ISO/ICE 13818-1.pdf 49 / 174 有说明,这主要是方便
                //当一帧H264的长度大于PES_packet_length(2个字节)能表示的最大长度65535
                //的时候分包的问题, 这里我们设置PES_packet_length的长度为0000之后 ,  那么即使该H264视频帧的长度
                //大于65535个字节也不需要分多个PES包存放, 事实证明这样做是可以的, ipad可播放
                PES_Packet_Length = 0;
            }
        }

        private unsafe byte[] GetHeadBytes() {
            byte[] head = new byte[3 + PES_Header_Length];
            head[0] = PES_Header_Flags[0];//0x81
            head[1] = PES_Header_Flags[1];//PTS_DTS_flags 11 头扩展信息中包含 pts和dts
            head[2] = PES_Header_Length;           //信息区长度
            Array.Copy(PES_Header_Fields, 0, head, 3, PES_Header_Length);
            return head;
        }

        private unsafe void SetTimetick(byte[] buf, long pts, long dts) {
            buf[0] = (byte)(((pts >> 29) | 0x31) & 0x3f);
            buf[1] = (byte)(pts >> 22);
            buf[2] = (byte)((pts >> 14) | 0x01);
            buf[3] = (byte)(pts >> 7);
            buf[4] = (byte)((pts << 1) | 0x01);
            if (PES_Header_Length > 5) {
                buf[5] = (byte)(((dts >> 29) | 0x11) & 0x1f);
                buf[6] = (byte)(dts >> 22);
                buf[7] = (byte)((dts >> 14) | 0x01);
                buf[8] = (byte)(dts >> 7);
                buf[9] = (byte)((dts << 1) | 0x01);
            }
        }

        private static unsafe long ParsePTS(byte* pBuf) {
            //该算法在HIK流下算不准,改为PS流包头的SCR来获取
            long llpts = (((long)(pBuf[0] & 0x0E)) << 29)
               | (long)(pBuf[1] << 22)
               | (((long)(pBuf[2] & 0xFE)) << 14)
               | (long)(pBuf[3] << 7)
               | (long)(pBuf[4] >> 1);
            return llpts;
        }

        public unsafe long GetAudioTimetick() {
            if (PES_Header_Fields == null || PES_Header_Fields.Length == 0)
                return 0;
            fixed (byte* pbuf = PES_Header_Fields) {
                return ParsePTS(pbuf) / 90;
            }
        }

        public unsafe long GetVideoTimetick() {
            if (PES_Header_Fields == null || PES_Header_Fields.Length == 0)
                return 0;
            fixed (byte* pbuf = PES_Header_Fields) {
                return ParsePTS(pbuf) / 90;
            }
        }

        private byte[] GetBodyBytes() {
            if (MediaFrame != null) {
                return MediaFrame.Data;
            } else if (PES_Packet_Data != null)
                return PES_Packet_Data;
            else
                return null;

        }
 
        public byte[] GetBytes() {
            var head = GetHeadBytes();
            var body = GetBodyBytes();
            var ms = new System.IO.MemoryStream();
            var bw = new System.IO.BinaryWriter(ms);
            bw.Write(Packet_Start_Code_Prefix);
            bw.Write(Stream_ID);
            bw.Write(IPAddress.HostToNetworkOrder((short)PES_Packet_Length));
            bw.Write(head);
            bw.Write(body);
            return ms.ToArray();
        }

        public void SetBytes(Stream stream) {
            var br = new System.IO.BinaryReader(stream);
            Packet_Start_Code_Prefix = br.ReadBytes(3);
            if (Packet_Start_Code_Prefix[0] != 0 || Packet_Start_Code_Prefix[1] != 0 || Packet_Start_Code_Prefix[2] != 1)
                throw new Exception();
            Stream_ID = br.ReadByte();
            PES_Packet_Length = (ushort)IPAddress.NetworkToHostOrder(br.ReadInt16());
            if (PES_Packet_Length == 0&&false) {
                throw new Exception("PES_Packet_Length error");
            }
            PES_Header_Flags = br.ReadBytes(2);
            PES_Header_Length = br.ReadByte();
            PES_Header_Fields = br.ReadBytes(PES_Header_Length);
            if (PES_Packet_Length > 0) {
                PES_Packet_Data = br.ReadBytes(PES_Packet_Length - 3 - PES_Header_Length);
            } else {
                PES_Packet_Data = br.ReadBytes((int)(stream.Length - stream.Position));
            }
        }

        public void SetBytes(byte[] buf) {
            var ms = new System.IO.MemoryStream(buf);
            SetBytes(ms);
        }

        private static long nTimetick = 0;

        public static PESPacket MediaFrame2PES(MediaFrame frame) {
            if (nTimetick == 0)
                nTimetick = frame.nTimetick;
            try {
                List<PESPacket> packs = new List<PESPacket>();
                var ms = new MemoryStream(frame.Data)
                {
                    Position = 0
                };
                var tick = (frame.nTimetick - nTimetick) * 90;
                while (ms.Position < ms.Length) {
                    var size = frame.nSize;
                    byte[] buf = null;
                    if (frame.nIsAudio == 0) {
                        buf = new byte[size + 6];
                        buf[0] = 0x00;
                        buf[1] = 0x00;
                        buf[2] = 0x00;
                        buf[3] = 0x01;
                        buf[4] = 0x09;
                        buf[5] = 0xF0;
                        Array.Copy(frame.Data, 0, buf, 6, size);
                    } else {
                        buf = frame.Data;
                    }
                    var pack = new PESPacket(buf, frame.nIsAudio == 0, tick);
                    return pack;
                }
                return null;
            }
            catch (Exception)
            {
                throw;
            }
        }
    }

    public class PSPacketHeader : IByteObj
    {
        public int start_code;                                  //32bit  0x000000BA
        public byte marker1;                                    //2bit
        public byte system_clock1;                              //3bit
        public byte marker2;                                    //1bit
        public short system_clock2;                             //15bit
        public byte marker3;                                    //1bit
        public short system_clock3;                             //15bit
        public byte marker4;                                    //1bit
        public short SCR_externsion;                            //9bit
        public byte marker5;                                    //1bit
        public int mutiplex_rate;                               //22bit
        public byte marker6;                                    //2bit
        public byte reserved;                                   //5bit
        public byte stuffing_length;                            //3bit
        public byte[] stuffing;

        public PSPacketHeader(Stream stream)
        {
            byte[] buffer = new byte[14];
            stream.Read(buffer, 0, buffer.Length);
            BitStream bs = new BitStream(buffer)
            {
                Position = 0
            };
            bs.Read(out start_code, 0, 32);
            bs.Read(out marker1, 0, 2);
            bs.Read(out system_clock1, 0, 3);
            bs.Read(out marker2, 0, 1);
            bs.Read(out system_clock2, 0, 15);
            bs.Read(out marker3, 0, 1);
            bs.Read(out system_clock3, 0, 15);
            bs.Read(out marker4, 0, 1);
            bs.Read(out SCR_externsion, 0, 9);
            bs.Read(out marker5, 0, 1);
            bs.Read(out mutiplex_rate, 0, 22);
            bs.Read(out marker6, 0, 2);
            bs.Read(out reserved, 0, 5);
            bs.Read(out stuffing_length, 0, 3);
            stuffing = new byte[stuffing_length];
            stream.Read(stuffing, 0, stuffing_length);
        }


        public byte[] GetBytes()
        {
            throw new NotImplementedException();
        }

        public void SetBytes(byte[] buf)
        {
            throw new NotImplementedException();
        }

        public long GetSCR()
        {

            BitStream bb = new BitStream();
            bb.Write(0, 0, 64 - 33);
            bb.Write(system_clock1, 0, 3);
            bb.Write(system_clock2, 0, 15);
            bb.Write(system_clock3, 0, 15);
            bb.Position = 32;
            long scr = 0;
            bb.Read(out scr, 0, 32);
            bb.Close();
            scr = scr / 90;
            return scr;
        }
    }
}




