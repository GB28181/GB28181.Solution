using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Win.Media;

namespace Win.MediaServer.Media.TS
{
    //http://www.360doc.com/content/13/0512/11/532901_284774233.shtml

    //http://blog.csdn.net/ccskyer/article/details/7899991


    //http://blog.csdn.net/feixiaku/article/details/39119453
    /*TS流，是基于packet的位流格式，每个packet是188个字节或者204个字节（一般是188字节，204字节格式是在188字节的packet后面加上16字节的CRC数据，其他格式相同），
    解析TS流，先解析每个packet ，然后从一个packet中，解析出PAT的PID，根据PID找到PAT包，
    然后从PAT包中解析出PMT的PID，根据PID找到PMT包，在从PMT包中解析出Video和Audio（也有的包括Teletext和EPG）的PID。然后根据PID找出相应的包。*/

    //http://blog.163.com/benben_168/blog/static/185277057201152125757560/


    public enum TSPacketType {
        UNKNOW,
        PAT,
        PMT,
        OTHER,
        DATA,
    }

    public class TSProgramManage : IDisposable
    {
        private Dictionary<int, byte> _dicCounter = new Dictionary<int, byte>();
        private List<TS.TS_PAT.TS_PAT_Program> _programList = new List<TS_PAT.TS_PAT_Program>();
        private List<TS.TS_PMT.TS_PMT_Stream> _streamList = new List<TS_PMT.TS_PMT_Stream>();
        private Dictionary<int, MediaInfo> _dicMediaFrameCache = new Dictionary<int, MediaInfo>();
        public long FirstFrameTimeTick = -1;
        public Action<MediaFrame> NewMediaFrame;
        public void AddProgram(TS.TS_PAT.TS_PAT_Program program) {
            if (_programList.Contains(program))
                return;
            _programList.Add(program);

        }
        
     

        public void AddStream(TS.TS_PMT.TS_PMT_Stream stream) {
            if (_streamList.Contains(stream))
                return;
            _streamList.Add(stream);

        }

        public bool IsPMT_PID(ushort pid) {

            for (int i = 0; i < _programList.Count; i++)
                if (_programList[i].program_map_PID == pid)
                    return true;
            return false;
             
        }

        public bool IsTSTable(ushort pid) {
            #region 特殊PID映射列 http://blog.163.com/benben_168/blog/static/185277057201152125757560/
            /* 
               TABLE TYPE                      PID VALUE
             
               PAT                             0X0000
               CAT                             0X0001
               TSDT                            0X0002 
               RESERVED                        0X0003 TO 0X000F
               NIT、ST                         0X0010
               SDT、BAT、ST                    0X0011
               EIT、ST                         0X0012 
               RST、ST                         0X0013
               TDT、TOT、ST                    0X0014
               Network Synchroniztion          0X0015
               Reserved for future use         0X0016 TO 0X001B
               Inband signaling                0X001C
               Measurement                     0X001D
               DIT                             0X001E
               SIT                             0X001F
              
           */
            #endregion
            return pid <= 0X001F || IsPMT_PID(pid);

        }

        public bool IsData(ushort pid) {
            for (int i = 0; i < _streamList.Count; i++)
                if (_streamList[i].elementary_PID == pid)
                    return true;
            return false;

        }

   

        public byte GetCounter(int pid) {
            if (_dicCounter.ContainsKey(pid)) {
                return _dicCounter[pid]++;
            } else {
                _dicCounter[pid] = 0;
                return _dicCounter[pid]++;
            }
        }
 
        public void WriteMediaTSPacket(TSPacket pack) {

           // Console.WriteLine(pack.PID);
            var data = pack.data;
            var newFrameFlag = data[0] == 0x0 && data[1] == 0x0 && data[2] == 0x1 && (data[3] == 0xE0 || data[3] == 0xC0);
             
            MediaInfo mi = null;

            lock (_dicMediaFrameCache) {
                if (_dicMediaFrameCache.ContainsKey(pack.PID))
                    mi = _dicMediaFrameCache[pack.PID];
                else {
                    if (newFrameFlag) {
                        _dicMediaFrameCache[pack.PID] = mi = new MediaInfo() {
                            PID = pack.PID,
                            IsVideo = pack.data[3] == 0xE0,
                            IsAudio = pack.data[3] == 0xC0,
                        };
                    }
                }
            }
            if (mi != null) {
                if (newFrameFlag) {
                    lock (mi.TSPackets) {
                        if ((mi.IsAudio && pack.data[3] != 0xC0) || (mi.IsVideo && pack.data[3] != 0xE0)) {
                            lock (_dicMediaFrameCache) {
                                foreach (var item in _dicMediaFrameCache.Values) {
                                    item.Release();
                                }
                                _dicMediaFrameCache.Clear();
                            }
                            return;
                        }
                        
                        var mediaFrame = mi.NextMediaFrame();

                        if (mediaFrame != null &&  mediaFrame.nSize == mediaFrame.Data.Length) {
                            OnNewMediaFrame(mediaFrame);
                        }

                        mi.TSPackets.Clear();

                        mi.TSPackets.Add(pack);
                    }
                } else {
                    lock (mi.TSPackets)
                        mi.TSPackets.Add(pack);
                }
            }
        }

 
        protected void OnNewMediaFrame(MediaFrame frame) {
            NewMediaFrame(frame);
             
            
         
        }


