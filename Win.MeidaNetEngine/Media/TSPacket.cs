using GLib.IO;
using Win.Media;
using System;
using System.IO;
using System.Text;


namespace Win.MediaServer.Media.TS
{
    //http://www.360doc.com/content/13/0512/11/532901_284774233.shtml

    //http://blog.csdn.net/ccskyer/article/details/7899991


    //http://blog.csdn.net/feixiaku/article/details/39119453
    /*TS流，是基于packet的位流格式，每个packet是188个字节或者204个字节（一般是188字节，204字节格式是在188字节的packet后面加上16字节的CRC数据，其他格式相同），
    解析TS流，先解析每个packet ，然后从一个packet中，解析出PAT的PID，根据PID找到PAT包，
    然后从PAT包中解析出PMT的PID，根据PID找到PMT包，在从PMT包中解析出Video和Audio（也有的包括Teletext和EPG）的PID。然后根据PID找出相应的包。*/

    //http://blog.163.com/benben_168/blog/static/185277057201152125757560/

    public partial class TSPacket {
        private TS_PAT _pat = null;
        private TS_PMT _pmt = null;
        public TSProgramManage ProgramManage { get; set; }
        public byte sync_byte;                                          //8bits 同步字节
        public byte transport_error_indicator;                          //1bit 错误指示信息（1：该包至少有1bits传输错误）
        public byte payload_unit_start_indicator;                       //1bit 负载单元开始标志（packet不满188字节时需填充）
        public byte transport_priority;                                 //1bit 传输优先级标志（1：优先级高）
        public ushort PID;                                              //13bits Packet ID号码，唯一的号码对应不同的包
        public byte transport_scrambling_control;                       //2bits 加密标志（00：未加密；其他表示已加密）
        public byte adaptation_field_control;                           //2bits 附加区域控制
        public byte continuity_counter;                                 //4bits 包递增计数器
        public AdaptationInfo AdaptationField;
        public byte[] data;
        public byte[] SrcBuffer;
 
        //只有调用Decode后才能解析出来
        public TSPacketType PacketType { get; private set; }
 
        public static byte[] MediaFrame2TSData(MediaFrame frame,TSProgramManage pm) {
            var msOutput = new MemoryStream();
            var pes = PESPacket.MediaFrame2PES(frame);
            var pes_buffer = pes.GetBytes();
            var msInput = new MemoryStream(pes_buffer);
            int PID = frame.nIsAudio == 1 ? 257 : 258;
            bool isFirstPack = true;
            do {
                var payload_unit_start_indicator = 0;
                var size = (int)(msInput.Length - msInput.Position);
                var max_data_len = 188 - 4;//4B为包头长度
                var data_len = 0;
                AdaptationInfo ai = null;
                if ((isFirstPack || size < max_data_len)) {
                    if (isFirstPack && (frame.nIsAudio == 0 || true)) {
                        max_data_len = 188 - 4 - 8;//5B为包头长度 8B为adaptation长度
                        data_len = Math.Min(max_data_len, size);
                        var adaptation_field_length = 188 - 4 - data_len - 1;//1B为adaptation的头，这1B不算在adaptation_field_length中
                        var pcr_bas = (frame.nTimetick - pm.FirstFrameTimeTick) * 90;//这里为什么是*45我也不知道，
                        ai = new AdaptationInfo() {
                            adaptation_field_length = (byte)adaptation_field_length,
                            random_access_indicator = 1,
                            PCR_flag = 1,
                            PCR_base = pcr_bas,
                        };
                        payload_unit_start_indicator = 1;
                    } else {
                        max_data_len = 188 - 4 - 1;//4B为包头长度 8B为adaptation长度
                        if (size < max_data_len) {
                            data_len = Math.Min(max_data_len, size);
                            var adaptation_field_length = 188 - 4 - data_len - 1;
                            ai = new AdaptationInfo();
                            ai.adaptation_field_length = (byte)adaptation_field_length;
                        } else {
                            payload_unit_start_indicator = 0;
                            data_len = size;
                        }
                    }
                } else {
                    data_len = max_data_len;
                }
                byte[] data = new byte[data_len];
                msInput.Read(data, 0, data.Length);
                TSPacket pack_pat = new TSPacket() {
                    sync_byte = 0x47,
                    transport_error_indicator = 0,
                    payload_unit_start_indicator = (byte)payload_unit_start_indicator,//一帧第一个packet或者这个packet需要fill字节时
                    transport_priority = 0,
                    PID = (ushort)PID,
                    transport_scrambling_control = 0,
                    adaptation_field_control = (byte)(ai != null ? 3 : 1),
                    continuity_counter = pm.GetCounter(PID),
                    AdaptationField = ai,
                    data = data
                };
                var buf = pack_pat.GetBytes();
                msOutput.Write(buf, 0, buf.Length);
                isFirstPack = false;
            } while (msInput.Position < msInput.Length);
            var result = msOutput.ToArray();
            return result;
        }

