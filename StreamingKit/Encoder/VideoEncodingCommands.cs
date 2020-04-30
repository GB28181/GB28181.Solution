namespace StreamingKit.VideoEncoder
{
	public class VideoEncodingCommands
	{
		private static string LQVideoBitrate = "256k";

		private static string MQVideoBitrate = "512k";

		private static string HQVideoBitrate = "756k";

		private static string VHQVideoBitrate = "1024k";

		private static string LQAudioBitrate = "32k";

		private static string MQAudioBitrate = "64k";

		private static string HQAudioBitrate = "96k";

		private static string VHQAudioBitrate = "128k";

		private static string LQAudioSamplingFrequency = "22050";

		private static string MQAudioSamplingFrequency = "44100";

		private static string HQAudioSamplingFrequency = "44100";

		private static string SQCIF = "sqcif";

		private static string QCIF = "qcif";

		private static string QVGA = "qvga";

		private static string CIF = "cif";

		private static string VGA = "vga";

		private static string SVGA = "svga";

	//	private static string N43 = "480x360";

		public static string FLVLowQualityQCIF = $"-y -b {LQVideoBitrate} -ab {LQAudioBitrate} -ar {LQAudioSamplingFrequency} -s {QVGA} -f flv";

		public static string FLVMediumQualityCIF = $"-y -b {MQVideoBitrate} -ab {MQAudioBitrate} -ar {MQAudioSamplingFrequency} -s {CIF} -f flv";

		public static string FLVHighQualityVGA = $"-y -b {HQVideoBitrate} -ab {HQAudioBitrate} -ar {HQAudioSamplingFrequency} -s {VGA} -f flv";

		public static string FLVVeryHighQualitySVGA = $"-y -b {VHQVideoBitrate} -ab {VHQAudioBitrate} -ar {HQAudioSamplingFrequency} -s {SVGA} -f flv";

		public static string FLVLowQualityKeepOriginalSize = string.Format("-y -b {0} -ab {1} -ar {2} -f flv", LQVideoBitrate, LQAudioBitrate, LQAudioSamplingFrequency, QVGA);

		public static string FLVMediumQualityKeepOriginalSize = string.Format("-y -b {0} -ab {1} -ar {2} -f flv", MQVideoBitrate, MQAudioBitrate, MQAudioSamplingFrequency, CIF);

		public static string FLVHighQualityKeepOriginalSize = string.Format("-y -b {0} -ab {1} -ar {2} -f flv", HQVideoBitrate, HQAudioBitrate, HQAudioSamplingFrequency, VGA);

		public static string FLVVeryHighQualityKeepOriginalSize = string.Format("-y -b {0} -ab {1} -ar {2} -f flv", VHQVideoBitrate, VHQAudioBitrate, HQAudioSamplingFrequency, SVGA);

		public static string THREEGPLowQualitySQCIF = $"-y -acodec aac -ac 1 -b {LQVideoBitrate} -ab {LQAudioBitrate} -ar {LQAudioSamplingFrequency} -s {SQCIF} -f 3gp";

		public static string THREEGPMediumQualityQCIF = $"-y -acodec aac -b {MQVideoBitrate} -ab {MQAudioBitrate} -ar {MQAudioSamplingFrequency} -s {QCIF} -f 3gp";

		public static string THREEGPHighQualityCIF = $"-y -acodec aac -b {VHQVideoBitrate} -ab {VHQAudioBitrate} -ar {HQAudioSamplingFrequency} -s {CIF} -f 3gp";

		public static string MP4LowQualityKeepOriginalSize = string.Format("-y -b {0} -ab {1} -ar {2} -f mp4", LQVideoBitrate, LQAudioBitrate, LQAudioSamplingFrequency, QVGA);

		public static string MP4MediumQualityKeepOriginalSize = string.Format("-y -b {0} -ab {1} -ar {2} -f mp4", MQVideoBitrate, MQAudioBitrate, MQAudioSamplingFrequency, CIF);

		public static string MP4HighQualityKeepOriginalSize = string.Format("-y -b {0} -ab {1} -ar {2} -f mp4", HQVideoBitrate, HQAudioBitrate, HQAudioSamplingFrequency, VGA);

		public static string MP4LowQualityQVGA = $"-y -b {LQVideoBitrate} -ab {LQAudioBitrate} -ar {LQAudioSamplingFrequency} -s {QVGA} -f mp4";

		public static string MP4MediumQualityCIF = $"-y -b {MQVideoBitrate} -ab {MQAudioBitrate} -ar {MQAudioSamplingFrequency} -s {CIF} -f mp4";

		public static string MP4HighQualityVGA = $"-y -b {HQVideoBitrate} -ab {HQAudioBitrate} -ar {HQAudioSamplingFrequency} -s {VGA} -f mp4";

		public static string WMVLowQualityQVGA = $"-y -vcodec wmv2  -acodec wmav2 -b {LQVideoBitrate} -ab {LQAudioBitrate} -ar {LQAudioSamplingFrequency} -s {QVGA}";

		public static string WMVMediumQualityCIF = $"-y -vcodec wmv2  -acodec wmav2 -b {MQVideoBitrate} -ab {MQAudioBitrate} -ar {MQAudioSamplingFrequency} -s {CIF}";

		public static string WMVHighQualityVGA = $"-y -vcodec wmv2  -acodec wmav2 -b {HQVideoBitrate} -ab {HQAudioBitrate} -ar {HQAudioSamplingFrequency} -s {VGA}";

		public static string WMVVeryHighQualitySVGA = $"-y -vcodec wmv2  -acodec wmav2 -b {VHQVideoBitrate} -ab {VHQAudioBitrate} -ar {HQAudioSamplingFrequency} -s {SVGA}";

		public static string WMVLowQualityKeepOriginalSize = string.Format("-y -vcodec wmv2  -acodec wmav2 -b {0} -ab {1} -ar {2}", LQVideoBitrate, LQAudioBitrate, LQAudioSamplingFrequency, QVGA);

		public static string WMVMediumQualityKeepOriginalSize = string.Format("-y -vcodec wmv2  -acodec wmav2 -b {0} -ab {1} -ar {2}", MQVideoBitrate, MQAudioBitrate, MQAudioSamplingFrequency, CIF);

		public static string WMVHighQualityKeepOriginalSize = string.Format("-y -vcodec wmv2  -acodec wmav2 -b {0} -ab {1} -ar {2}", HQVideoBitrate, HQAudioBitrate, HQAudioSamplingFrequency, VGA);

		public static string WMVVeryHighQualityKeepOriginalSize = string.Format("-y -vcodec wmv2  -acodec wmav2 -b {0} -ab {1} -ar {2}", VHQVideoBitrate, VHQAudioBitrate, HQAudioSamplingFrequency, SVGA);
	}
}
