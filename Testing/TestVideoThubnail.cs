using StreamingKit.VideoEncoder;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using StreamingKit.Helper;

namespace Test.VideoThumbnail
{
    public class TestVideoThubnail
    {
        private const string Thumbnails_cmd_format = " -y -i {0}  -f image2   -ss {1}.001 -s 240x180 {2}";

        private const string ffmpeg_param_format = " -y  -i {0} {1}";

       // private static string ffmpeg = "ffmpeg.exe";

        private static List<Process> ProcessList = new List<Process>();

      //  public static event Action<string> OnProcessData;

     //   public static event Action<string> Trace;

        [Fact]
        public void Test()
        {
            VideoFile videoInfo = Thumbnail.GetVideoInfo("record\\tmp4.mp4");
            CreateThumbnails("record\\tmp4.mp4", "record\\abc.jpg");
        }

        static void CreateThumbnails(string iFile, string oFile)
        {
            VideoFile videoInfo = Thumbnail.GetVideoInfo(iFile);
            Thumbnail.CreateThumbnails(iFile, oFile, (int)videoInfo.Duration.TotalSeconds / 2);
        }



    

      
    }
}