        public byte[] GetBytes() {
            byte[] result = new byte[188];
            var ms = new MemoryStream(result);
            ms.Position = 0;
            byte[] head = new byte[4];
            head[0] = 0x47;
            head[1] = (byte)((transport_error_indicator << 7) | (payload_unit_start_indicator << 6) | (transport_priority << 5) | (PID >> 8 & 0x001f));
            head[2] = (byte)(PID & 0x00ff);
            head[3] = (byte)((transport_scrambling_control << 6) | (adaptation_field_control << 4) | (continuity_counter & 0x0F));
            ms.Write(head, 0, head.Length);
            if (payload_unit_start_indicator == 1 && adaptation_field_control != 3)
                ms.WriteByte(0x00);//1个字节的占位

            if (AdaptationField != null) {
                var adapBuffer = AdaptationField.GetBytes();
                ms.Write(adapBuffer, 0, adapBuffer.Length);
            }
            if (data != null) {
                ms.Write(data, 0, data.Length);
            }
            return result;
        }

        public void SetBytes(byte[] buffer) {
            SrcBuffer = buffer;
            sync_byte = buffer[0];
            transport_error_indicator = (byte)(buffer[1] >> 7);
            payload_unit_start_indicator = (byte)(buffer[1] >> 6 & 0x01);
            transport_priority = (byte)(buffer[1] >> 5 & 0x01);
            PID = (ushort)(((buffer[1] & 0x1f) << 8) | buffer[2]);
            transport_scrambling_control = (byte)((buffer[3] >> 6) & 0x3);
            adaptation_field_control = (byte)((buffer[3] >> 4) & 0x3);
            continuity_counter = (byte)(buffer[3] & 0x0F);
        }
        public bool TryDecode() {
            int skip = 4;
            switch (adaptation_field_control) {
                case 0x0:                                    // reserved for future use by ISO/IEC
                    return false;
                case 0x1:                                    // 无调整字段，仅含有效负载       
                    skip = 4 + payload_unit_start_indicator;
                    break;
                case 0x2:                                     // 仅含调整字段，无有效负载
                    skip = SrcBuffer.Length;
                    break;
                case 0x3: // 调整字段后含有效负载
                    AdaptationField = new AdaptationInfo();
                    AdaptationField.SetBytes(SrcBuffer);
                    skip = 4 + AdaptationField.adaptation_field_length + 1;
                    break;
                default:
                    break;
            }
            return true;
        }
        public void Decode() {
            //以下是整个程序的逻辑主要是以解析为主，实际应用可能要优化下方式以提高性能
            var buffer = SrcBuffer;
            int skip = 4;
            switch (adaptation_field_control) {
                case 0x0:                                    // reserved for future use by ISO/IEC
                    throw new Exception();
                case 0x1:                                    // 无调整字段，仅含有效负载       
                    skip = 4 + payload_unit_start_indicator;
                    skip = 4;//
                    break;
                case 0x2:                                     // 仅含调整字段，无有效负载
                    skip = SrcBuffer.Length;
                    break;
                case 0x3: // 调整字段后含有效负载
                    AdaptationField = new AdaptationInfo();
                    AdaptationField.SetBytes(SrcBuffer);
                    skip = 4 + AdaptationField.adaptation_field_length + 1;
                    break;
                default:
                    break;
            }

            data = new byte[SrcBuffer.Length - skip];
            if (data.Length > 0) {
                Array.Copy(SrcBuffer, skip, data, 0, data.Length);
            }
            if (PID == 0x00) {
                PacketType = TSPacketType.PAT;
                var pat = new TS_PAT();
                pat.SetBytes(data);
                foreach (var p in pat.PATProgramList)
                    ProgramManage.AddProgram(p);
                _pat = pat;
            } else {
                if (ProgramManage.IsPMT_PID(PID)) {
                    //PMT
                    PacketType = TSPacketType.PMT;
                    var pmt = new TS_PMT();
                    pmt.SetBytes(data);
                    foreach (var p in pmt.PMTStreamList)
                        ProgramManage.AddStream(p);
                    _pmt = pmt;
                } else if (ProgramManage.IsTSTable(PID)) {
                    PacketType = TSPacketType.OTHER;
                    //其他表示，这里先不作处理

                } else if (ProgramManage.IsData(PID)) {
                    //这里一般为 媒体数据
                    PacketType = TSPacketType.DATA;
                }
            }
        }
 
