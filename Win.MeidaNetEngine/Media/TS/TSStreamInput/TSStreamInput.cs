using GLib.AXLib.Utility;
using GLib.GeneralModel;
using GLib.IO;
using SLW.Comm;
using Win.MediaNetEngine;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml.Serialization;

namespace SLW.MediaServer.Media.TS
{

    public class WSIPTVChannelInfo 
    {
        public string Name { get; set; }

        public string Key { get; set; }

        public string IP { get; set; }
        public int Port { get; set; }

        public WSIPTVChannelInfo()
        {

        }

        public WSIPTVChannelInfo(string key, string name, string ip, int port)
        {
            Key = key;
            Name = name;
            IP = ip;
            Port = port;
        }


    }

    public abstract class TSStreamInput : IDisposable {

        protected Boolean _isworking = false;
        protected Boolean _tsHeadFlagFinded = false;
        protected TSProgramManage _tsms = new TSProgramManage();
        protected IOStream _ioStream = new IOStream();
        protected Thread _threadTSStreamResolve = null;

        protected long _audioCount = 0, _videoCount = 0;
        protected bool _IsCanPlay = false;
        private bool _EnabledFrameSequence = true;
        protected FileStream _fileStream = null;
        protected long _lastTimetick = 0;
        protected long _firstTimetick = 0;
        protected long _timetickOffset = 0;
        protected long _firstFrameSystemTick = 0;//收到第1帧时系统的时间数，用于重置帧的时差，该值在开启ResetFrameTimetickForSystemTick时有效
        protected long _firstFrameTimetick = 0;//第1帧时间戳
        public bool IsNormal { get; protected set; }

        public bool IsCanPlay { get { return IsNormal && _IsCanPlay; } protected set { _IsCanPlay = value; } }
        //是否对帧进行排序
        public bool EnabledFrameSequence { get { return _EnabledFrameSequence; } set { _EnabledFrameSequence = value; } }

        public WSIPTVChannelInfo IPTVChannelInfo { get; protected set; }

        public virtual Action<MediaFrame> ReceiveFrame { get; set; }

        /// <summary>
        /// 该属性只在调试的时候置为true,主要是解决串流往回调时播放受到影响
        /// </summary>
        public bool AutoResetFrameTimetick { get; set; }

        //是否将帧时间戳重置为系统时间，该项必须启用EnabledFrameSequence才有效
        public bool ResetFrameTimetickForSystemTick { get; set; }

        protected TSStreamInput(WSIPTVChannelInfo info) {
            IPTVChannelInfo = info;
            _tsms = new TSProgramManage();
            _tsms.NewMediaFrame = NewMediaFrame;
        }

        public virtual void Start() {
            if (_isworking)
                return;
            _isworking = true;

            OnStart();

            _threadTSStreamResolve = ThreadEx.ThreadCall(TSStreamResolveThread);

        }

        public virtual void Stop() {
            if (!_isworking)
                return;
            _isworking = false;

            OnStop();

            _threadTSStreamResolve = ThreadEx.ThreadCall(TSStreamResolveThread);

            if (_fileStream != null)
                _fileStream.Close();
        }

        protected virtual void Dispose(bool all)
        {
            Stop();

            if (_ioStream != null)
                _ioStream.Close();
            if (_tsms != null)
                _tsms.Dispose();

        }
        protected abstract void OnStart();

        protected abstract void OnStop();

        protected virtual void OnDataReceived(byte[] data) {

            if (!_isworking)
                return;

  
            if (!_tsHeadFlagFinded) {
                var offset = FindTSHeadFlag(data);
                data = data.Skip(offset).ToArray();
                _tsHeadFlagFinded = true;
            }
            _ioStream.Write(data);
        }

