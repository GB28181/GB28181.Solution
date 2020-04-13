using StreamingKit.Interface;
using Common.Streams;
using System;
using System.IO;

namespace StreamingKit.Media.TS
{

    public class PSMap : IBufferBytes
    {
        public int start_code;                                 //32bit
        public ushort length;
        public byte[] body;
        public PSMap(Stream stream)
        {
            byte[] buffer = new byte[6];
            stream.Read(buffer, 0, buffer.Length);
            BitStream bs = new BitStream(buffer)
            {
                Position = 0
            };
            bs.Read(out start_code, 0, 32);
            bs.Read(out length, 0, 16);
            body = new byte[length];
            stream.Read(body, 0, length);
        }

        public byte[] GetBytes()
        {
            throw new NotImplementedException();
        }

        public void SetBytes(byte[] buf)
        {
            throw new NotImplementedException();
        }
    }

    public class PSSystemHeader : IBufferBytes
    {
        public int start_code;                                 //32bit
        public ushort header_length;
        public byte[] body;
        public PSSystemHeader(Stream stream)
        {
            byte[] buffer = new byte[6];
            stream.Read(buffer, 0, buffer.Length);
            var bs = new BitStream(buffer)
            {
                Position = 0
            };
            bs.Read(out start_code, 0, 32);
            bs.Read(out header_length, 0, 16);
            body = new byte[header_length];
            stream.Read(body, 0, header_length);
        }
        public byte[] GetBytes()
        {
            throw new NotImplementedException();
        }
        public void SetBytes(byte[] buf)
        {
            throw new NotImplementedException();
        }
    }

    public class PSPacketHeader : IBufferBytes
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
            var bs = new BitStream(buffer)
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
            bb.Read(out long scr, 0, 32);
            bb.Close();
            scr /= 90;
            return scr;
        }
    }


}




