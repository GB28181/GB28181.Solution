using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace StreamingKit.Helper
{
    public class FileStreamHelper
    {
        public static List<byte[]> GetBuffByFile1(string file)
        {
            var q = GetBuffQueueByFile(file);
            return q.Select(p => p.Item2).ToList();
        }
        public static Queue<Tuple<int, byte[]>> GetBuffQueueByFile(string file)
        {
            var r = new Queue<Tuple<int, byte[]>>();
            var fs = new System.IO.FileStream(file, System.IO.FileMode.Open);
            var br = new System.IO.BinaryReader(fs);
            while (fs.Length > fs.Position + 4)
            {
                var len = br.ReadInt32();
                var buff = br.ReadBytes(len);
                var t = new Tuple<int, byte[]>(len, buff);
                if (len == buff.Length)
                    r.Enqueue(t);
            }
            fs.Close();
            return r;
        }
        public static Queue<Tuple<int, byte[]>> GetBuffByFileLong(string file)
        {
            var r = new Queue<Tuple<int, byte[]>>();
            var fs = new FileStream(file, System.IO.FileMode.Open);
            var br = new BinaryReader(fs);
            while (fs.Length > fs.Position + 4)
            {
                var len = br.ReadInt64();
                var buff = br.ReadBytes((int)len);
                var t = new Tuple<int, byte[]>((int)len, buff);
                if (len == buff.Length)
                    r.Enqueue(t);
            }
            fs.Close();
            return r;
        }
        public static List<MediaFrame> GetMediaFrameByFile(string file)
        {
            var list = new List<MediaFrame>();
            var fs = new FileStream(file, System.IO.FileMode.Open, System.IO.FileAccess.Read);
            var br = new BinaryReader(fs);
            while (fs.Length > fs.Position + 4)
            {
                var len = br.ReadInt32();
                var buff = br.ReadBytes(len);
                if (len > buff.Length)
                    break;

                var f = new MediaFrame();
                f.SetBytes(buff);
                list.Add(f);
            }
            return list;
        }
        //从frame文件流转换为裸流文件
        public static void FrameStreamToEStreamFile(string file1, string file2)
        {
            var list = GetMediaFrameByFile(file1);
            var fsOut = new FileStream(file2, FileMode.Create);
            foreach (var item in list)
                fsOut.Write(item.GetData(), 0, item.GetData().Length);
            fsOut.Flush();
            fsOut.Close();
        }
        //从frame文件流转换为len+data文件
        public static void FrameStreamToBStreamFile(string file1, string file2)
        {
            var list = GetMediaFrameByFile(file1);
            var fsOut = new FileStream(file2, FileMode.CreateNew);
            foreach (var item in list)
            {
                fsOut.Write(BitConverter.GetBytes(item.GetData().Length), 0, 4);
                fsOut.Write(item.GetData(), 0, item.GetData().Length);
            }
            fsOut.Flush(); fsOut.Close();
        }
        //从frame文件流转换为len+data文件
        public static void FrameStreamToEStreamFile(List<MediaFrame> list, string file2)
        {

            var fsOut = new FileStream(file2, FileMode.CreateNew);
            foreach (var item in list)
            {

                fsOut.Write(item.GetData(), 0, item.GetData().Length);
            }
            fsOut.Flush(); fsOut.Close();
        }
        public static void ToFrameStreamFile(List<MediaFrame> list, string file2)
        {

            var fsOut = new FileStream(file2, FileMode.Create);
            foreach (var item in list)
            {
                var buf = item.GetBytes();
                fsOut.Write(BitConverter.GetBytes(buf.Length), 0, 4);
                fsOut.Write(buf, 0, buf.Length);
            }
            fsOut.Flush(); fsOut.Close();
        }


    }
    public class StreamFileHelper : FileStreamHelper
    {
    }
}