        protected virtual void TSStreamResolveThread() {

            byte[] buffer_ts = new byte[188 * 2];   //两个包的空间，这里需要读取两个188字节的ts packet 而不是读取一个的原因是，
            //读取两个，当可以检验前后ts packet标识字段是否为0x47，如果不是则当前位置不是处理ts packet的开头，
            //则需要重新找到ts packet的头
            byte[] part_ts = null;//当检测到当前ts packet错误的时候 part_ts用来保存正确的ts packet中的一部分
            while (_isworking) {
                int readSize = 188 * 2 - (part_ts != null ? part_ts.Length : 0);//如果上一次没有残留数据则长度为 2个ts packet 的长度
                if (_ioStream.Length >= readSize) {

                    if (part_ts != null)
                        Array.Copy(part_ts, 0, buffer_ts, 0, part_ts.Length);//残留数据
                    _ioStream.Read(buffer_ts, buffer_ts.Length - readSize, readSize);//读取到缓冲区
                    if (buffer_ts[0] != 0x47) {
                        //当前为非 ts packet的头，则找到下一个ts packet的头的偏移
                        int offset = FindTSHeadFlag(buffer_ts);
                        if (offset == -1)//没有找到头则返回起点
                            continue;
                        //从2*188字节中减去偏移，再减去一个ts packet的长度得到残余的数据长度
                        part_ts = new byte[buffer_ts.Length - 188 - offset];
                        Array.Copy(buffer_ts, offset + 188, part_ts, 0, part_ts.Length);

                        //新建一个临时的缓冲区，并将完整 的ts数据copy到这个临时缓冲区中
                        var tmp_buffer = new byte[188];
                        Array.Copy(buffer_ts, offset, tmp_buffer, 0, tmp_buffer.Length);
                        try {
                            TSPacket pack = new TSPacket() { ProgramManage = _tsms };
                            pack.SetBytes(tmp_buffer);
                            pack.Decode();
                            if (pack.PacketType == TSPacketType.DATA)
                                pack.ProgramManage.WriteMediaTSPacket(pack);
                        }
                        catch (Exception e)
                        {
                            _DebugEx.Trace("TSStreamInput", "解析TS失败 1" + e.ToString());
                        }
                    } else {
                        //当前为 ts packet的头，则找到下一个ts packet的头的偏移
                        try {
                            //判断前后两个ts packet是否开头均正确
                            if (buffer_ts[0] == 0x47 && buffer_ts[188] == 0x47) {
                                //将两个ts packet 分别解析
                                var tmp_buffer = new byte[188];
                                Array.Copy(buffer_ts, 0, tmp_buffer, 0, tmp_buffer.Length);
                                TSPacket pack = new TSPacket() { ProgramManage = _tsms };
                                pack.SetBytes(tmp_buffer);
                                pack.Decode();
                                if (pack.PacketType == TSPacketType.DATA)
                                    pack.ProgramManage.WriteMediaTSPacket(pack);

                                Array.Copy(buffer_ts, 188, tmp_buffer, 0, tmp_buffer.Length);
                                pack = new TSPacket() { ProgramManage = _tsms };
                                pack.SetBytes(tmp_buffer);
                                pack.Decode();
                                if (pack.PacketType == TSPacketType.DATA)
                                    pack.ProgramManage.WriteMediaTSPacket(pack);

                            } else {
                                _DebugEx.Trace("TSStreamInput", "TS Packet HeadFlag Error");
                                throw new Exception("TS Packet HeadFlag Error");
                            }

                        } catch (Exception e) {
                            _DebugEx.Trace("TSStreamInput", "解析TS失败 2"+e.ToString());
                        }
                        part_ts = null;
                    }

                } else {
                    Thread.Sleep(10);
                }
            }
        }

        protected virtual int FindTSHeadFlag(byte[] data) {
            var ms = new MemoryStream(data);
            TSProgramManage tsms = new TSProgramManage();
            int pos = 0;
            while (pos++ < data.Length) {
            //    var index = ms.Position;
                if (ms.ReadByte() == 0x47) {
                    int offset = (int)ms.Position - 1;
                    if (!CheckOffsetIsPacketFlag(data, offset))
                        continue;
                    ms.Seek(-1, SeekOrigin.Current);
                    byte[] buf = new byte[188];
                    ms.Read(buf, 0, buf.Length);
                    try {
                        TSPacket pack = new TSPacket() { ProgramManage = tsms };
                        pack.SetBytes(buf);
                        pack.Decode();
                        ms.Seek(-188, SeekOrigin.Current);
                        return (int)ms.Position;
                    }
                    catch (Exception)
                    {
                        return -1;
                    }
                }
            }
            return -1;
        }