        public void Clean() {
            _dicCounter = new Dictionary<int, byte>();
            _programList = new List<TS_PAT.TS_PAT_Program>();
            _streamList = new List<TS_PMT.TS_PMT_Stream>();
            _dicMediaFrameCache = new Dictionary<int, MediaInfo>();
            FirstFrameTimeTick = -1;
        }

        protected virtual void Dispose(bool all)
        {

        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public override string ToString() {
            string str = string.Format("PAT_Program:{0}  PMT_Stream:{1}", _programList.Count, _streamList.Count);

            lock (_dicMediaFrameCache) {
                foreach (var item in _dicMediaFrameCache.Values) {
                    str += "\r\n" + item.ToString();
                }
            }

            return str;
        }

        class MediaInfo {
            private Boolean _firstAudioFrame = true;
            public int PID;
            public bool IsVideo;
            public bool IsAudio;
            public byte[] SPS;
            public byte[] PPS;
            public int Width;
            public int Height;
            public int  Frequency;
            public int Channel;
            public short AudioFormat;
            public short Samples;
            public int VideoFrameCount;
  //          public int AudioFrameCount;
            public List<TSPacket> TSPackets = new List<TSPacket>();
            public List<PESPacket> PESPackets = new List<PESPacket>();

            private Boolean CheckTSPack() {
                if (TSPackets.Count == 0)
                    return false;
                var ps = TSPackets.ToList();
                var index = ps[0].continuity_counter;
                for (int x = 0; x < ps.Count; x++) {
                    if ((index & 0x0f) != ps[x].continuity_counter) {
                        return false;
                    }
                    index++;
                }
                TSPackets = ps;
                return true;
            }

            public MediaFrame NextMediaFrame()
            {

                var success = CheckTSPack();
                if (!success)
                    return null;

                if (TSPackets.Count == 0)
                    return null;

                //验证PES包头
                if (TSPackets[0].data[0] != 0 || TSPackets[0].data[1] != 0 || TSPackets[0].data[2] != 1) {
                    TSPackets = new List<TSPacket>();
                    return null;
                }
                var tmp_tspackets = TSPackets;
                TSPackets = new List<TSPacket>();

                
                var stream = new MemoryStream();
                foreach (var item in tmp_tspackets) {
                    stream.Write(item.data, 0, item.data.Length);
                }
                stream.Position = 0;
                var pes = new PESPacket();
                pes.SetBytes(stream);
                
                if (IsVideo) {
                    var esdata = pes.PES_Packet_Data;
                    //新的一帧
                    if (PESPackets.Count > 0 && esdata[0] == 0 && esdata[1] == 0 && esdata[2] == 0 && esdata[3] == 1) {
                         
                        stream = new MemoryStream();
                        foreach (var item in PESPackets) {
                            stream.Write(item.PES_Packet_Data, 0, item.PES_Packet_Data.Length);
                        }
                        long tick = PESPackets.FirstOrDefault().GetVideoTimetick();
                        esdata = stream.ToArray();
                        var frame = CreateVideoMediaFrame(esdata, tick); ;
                        PESPackets = new List<PESPacket>();
                        PESPackets.Add(pes);
                        return frame;
                    } else {
                        PESPackets.Add(pes);
                        return null;
                    }
                }else if (IsAudio) {
                    var esdata = pes.PES_Packet_Data;
                    //新的一帧
                    if (PESPackets.Count > 0 && esdata[0] == 0xFF && (esdata[1] & 0xF0)==0xF0) {
                        stream = new MemoryStream();
                        foreach (var item in PESPackets) {
                            stream.Write(item.PES_Packet_Data, 0, item.PES_Packet_Data.Length);
                        }
                        long tick = PESPackets.FirstOrDefault().GetAudioTimetick();
                        PESPackets = new List<PESPacket>();
                        PESPackets.Add(pes);
                        esdata = stream.ToArray();
                        return CreateAudioMediaFrame(esdata, tick);
                    } else {
                        PESPackets.Add(pes);
                        return null;
                    }
                }
                return null;
            }
 
            private MediaFrame CreateVideoMediaFrame(byte[] data,long tick) {

                int keyFrameFlagOffset = 0;
                if (data[0] == 0 && data[1] == 0 && data[2] == 0 && data[3] == 1 && data[4] == 0x09  ) {
                    var tmp = new byte[data.Length - 6];
                    Array.Copy(data, 6, tmp, 0, tmp.Length - 6);
                    data = tmp;
                    keyFrameFlagOffset = 0;
                }

                if (data[keyFrameFlagOffset + 4] == 0x67 || data[keyFrameFlagOffset + 4] == 0x27) {
                    if (this.SPS == null) {
                        var tdata = data;
                        if (keyFrameFlagOffset > 0) {
                            tdata = new byte[data.Length - keyFrameFlagOffset];
                            Array.Copy(data, keyFrameFlagOffset, tdata, 0, tdata.Length);
                        }
                        var sps_pps = SliceHeader.GetSPS_PPS(tdata);
                        var sps = com.googlecode.mp4parser.h264.model.SeqParameterSet.read(new MemoryStream(sps_pps[0], 1, sps_pps[0].Length - 1));
                        var pps = com.googlecode.mp4parser.h264.model.PictureParameterSet.read(new MemoryStream(sps_pps[1], 1, sps_pps[1].Length - 1));
                        Width = (sps.pic_width_in_mbs_minus1 + 1) * 16 - 2 * sps.frame_crop_left_offset - 2 * sps.frame_crop_right_offset;
                        Height = (sps.pic_height_in_map_units_minus1 + 1) * 16 - 2 * sps.frame_crop_top_offset - 2 * sps.frame_crop_bottom_offset;
                        this.SPS = sps_pps[0];
                        this.PPS = sps_pps[1];
                    }
                    var mf = new MediaFrame() {
                        nIsAudio = 0,
                        nIsKeyFrame = 1,
                        Data = data,
                        nSize = data.Length,
                        nWidth = Width,
                        nHeight = Height,
                        nSPSLen = (short)this.SPS.Length,
                        nPPSLen = (short)this.PPS.Length,
                        nTimetick = tick,
                        nOffset = 0,
                        nEncoder = MediaFrame.H264Encoder,
                        nEx = 1,
                    };
                    mf.MediaFrameVersion = (byte)(mf.nIsKeyFrame == 1 ? 1 : 0);
                    VideoFrameCount++;
                    return mf;
                } else {
                    if (this.SPS != null) {
                        var mf = new MediaFrame() {
                            nIsAudio = 0,
                            nIsKeyFrame = 0,
                            Data = data,
                            nSize = data.Length,
                            nWidth = Width,
                            nHeight = Height,
                            nSPSLen = (short)this.SPS.Length,
                            nPPSLen = (short)this.PPS.Length,
                            nTimetick = tick,
                            nOffset = 0,
                            nEncoder = MediaFrame.H264Encoder,
                            nEx = 1,
                        };
                        mf.MediaFrameVersion = (byte)(mf.nIsKeyFrame == 1 ? 1 : 0);
                        VideoFrameCount++;
                        return mf;
                    }
                }
                return null;

            }

            private MediaFrame CreateAudioMediaFrame(byte[] data, long tick) {
                if (_firstAudioFrame) {
                    var adts = new AAC_ADTS(data);
                    var frequencys = new int[] { 96000, 88200, 64000, 48000, 44100, 32000, 24000, 22050, 16000, 2000, 11025 };
                    Frequency = frequencys[adts.MPEG_4_Sampling_Frequency_Index];
                    Channel = adts.MPEG_4_Channel_Configuration;
                    AudioFormat = 16;
                    Samples = 0;
                }
                VideoFrameCount++;
                var mf = new MediaFrame() {
                    nIsAudio = 1,
                    nIsKeyFrame = 1,
                    Data = data,
                    nSize = data.Length,
                    nChannel = Channel,
                    nFrequency = Frequency,
                    nAudioFormat = AudioFormat,
                    nTimetick = tick,
                    nOffset = 0,
                    nEncoder = MediaFrame.AAC_Encoder,
                    nEx = (byte)(_firstAudioFrame ? 0 : 1),
                    MediaFrameVersion = 1,
                };
                _firstAudioFrame = false;

                return mf;
 
            }

            public void Release() {

                TSPackets.Clear();
                PESPackets.Clear();
            }

            public override string ToString() {
                return string.Format("TSPacket:{0}   PESPackets:{1}", TSPackets.Count, PESPackets.Count);
            }
        }


    }
 
}




