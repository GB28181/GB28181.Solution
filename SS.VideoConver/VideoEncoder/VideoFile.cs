using System;
using System.IO;

namespace VideoEncoder
{
	public class VideoFile
	{
		private string _File;

		public string File
		{
			get
			{
				return _File;
			}
			set
			{
				_File = value;
			}
		}

		public TimeSpan Duration
		{
			get;
			set;
		}

		public double BitRate
		{
			get;
			set;
		}

		public string RawAudioFormat
		{
			get;
			set;
		}

		public string AudioFormat
		{
			get;
			set;
		}

		public string RawVideoFormat
		{
			get;
			set;
		}

		public string VideoFormat
		{
			get;
			set;
		}

		public int Height
		{
			get;
			set;
		}

		public int Width
		{
			get;
			set;
		}

		public string RawInfo
		{
			get;
			set;
		}

		public bool infoGathered
		{
			get;
			set;
		}

		public VideoFile(string path)
		{
			_File = path;
			Initialize();
		}

		private void Initialize()
		{
			infoGathered = false;
			if (string.IsNullOrEmpty(_File))
			{
				throw new Exception("Video file Path not set or empty.");
			}
			if (!System.IO.File.Exists(_File))
			{
				throw new Exception("The video file " + _File + " does not exist.");
			}
		}

		public override string ToString()
		{
			return $"w*h:{Width}*{Height}, time:{Duration}, format:{VideoFormat}, raw:{RawVideoFormat}, ";
		}
	}
}