        protected virtual bool CheckOffsetIsPacketFlag(byte[] data, int offset) {
            //判断下一个包是否为为0x47开头，如果不是则当前位置不是一个packet的开头
            while (offset < data.Length) {
                if (data[offset] != 0x47) {
                    return false;
                }
                offset += 188;
            }
            return true;
        }


        private AQueue<MediaFrame> _qVideoMediaFrame = new AQueue<MediaFrame>();

        private AQueue<MediaFrame> _qAudioMediaFrame = new AQueue<MediaFrame>();


        protected virtual void NewMediaFrame(MediaFrame frame) {
           
            if (!EnabledFrameSequence) {
                //不排序
                OnNewMediaFrame(frame);
            } else {
                //排序
                if (frame.nIsAudio == 0) {
                    _qVideoMediaFrame.Enqueue(frame);
                } else if (frame.nIsAudio == 1) {
                    _qAudioMediaFrame.Enqueue(frame);
                }
                while (true) {
                    if (_qVideoMediaFrame.Count > 0 && _qAudioMediaFrame.Count > 0) {
                        var v = _qVideoMediaFrame.Peek();
                        var a = _qAudioMediaFrame.Peek();
                        if (v.nTimetick < a.nTimetick) {
                            v = _qVideoMediaFrame.Dequeue();
                            OnNewMediaFrame(v);
                        } else {
                            a = _qAudioMediaFrame.Dequeue();
                            OnNewMediaFrame(a);
                        }
                    } else if (_qVideoMediaFrame.Count > 5) {
                       var  v = _qVideoMediaFrame.Dequeue();
                        OnNewMediaFrame(v);
                    } else if (_qAudioMediaFrame.Count > 50) {
                        var a = _qAudioMediaFrame.Dequeue();
                        OnNewMediaFrame(a);
                    } else {
                        break;
                    }
                }

            }

        }
 

        protected void OnNewMediaFrame(MediaFrame frame) {
            if (!_isworking)
                return;

            if (frame.nIsAudio == 0)
                _videoCount++;
            else
                _audioCount++;
 
            if (_firstTimetick == 0)
                _firstTimetick = frame.nTimetick;

            if (AutoResetFrameTimetick) {

                if (frame.nTimetick < _firstTimetick) {
                    return;
                }

                if (frame.nTimetick == _firstTimetick && _lastTimetick != 0) {
                    _tsms.Clean();
                    _qAudioMediaFrame.Clear();
                    _qVideoMediaFrame.Clear();
                    _timetickOffset = _lastTimetick - _firstTimetick;
                }

                //重置时间戳
                frame.nTimetick += _timetickOffset;
                if (frame.nIsAudio == 0) {
                  //  Console.WriteLine(frame.nTimetick - _lastTimetick);
                }

            }
            _lastTimetick = frame.nTimetick;
 
       

            if (ResetFrameTimetickForSystemTick) {
                if (_firstFrameSystemTick == 0) {
                    _firstFrameTimetick = frame.nTimetick;
                    _firstFrameSystemTick = DateTime.Now.Ticks / 10000;
                    
                }
                frame.nTimetick = (frame.nTimetick - _firstFrameTimetick) + _firstFrameSystemTick;


            }
            if (frame.nIsKeyFrame == 1 && frame.nIsAudio == 0) {
                IsCanPlay = true;
                if (IPTVChannelInfo != null) {
                    Log(string.Format("{0}   video:{1}  audio:{2}   ", IPTVChannelInfo.Name, _videoCount, _audioCount));
                }

 
            }
            try {
                if (ReceiveFrame != null)
                    ReceiveFrame(frame);
            } catch (Exception e) {
               
                _DebugEx.Trace(e);
                throw;
            }

        }
 
        protected virtual void Log(string msg, params string[] ps) {
            Console.WriteLine(String.Format(msg, ps));
        }

         public void Dispose() 
        {
            Dispose(true);
            GC.SuppressFinalize(this);

        }



    }
 
}
