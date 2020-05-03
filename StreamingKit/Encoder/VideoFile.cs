using System;

namespace StreamingKit.VideoEncoder
{
	public class VideoFile
	{
		public string File { get; set; }

		public TimeSpan Duration { get; set; }

		public double BitRate { get; set; }

		public string RawAudioFormat { get; set; }

		public string AudioFormat { get; set; }

		public string RawVideoFormat { get; set; }

		public string VideoFormat { get; set; }

		public int Height { get; set; }

		public int Width { get; set; }

		public string RawInfo { get; set; }
		public bool InfoGathered { get; set; }
		public VideoFile(string path)
		{
			File = path;
			Initialize();
		}

		private void Initialize()
		{
			InfoGathered = false;
			if (string.IsNullOrEmpty(File))
			{
				throw new Exception("Video file Path not set or empty.");
			}
			if (!System.IO.File.Exists(File))
			{
				throw new Exception("The video file " + File + " does not exist.");
			}
		}

		public override string ToString()
		{
			return $"w*h:{Width}*{Height}, time:{Duration}, format:{VideoFormat}, raw:{RawVideoFormat}, ";
		}
	}
}