        public override String ToString() {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("sync_byte:{0}\r\n", sync_byte);
            sb.AppendFormat("transport_error_indicator:{0}\r\n", transport_error_indicator);
            sb.AppendFormat("payload_unit_start_indicator:{0}\r\n", payload_unit_start_indicator);
            sb.AppendFormat("transport_priority:{0}\r\n", transport_priority);
            sb.AppendFormat("PID:{0}\r\n", PID);
            sb.AppendFormat("transport_scrambling_control:{0}\r\n", transport_scrambling_control);
            sb.AppendFormat("adaptation_field_control:{0}\r\n", adaptation_field_control);
            sb.AppendFormat("continuity_counter:{0}\r\n", continuity_counter);
            if (_pat != null)
                sb.AppendFormat("PAT:\r\n{0}", _pat.ToString());
            if (_pmt != null)
                sb.AppendFormat("PMT:\r\n{0}", _pmt.ToString());
            return sb.ToString();
        }
 
        private static byte[] _pmt_pat_tsdata = null;

        public static byte[] GetPMTPATData(bool isOnlyVideo=false) {

            if (_pmt_pat_tsdata == null) {
                var msOutput = new MemoryStream();
                TSProgramManage ts_pm = new TSProgramManage();

                TS_PAT pat = new TS_PAT();
                var pat_data = pat.GetBytes();

                TSPacket pack_pat = new TSPacket() {
                    sync_byte = 0x47,
                    transport_error_indicator = 0,
                    payload_unit_start_indicator = 1,
                    transport_priority = 0,
                    PID = 0,
                    transport_scrambling_control = 0,
                    adaptation_field_control = 1,
                    continuity_counter = ts_pm.GetCounter(0),
                    data = pat_data,
                };
                var pack_pat_data = pack_pat.GetBytes();
                var pack_pat_tmp = new TSPacket() {
                    ProgramManage = ts_pm,
                };
                msOutput.Write(pack_pat_data, 0, pack_pat_data.Length);

                //pack_pat_tmp.SetBytes(pack_pat_data);
                //pack_pat_tmp.Decode();


                TS_PMT pmt = new TS_PMT();
                var pmt_data = isOnlyVideo ? pmt.GetVideoBytes() : pmt.GetBytes();
                TSPacket pack_pmt = new TSPacket() {
                    sync_byte = 0x47,
                    transport_error_indicator = 0,
                    payload_unit_start_indicator = 1,
                    transport_priority = 0,
                    PID = 256,//PMT_PID
                    transport_scrambling_control = 0,
                    adaptation_field_control = 1,
                    continuity_counter = ts_pm.GetCounter(256),
                    data = pmt_data,
                };
                var pack_pmt_data = pack_pmt.GetBytes();
                var pack_pmt_tmp = new TSPacket() {
                    ProgramManage = ts_pm,
                };
                msOutput.Write(pack_pmt_data, 0, pack_pmt_data.Length);

                //pack_pmt_tmp.SetBytes(pack_pmt_data);
                //pack_pmt_tmp.Decode();

                var _out = msOutput.ToArray();
                _pmt_pat_tsdata = _out;
            }
            return _pmt_pat_tsdata;
        }
       
    }


