using StreamingKit.VideoEncoder;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace StreamingKit.Helper
{
	public class Thumbnail
	{
		private const string Thumbnails_cmd_format = " -y -i {0}  -f image2   -ss {1}.001 -s 240x180 {2}";

		private const string ffmpeg_param_format = " -y  -i {0} {1}";

		//you need this tool installed!!!!!!!!!
		private static string ffmpeg = "ffmpeg.exe";

		private static List<Process> ProcessList = new List<Process>();

		public static event Action<string> OnProcessData;

		//public static event Action<string> Trace;


		public static void CreateThumbnails(string iFile, string oFile)
		{
			VideoFile videoInfo = GetVideoInfo(iFile);
			CreateThumbnails(iFile, oFile, (int)videoInfo.Duration.TotalSeconds / 2);
		}

		public static void CreateThumbnails(string iFile, string oFile, int time)
		{
			VideoFile videoInfo = GetVideoInfo(iFile);
			oFile =   Path.GetFileName(oFile) + "\\" + Path.GetFileName(oFile);
			iFile = Path.GetFileName(iFile);
			var num = videoInfo.Duration.TotalSeconds / 2.0;
			if (num > 8.0)
			{
				num = 8.0;
			}
			var arguments = string.Format(" -y -i {0}  -f image2 -ss {2}.001 -s 240x180 {1}", iFile, oFile, time);
			var p = CreateProcess(ffmpeg, arguments);
			RunProcess(p);
		}

		public static void RunProcess(Process p)
		{
			DateTime dt = DateTime.Now;
			try
			{
				ProcessList.Add(p);
				p.Start();
				p.BeginOutputReadLine();
				p.BeginErrorReadLine();
				void value(object sender, DataReceivedEventArgs e)
				{
					dt = DateTime.Now;
					OnProcessData?.Invoke(e.Data);
				}
				p.ErrorDataReceived += value;
				p.OutputDataReceived += value;
				Task task = new Task(delegate
				{
					p.WaitForExit();
				});
				task.Start();
				do
				{
					Thread.Sleep(100);
				}
				while (dt.AddMinutes(1.0) > DateTime.Now && !task.IsCompleted && !task.IsFaulted && !task.IsCanceled);
				p.ErrorDataReceived -= value;
				p.OutputDataReceived -= value;
				p.CancelOutputRead();
				p.CancelErrorRead();
				if (!p.HasExited)
				{
					p.Kill();
				}
				p.Close();
				p.Dispose();
				ProcessList.Remove(p);
			}
			catch
			{
				p.Dispose();
				throw;
			}
		}

		public static VideoFile GetVideoInfo(string iFile)
		{
			VideoEncoder.Encoder encoder = new VideoEncoder.Encoder
			{
				FFmpegPath = ffmpeg
			};
			VideoFile videoFile = new VideoFile(iFile);
			encoder.GetVideoInfo(videoFile);
			TimeSpan duration = videoFile.Duration;
			StringBuilder stringBuilder = new StringBuilder();
			string arg = $"{(int)duration.TotalHours:00}:{duration.Minutes:00}:{duration.Seconds:00}";
			stringBuilder.AppendFormat("时间长度：{0}\n", arg);
			stringBuilder.AppendFormat("高度：{0}\n", videoFile.Height);
			stringBuilder.AppendFormat("宽度：{0}\n", videoFile.Width);
			stringBuilder.AppendFormat("数据格式：{0}\n", videoFile.VideoFormat);
			stringBuilder.AppendFormat("比特率：{0}\n", videoFile.BitRate);
			stringBuilder.AppendFormat("文件路径：{0}\n", videoFile.File);
			return videoFile;
		}

		public static Process CreateProcess(string exe, string arguments)
		{
			string text = $"{exe} {arguments}";
			Process process = new Process();
			process.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
			process.StartInfo.FileName = exe;
			process.StartInfo.Arguments = arguments;
			process.StartInfo.UseShellExecute = false;
			process.StartInfo.RedirectStandardError = true;
			process.StartInfo.RedirectStandardOutput = true;
			process.StartInfo.CreateNoWindow = true;
			return process;
		}

		public static void MP42TS(string mp4, string ts)
		{
			var arguments = $" -y -i {mp4}  -codec copy -bsf h264_mp4toannexb {ts}";
			var p = CreateProcess(ffmpeg, arguments);
			RunProcess(p);
		}
	}
}
