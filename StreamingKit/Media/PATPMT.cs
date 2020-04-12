using Common.Streams;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace StreamingKit.Media.TS
{

    #region PAT

    public class TS_PAT {
        public byte table_id;                           //: 8;固定为0x00 ，标志是该表是PAT表  
        public byte section_syntax_indicator;           //: 1; //段语法标志位，固定为1  
        public byte zero;                               //: 1; //0  
        public byte reserved_1;                         //: 2; // 保留位  
        public short section_length;                    //: 12; //表示从下一个字段开始到CRC32(含)之间有用的字节数  
        public short transport_stream_id;               // : 16; //该传输流的ID，区别于一个网络中其它多路复用的流  
        public byte reserved_2;                         //: 2;// 保留位  
        public byte version_number;                     //: 5; //范围0-31，表示PAT的版本号  
        public byte current_next_indicator;             //: 1; //发送的PAT是当前有效还是下一个PAT有效  
        public byte section_number;                     //: 8; //分段的号码。PAT可能分为多段传输，第一段为00，以后每个分段加1，最多可能有256个分段  
        public byte last_section_number;                //: 8;  //最后一个分段的号码  
        public List<TS_PAT_Program> PATProgramList = new List<TS_PAT_Program>();

        //以下三个字段与TS_PAT_Program结构对应，当program_num=0x00的时候为节目号未尾或表示没有节目号，当有节目号时使用TS_PAT_Program结构表示
        public byte program_num;                         //16; //节目号
        public byte reserved_3;                         //: 3; // 保留位  
        public short network_PID;                       //: 13; /网络信息表（NIT）的PID,节目号为0时对应的PID为network_PID  

        public Int32 CRC_32;                            //: 32;  //CRC32校验码  /

        public byte[] GetBytes() {
            //这里写死，因为暂时所使用的PMT只有一个，如果有多个或复杂应用的时候需要根据当前结构生成结果

            //以下表示PMT有一个，PMT_PID为256
            return new byte[] { 0x00, 0xB0, 0x0D, 0x00, 0x01, 0xC1, 0x00, 0x00, 0x00, 0x01, 0xE1, 0x00, 0xE8, 0xF9, 0x5E, 0x7D };


            //var ms = new MemoryStream();
            //BitStream bs = new BitStream(ms);
            //bs.Write(table_id, 0, 8);
            //bs.Write(section_syntax_indicator, 0, 1);
            //bs.Write(zero, 0, 1);
            //bs.Write(reserved_1, 0, 2);
            //bs.Write(section_length, 0, 12);
            //bs.Write(transport_stream_id, 0, 16);
            //bs.Write(reserved_2, 0, 2);
            //bs.Write(version_number, 0, 5);
            //bs.Write(current_next_indicator, 0, 1);
            //bs.Write(section_number, 0, 8);
            //bs.Write(last_section_number, 0, 8);


            //bs.Write(version_number, 0, 5);
            //bs.Write(version_number, 0, 5);
        }

        public void SetBytes(byte[] buffer) {
            buffer=buffer.Skip(1).ToArray();
            BitStream bs = new BitStream(buffer);
            bs.Position = 0;
            bs.Read(out table_id, 0, 8);                           //: 8;固定为0x00 ，标志是该表是PAT表  
            bs.Read(out section_syntax_indicator, 0, 1);           //: 1; //段语法标志位，固定为1  
            bs.Read(out zero, 0, 1);                               //: 1; //0  
            bs.Read(out reserved_1, 0, 2);                        //: 2; // 保留位  
            bs.Read(out section_length, 0, 12);                    //: 12; //表示从下一个字段开始到CRC32(含)之间有用的字节数  

            bs.Read(out transport_stream_id, 0, 16);               // : 16; //该传输流的ID，区别于一个网络中其它多路复用的流  
            bs.Read(out reserved_2, 0, 2);                        //: 2;// 保留位  
            bs.Read(out  version_number, 0, 5);                     //: 5; //范围0-31，表示PAT的版本号  
            bs.Read(out current_next_indicator, 0, 1);             //: 1; //发送的PAT是当前有效还是下一个PAT有效  
            bs.Read(out  section_number, 0, 8);                     //: 8; //分段的号码。PAT可能分为多段传输，第一段为00，以后每个分段加1，最多可能有256个分段  
            bs.Read(out  last_section_number, 0, 8);                //: 8;  //最后一个分段的号码  

            List<TS_PAT_Program> programList = new List<TS_PAT_Program>();
            int n = 0;
            for (n = 0; n < section_length - 12; n += 4) {
                program_num = (byte)(buffer[8 + n] << 8 | buffer[9 + n]);
                reserved_3 = (byte)(buffer[10 + n] >> 5); //3 保留位  
                network_PID = 0x00;
                if (program_num == 0x00) {
                    network_PID = (short)((buffer[10 + n] & 0x1F) << 8 | buffer[11 + n]);//: 13 网络信息表（NIT）的PID,节目号为0时对应的PID为network_PID  
                    var TS_network_Pid = network_PID; //记录该TS流的网络PID  
                } else {
                    TS_PAT_Program PAT_program = new TS_PAT_Program();
                    PAT_program.program_map_PID = (short)((buffer[10 + n] & 0x1F) << 8 | buffer[11 + n]);
                    PAT_program.program_number = program_num;
                    programList.Add(PAT_program);

                    //TS_program.push_back(PAT_program);//向全局PAT节目数组中添加PAT节目信息       
                }
            }
            PATProgramList = programList;

            var len = 3 + section_length;
            CRC_32 = (buffer[len - 4] & 0x000000FF) << 24
                      | (buffer[len - 3] & 0x000000FF) << 16
                      | (buffer[len - 2] & 0x000000FF) << 8
                      | (buffer[len - 1] & 0x000000FF);

        }

        public override String ToString() {
            StringBuilder sb = new StringBuilder();



            sb.AppendFormat("table_id:{0}\r\n", table_id);
            sb.AppendFormat("section_syntax_indicator:{0}\r\n", section_syntax_indicator);
            sb.AppendFormat("zero:{0}\r\n", zero);
            sb.AppendFormat("reserved_1:{0}\r\n", reserved_1);
            sb.AppendFormat("section_length:{0}\r\n", section_length);
            sb.AppendFormat("transport_stream_id:{0}\r\n", transport_stream_id);
            sb.AppendFormat("reserved_2:{0}\r\n", reserved_2);

            sb.AppendFormat("version_number:{0}\r\n", version_number);
            sb.AppendFormat("current_next_indicator:{0}\r\n", current_next_indicator);
            sb.AppendFormat("section_number:{0}\r\n", section_number);
            sb.AppendFormat("last_section_number:{0}\r\n", last_section_number);
            sb.AppendFormat("reserved_3:{0}\r\n", reserved_3);
            sb.AppendFormat("network_PID:{0}\r\n", network_PID);


            sb.AppendFormat("CRC_32:{0}\r\n", CRC_32);
            sb.AppendFormat("PATProgramList:{0}\r\n", PATProgramList.Count);
            foreach (var item in PATProgramList) {
                sb.AppendFormat("   program_map_PID:{0}\r\n", item.program_map_PID);
                sb.AppendFormat("   program_number:{0}\r\n", item.program_number);

            }



            return sb.ToString();

        }

        public struct TS_PAT_Program {
            public short program_number;//:16; //节目号
            public byte reserved_3;
            public short program_map_PID;//:13;   //节目映射表的PID，节目号大于0时对应的PID，每个节目对应一个
        }

    }

    #endregion

    #region PMT

    public class TS_PMT {
        public byte table_id;//                       : 8; //固定为0x02, 表示PMT表  
        public byte section_syntax_indicator;//        : 1; //固定为0x01  
        public byte zero;//                           : 1; //0x01  
        public byte reserved_1;//                     : 2; //0x03  
        public ushort section_length;//                  : 12;//首先两位bit置为00，它指示段的byte数，由段长度域开始，包含CRC。  
        public ushort program_number;//                   : 16;// 指出该节目对应于可应用的Program map PID  
        public byte reserved_2;//                      : 2; //0x03  
        public byte version_number;//                    : 5; //指出TS流中Program map section的版本号  
        public byte current_next_indicator;//           : 1; //当该位置1时，当前传送的Program map section可用；  
        //当该位置0时，指示当前传送的Program map section不可用，下一个TS流的Program map section有效。  
        public byte section_number;//                 : 8; //固定为0x00  
        public byte last_section_number;//          : 8; //固定为0x00  

        public byte reserved_3;//                   : 3; //0x07  
        public ushort PCR_PID;//                   : 13; //指明TS包的PID值，该TS包含有PCR域，  
        //该PCR值对应于由节目号指定的对应节目。  
        //如果对于私有数据流的节目定义与PCR无关，这个域的值将为0x1FFF。  
        public byte reserved_4;//                     : 4; //预留为0x0F  
        public ushort program_info_length;//          : 12; //前两位bit为00。该域指出跟随其后对节目信息的描述的byte数。  

        public List<TS_PMT_Stream> PMTStreamList = new List<TS_PMT_Stream>();  //每个元素包含8位, 指示特定PID的节目元素包的类型。该处PID由elementary PID指定  
        public byte reserved_5;//                   : 3; //0x07  
        public byte reserved_6;//                     : 4; //0x0F  
        public int CRC_32;//                    : 32;   


        public byte[] GetBytes() {
            //这里写死，对应PMT_PID为256  因为暂时所使用频道只有一个，即只有一个音频流及视频流组成一个频道

            //以下表示频道只有一个，PMT_PID为256  
            //音频流AAC PID为257   ES流描述长度为0
            //视频流H264 PID为258  ES流描述长度为0
            //return new byte[] { 0x02, 0xB0, 0x12, 0x00, 0x01, 0xC1, 0x00, 0x00, 0xE1, 0x02, 0xF0, 0x00, 0x1B, 0xE1, 0x02, 0xF0, 0x00,   0x4D, 0x8E, 0xB1, 0xB2 };

            //视频
            //return new byte[] { 0x02, 0xB0, 0x12, 0x00, 0x01, 0xC1, 0x00, 0x00, 0xE1, 0x02, 0xF0, 0x00, 0x1B, 0xE1, 0x02, 0xF0, 0x00, 0xA1, 0x4F, 0xAD, 0xCC };

            //音视频
           return new byte[] { 0x02, 0xB0, 0x17, 0x00, 0x01, 0xC1, 0x00, 0x00, 0xE1, 0x02, 0xF0, 0x00, 0x1B, 0xE1, 0x02, 0xF0, 0x00, 0x0F, 0xE1, 0x01, 0xF0, 0x00, 0x4D, 0x8E, 0xB1, 0xB2 };



        }
        /// <summary>
        /// 监控只获取视频
        /// </summary>
        /// <returns></returns>
        public byte[] GetVideoBytes()
        {
            return new byte[] { 0x02, 0xB0, 0x12, 0x00, 0x01, 0xC1, 0x00, 0x00, 0xE1, 0x02, 0xF0, 0x00, 0x1B, 0xE1, 0x02, 0xF0, 0x00, 0xA1, 0x4F, 0xAD, 0xCC };
        }

        public void SetBytes(byte[] buffer) {
            buffer = buffer.Skip(1).ToArray();
            table_id = buffer[0];
            section_syntax_indicator = (byte)(buffer[1] >> 7);
            zero = (byte)(buffer[1] >> 6 & 0x01);
            reserved_1 = (byte)(buffer[1] >> 4 & 0x03);
            section_length = (ushort)((buffer[1] & 0x0F) << 8 | buffer[2]);
            program_number = (ushort)(buffer[3] << 8 | buffer[4]);
            reserved_2 = (byte)(buffer[5] >> 6);
            version_number = (byte)(buffer[5] >> 1 & 0x1F);
            current_next_indicator = (byte)((buffer[5]) >> 7);
            section_number = buffer[6];
            last_section_number = buffer[7];
            reserved_3 = (byte)(buffer[8] >> 5);
            PCR_PID = (ushort)(((buffer[8] << 8) | buffer[9]) & 0x1FFF);

            var PCRID = PCR_PID;

            reserved_4 = (byte)(buffer[10] >> 4);
            program_info_length = (ushort)((buffer[10] & 0x0F) << 8 | buffer[11]);
            // Get CRC_32  
            int len_crc = 0;
            len_crc = section_length + 3;
            CRC_32 = (buffer[len_crc - 4] & 0x000000FF) << 24
                      | (buffer[len_crc - 3] & 0x000000FF) << 16
                      | (buffer[len_crc - 2] & 0x000000FF) << 8
                      | (buffer[len_crc - 1] & 0x000000FF);

            int pos = 12;
            // program info descriptor  
            if (program_info_length != 0)
                pos += program_info_length;

            List<TS_PMT_Stream> list = new List<TS_PMT_Stream>();

            // Get stream type and PID      
            for (; pos <= (section_length + 2) - 4; ) {
                TS_PMT_Stream pmt_stream = new TS_PMT_Stream();
                pmt_stream.stream_type = buffer[pos];
                reserved_5 = (byte)(buffer[pos + 1] >> 5);
                pmt_stream.elementary_PID = (ushort)(((buffer[pos + 1] << 8) | buffer[pos + 2]) & 0x1FFF);
                reserved_6 = (byte)(buffer[pos + 3] >> 4);
                pmt_stream.ES_info_length = (ushort)((buffer[pos + 3] & 0x0F) << 8 | buffer[pos + 4]);

                pmt_stream.descriptor = 0x00;
                if (pmt_stream.ES_info_length != 0) {
                    pmt_stream.descriptor = buffer[pos + 5];

                    for (int len = 2; len <= pmt_stream.ES_info_length; len++) {
                        pmt_stream.descriptor = (ushort)(pmt_stream.descriptor << 8 | buffer[pos + 4 + len]);
                    }
                    pos += pmt_stream.ES_info_length;
                }
                pos += 5;
                list.Add(pmt_stream);
            }
            PMTStreamList = list;
        }

        public override String ToString() {
            StringBuilder sb = new StringBuilder();

            sb.AppendFormat("table_id:{0}\r\n", table_id);
            sb.AppendFormat("section_syntax_indicator:{0}\r\n", section_syntax_indicator);
            sb.AppendFormat("zero:{0}\r\n", zero);
            sb.AppendFormat("reserved_1:{0}\r\n", reserved_1);
            sb.AppendFormat("section_length:{0}\r\n", section_length);
            sb.AppendFormat("program_number:{0}\r\n", program_number);
            sb.AppendFormat("reserved_2:{0}\r\n", reserved_2);

            sb.AppendFormat("version_number:{0}\r\n", version_number);
            sb.AppendFormat("current_next_indicator:{0}\r\n", current_next_indicator);
            sb.AppendFormat("section_number:{0}\r\n", section_number);
            sb.AppendFormat("last_section_number:{0}\r\n", last_section_number);
            sb.AppendFormat("reserved_3:{0}\r\n", reserved_3);
            sb.AppendFormat("PCR_PID:{0}\r\n", PCR_PID);

            sb.AppendFormat("reserved_4:{0}\r\n", reserved_4);
            sb.AppendFormat("program_info_length:{0}\r\n", program_info_length);
            sb.AppendFormat("reserved_5:{0}\r\n", reserved_5);
            sb.AppendFormat("CRC_32:{0}\r\n", CRC_32);
            sb.AppendFormat("PMTStreamList:{0}\r\n", PMTStreamList.Count);
            foreach (var item in PMTStreamList) {
                sb.AppendFormat("   stream_type:{0}  //27=h264 15=aac \r\n", item.stream_type);
                sb.AppendFormat("   elementary_PID:{0}\r\n", item.elementary_PID);
                sb.AppendFormat("   descriptor:{0}\r\n", item.descriptor);
            }

            sb.AppendFormat("reserved_4:{0}\r\n", reserved_4);

            return sb.ToString();

        }

        public struct TS_PMT_Stream {
            public byte stream_type;//                      : 8; //指示特定PID的节目元素包的类型。该处PID由elementary PID指定  
            public ushort elementary_PID;//                   : 13; //该域指示TS包的PID值。这些TS包含有相关的节目元素  
            public ushort ES_info_length;//                    : 12; //前两位bit为00。该域指示跟随其后的描述相关节目元素的byte数  
            public ushort descriptor;// 8
        }
    }

    #endregion

}