    #region AdaptationInfo
    public class AdaptationInfo {
        public byte adaptation_field_length;                    //8

        public byte discontinuity_indicator;                     //1
        public byte random_access_indicator;                 //1
        public byte elementary_stream_priority_indicator;    //1
        public byte PCR_flag;                                  //1
        public byte OPCR_flag;                                //1
        public byte splicing_point_flag;                      //1
        public byte transport_private_data_flag;             //1
        public byte adaptation_field_extension_flag;        //1

        public long PCR_base;                                //33bit
        public byte PCR_reserved;                            //6
        public short PCR_ext;                                //9

        public long OPCR_base;                              //33bit
        public byte OPCR_reserved;                          //6
        public short OPCR_ext;                                 //9

        public byte splice_countdown;                       //8
        public byte transport_private_data_length;      //8

        public void SetBytes(byte[] buffer) {
            adaptation_field_length = buffer[4];
            var ms = new MemoryStream(buffer, 4, adaptation_field_length + 1);
            var bs = new BitStream(ms);
            bs.Position = 0;
            bs.Read(out adaptation_field_length, 0, 8);

            bs.Read(out discontinuity_indicator, 0, 1);
            bs.Read(out random_access_indicator, 0, 1);
            bs.Read(out elementary_stream_priority_indicator, 0, 1);
            bs.Read(out PCR_flag, 0, 1);
            bs.Read(out OPCR_flag, 0, 1);
            bs.Read(out splicing_point_flag, 0, 1);
            bs.Read(out transport_private_data_flag, 0, 1);
            bs.Read(out adaptation_field_extension_flag, 0, 1);

            if (PCR_flag == 1) {
                bs.Read(out PCR_base, 0, 33);
                bs.Read(out PCR_reserved, 0, 6);
                bs.Read(out PCR_ext, 0, 9);
            }
            if (OPCR_flag == 1) {//未测试
                bs.Read(out OPCR_base, 0, 33);
                bs.Read(out OPCR_reserved, 0, 6);
                bs.Read(out OPCR_ext, 0, 9);
            }
            if (adaptation_field_length > 7) {
                bs.Read(out splice_countdown, 0, 8);
                bs.Read(out transport_private_data_length, 0, 8);
            }
        }

        public byte[] GetBytes() {
            if (adaptation_field_length == 0) {
                throw new Exception();
            }
            var len = adaptation_field_length;

            var bs = new BitStream();

            bs.Write(adaptation_field_length);

            bs.Write(discontinuity_indicator, 0, 1);
            bs.Write(random_access_indicator, 0, 1);
            bs.Write(elementary_stream_priority_indicator, 0, 1);
            bs.Write(PCR_flag, 0, 1);
            bs.Write(OPCR_flag, 0, 1);
            bs.Write(splicing_point_flag, 0, 1);
            bs.Write(transport_private_data_flag, 0, 1);
            bs.Write(adaptation_field_extension_flag, 0, 1);
            len -= 1;

            if (PCR_flag == 1) {
                bs.Write(PCR_base, 0, 33);
                bs.Write(PCR_reserved, 0, 6);
                bs.Write(PCR_ext, 0, 9);
                len -= 6;
            }

            if (OPCR_flag == 1) {//未测试
                bs.Write(OPCR_base, 0, 33);
                bs.Write(OPCR_reserved, 0, 6);
                bs.Write(OPCR_ext, 0, 9);
                len -= 6;
            }

            //if (false) {//这两个字段暂不使用，未了解
            //    bs.Write(splice_countdown, 0, 8);
            //    bs.Write(transport_private_data_length, 0, 8);
            //    len -= 2;
            //}
            bs.Position = 0;
            var buf = bs.ToByteArray();

            var ms = new MemoryStream();
            ms.Write(buf, 0, buf.Length);

            var fill = new byte[len];
            ms.Write(fill, 0, fill.Length);
            ms.Position = 0;
            buf = ms.ToArray();

            return buf;
        }
    }

    #endregion

}




