
using Xunit;
using Helpers;
using GB28181.WinTool.Codec;
using System;
using System.IO;
using System.Runtime.InteropServices;
using StreamingKit;

namespace Test.GB28181.Service
{
    public class TestH264
    {
        private static BinaryWriter w = null;


        [Fact]
        public void Test264File()
        {
            int width = 320, height = 240;
            var x264 = new X264Native(new X264Params(width, height, 10, 320));

            //x264.SetIKeyIntMax(10);
            x264.Init();

            var ls = StreamingKit.Media.ReadFile.GetBuffByFile1(@".\test.yuv");
            AVCodecCfg cf = AVCodecCfg.CreateVideo(width, height, (int)StreamingKit.AVCode.CODEC_ID_H264, 100000);
            FFImp ffimp = new FFImp(cf, true);
            //FFScale ffscale = new FFScale(width, height, 26, 12, width, height, 12, 12);
            FFScale ffscale = new FFScale(width, height, 0, 12, width, height, 3, 24);
            foreach (var item1 in ls)
            {
                var item = ffscale.FormatS(item1);
                var in_buf = FunctionEx.BytesToIntPtr(item);
                var out_buf = Marshal.AllocHGlobal(item.Length);
                //var bKeyFrame = false;
                //var nOutLen = 0;
                var nInLen = item.Length;
                //  var size = X264Encode(x264.obj, in_buf, ref nInLen, out_buf, ref nOutLen, ref bKeyFrame);
                // var buf = FunctionEx.IntPtrToBytes(out_buf, 0, size);
                var buf = x264.Encode(item);
                Console.WriteLine(buf.To16Strs(0, 16));
                var size = buf.Length;

                if (w == null)  //OK
                {
                    w = new BinaryWriter(new FileStream("4567.es", FileMode.Create));
                }
                w.Write(buf);

            }

        }



        [Fact]
        public void Test264FileFrame()
        {
            int width = 320, height = 240;
            var x264 = new X264Native(new X264Params(width, height, 10, 320));

            //x264.SetIKeyIntMax(10);
            x264.Init();
            var fs = new FileStream("./testfile.test", FileMode.CreateNew);
            var ls = StreamingKit.Media.ReadFile.GetBuffByFile1(@".\test.yuv");
            AVCodecCfg cf = AVCodecCfg.CreateVideo(width, height, (int)StreamingKit.AVCode.CODEC_ID_H264, 100000);
            FFImp ffimp = new FFImp(cf, true);
            //FFScale ffscale = new FFScale(width, height, 26, 12, width, height, 12, 12);
            FFScale ffscale = new FFScale(width, height, 0, 12, width, height, 3, 24);
            foreach (var item1 in ls)
            {
                var item = ffscale.FormatS(item1);
                var in_buf = FunctionEx.BytesToIntPtr(item);
                var out_buf = Marshal.AllocHGlobal(item.Length);
                //var bKeyFrame = false;
                //var nOutLen = 0;
                var nInLen = item.Length;
                //  var size = X264Encode(x264.obj, in_buf, ref nInLen, out_buf, ref nOutLen, ref bKeyFrame);
                // var buf = FunctionEx.IntPtrToBytes(out_buf, 0, size);
                var buf = x264.Encode(item);
                Console.WriteLine(buf.To16Strs(0, 16));
                var size = buf.Length;

                if (w == null)  //OK
                {
                    w = new BinaryWriter(new FileStream("4567.es", FileMode.Create));
                }
                w.Write(buf);

                var mf = new MediaFrame();
                mf.IsKeyFrame = (byte)(x264.IsKeyFrame() ? 1 : 0);
                mf.Width = width;
                mf.Height = height;
                mf.Encoder = MediaFrame.H264Encoder;
                //mf.Timetick = 0;
                mf.Size = size;
                mf.SetData(buf);
                buf = mf.GetBytes();
                fs.Write(BitConverter.GetBytes(buf.Length), 0, 4);
                fs.Write(buf, 0, buf.Length);
                fs.Flush();
               // IntPtr intt = IntPtr.Zero;
                //var sssss = ffimp.VideoDec(buf, ref intt);
                //Console.WriteLine(buf.Take(32).ToArray().To16Strs());
               // var size = Encode1(ii, in_buf, ref nInLen, out_buf);
            }

            fs.Close();

        }

    }
}
