using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace StreamingKit.VideoEncoder
{
    public class Encoder
    {
        public string FFmpegPath { get; set; }

        private string Params { get; set; }

        public EncodedVideo EncodeVideo(VideoFile input, string encodingCommand, string outputFile, bool getVideoThumbnail)
        {
            EncodedVideo encodedVideo = new EncodedVideo();
            Params = $"-i {input.File} {encodingCommand} {outputFile}";
            var text2 = encodedVideo.EncodingLog = RunProcess(Params);
            encodedVideo.EncodedVideoPath = outputFile;
            if (File.Exists(outputFile))
            {
                encodedVideo.Success = true;
                if (getVideoThumbnail)
                {
                    string text3 = outputFile + "_thumb.jpg";
                    if (GetVideoThumbnail(input, text3))
                    {
                        encodedVideo.ThumbnailPath = text3;
                    }
                }
            }
            else
            {
                encodedVideo.Success = false;
            }
            return encodedVideo;
        }

        public bool GetVideoThumbnail(VideoFile input, string saveThumbnailTo)
        {
            if (!input.InfoGathered)
            {
                GetVideoInfo(input);
            }
            int num = (int)Math.Round(TimeSpan.FromTicks(input.Duration.Ticks / 3).TotalSeconds, 0);
            string parameters = $"-i {input.File} {saveThumbnailTo} -vcodec mjpeg -ss {num} -vframes 1 -an -f rawvideo";
            string text = RunProcess(parameters);
            if (File.Exists(saveThumbnailTo))
            {
                return true;
            }
            parameters = $"-i {input.File} {saveThumbnailTo} -vcodec mjpeg -ss {1} -vframes 1 -an -f rawvideo";
            text = RunProcess(parameters);
            if (File.Exists(saveThumbnailTo))
            {
                return true;
            }
            return false;
        }

        public void GetVideoInfo(VideoFile input)
        {
            string parameters = $"-i {input.File}";
            string text2 = input.RawInfo = RunProcess(parameters);
            input.Duration = ExtractDuration(input.RawInfo);
            input.BitRate = ExtractBitrate(input.RawInfo);
            input.RawAudioFormat = ExtractRawAudioFormat(input.RawInfo);
            input.AudioFormat = ExtractAudioFormat(input.RawAudioFormat);
            input.RawVideoFormat = ExtractRawVideoFormat(input.RawInfo);
            input.VideoFormat = ExtractVideoFormat(input.RawVideoFormat);
            input.Width = ExtractVideoWidth(input.RawInfo);
            input.Height = ExtractVideoHeight(input.RawInfo);
            input.InfoGathered = true;
        }

        private string RunProcess(string Parameters)
        {
            var processStartInfo = new ProcessStartInfo(FFmpegPath, Parameters)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            string result = null;
            StreamReader streamReader = null;
            try
            {
                Process process = Process.Start(processStartInfo);
                process.WaitForExit();
                streamReader = process.StandardError;
                result = streamReader.ReadToEnd();
                process.Close();
            }
            catch (Exception)
            {
                result = string.Empty;
            }
            finally
            {
                if (streamReader != null)
                {
                    streamReader.Close();
                    streamReader.Dispose();
                }
            }
            return result;
        }

        private TimeSpan ExtractDuration(string rawInfo)
        {
            TimeSpan result = new TimeSpan(0L);
            Regex regex = new Regex("[D|d]uration:.((\\d|:|\\.)*)", RegexOptions.Compiled);
            Match match = regex.Match(rawInfo);
            if (match.Success)
            {
                string value = match.Groups[1].Value;
                string[] array = value.Split(':', '.');
                if (array.Length == 4)
                {
                    result = new TimeSpan(0, Convert.ToInt16(array[0]), Convert.ToInt16(array[1]), Convert.ToInt16(array[2]), Convert.ToInt16(array[3]));
                }
            }
            return result;
        }

        private double ExtractBitrate(string rawInfo)
        {
            Regex regex = new Regex("[B|b]itrate:.((\\d|:)*)", RegexOptions.Compiled);
            Match match = regex.Match(rawInfo);
            double result = 0.0;
            if (match.Success)
            {
                double.TryParse(match.Groups[1].Value, out result);
            }
            return result;
        }

        private string ExtractRawAudioFormat(string rawInfo)
        {
            string text = string.Empty;
            Regex regex = new Regex("[A|a]udio:.*", RegexOptions.Compiled);
            Match match = regex.Match(rawInfo);
            if (match.Success)
            {
                text = match.Value;
            }
            return text.Replace("Audio: ", "");
        }

        private string ExtractAudioFormat(string rawAudioFormat)
        {
            string[] array = rawAudioFormat.Split(new string[1]
            {
                ", "
            }, StringSplitOptions.None);
            return array[0].Replace("Audio: ", "");
        }

        private string ExtractRawVideoFormat(string rawInfo)
        {
            string text = string.Empty;
            Regex regex = new Regex("[V|v]ideo:.*", RegexOptions.Compiled);
            Match match = regex.Match(rawInfo);
            if (match.Success)
            {
                text = match.Value;
            }
            return text.Replace("Video: ", "");
        }

        private string ExtractVideoFormat(string rawVideoFormat)
        {
            string[] array = rawVideoFormat.Split(new string[1]
            {
                ", "
            }, StringSplitOptions.None);
            return array[0].Replace("Video: ", "");
        }

        private int ExtractVideoWidth(string rawInfo)
        {
            int result = 0;
            Regex regex = new Regex("(\\d{2,4})x(\\d{2,4})", RegexOptions.Compiled);
            Match match = regex.Match(rawInfo);
            if (match.Success)
            {
                int.TryParse(match.Groups[1].Value, out result);
            }
            return result;
        }

        private int ExtractVideoHeight(string rawInfo)
        {
            int result = 0;
            Regex regex = new Regex("(\\d{2,4})x(\\d{2,4})", RegexOptions.Compiled);
            Match match = regex.Match(rawInfo);
            if (match.Success)
            {
                int.TryParse(match.Groups[2].Value, out result);
            }
            return result;
        }
    }
}
