using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Win.GB28181.Client.Player.Analyzer
{
    /// <summary>
    /// 分析视频流
    /// </summary>
    public class StreamAnalyzer
    {
        private bool initialized = false;
        public byte[] SPS;
        public byte[] PPS;
        private byte[] Buffer;
        private byte IsKeyFrame;

        /// <summary>
        /// 视频宽度
        /// </summary>
        public ushort Width;
        /// <summary>
        /// 视频高度
        /// </summary>
        public ushort Height;
        /// <summary>
        /// 视频帧率
        /// </summary>
        public ushort Framerate;

        public MediaFramePacket GetMediaFramePacket()
        {
            if (this.Buffer == null)
                return null;
            try
            {
                byte[] zero = new byte[] { 0x00 };
                if (Buffer[0] == 0x00 && Buffer[1] == 0x00 && Buffer[2] == 0x01)
                {
                    //Buffer = copybyte(zero, Buffer);nal_unit_type & 0x0F
                    Buffer = zero.Concat(Buffer).ToArray();

                    int int68 = 0;
                    int int65 = 0;
                    bool spsStartCode = Buffer[0] == 0x00 && Buffer[1] == 0x00 && Buffer[2] == 0x00 && Buffer[3] == 0x01 && ((Buffer[4] & 0x0F) == 7);
                    if (spsStartCode)
                    {
                        int i = 0;
                        int j = 0;
                        int bytes = Buffer.Length - 4;
                        while (i < bytes)
                        {
                            if (Buffer[i] == 0x00 && Buffer[i + 1] == 0x00 && Buffer[i + 2] == 0x01 && ((Buffer[i + 3] & 0x0F) == 8))
                            {
                                int68 = i;
                                break;
                            }
                            i++;
                        }

                        byte[] firstByte68 = new byte[i];
                        byte[] senondByte68 = new byte[Buffer.Length - i];
                        Array.Copy(Buffer, firstByte68, i);
                        Array.Copy(Buffer, i, senondByte68, 0, Buffer.Length - i);
                        senondByte68 = zero.Concat(senondByte68).ToArray();
                        Buffer = firstByte68.Concat(senondByte68).ToArray();

                        i = 0;
                        while (i < bytes)
                        {
                            if (Buffer[i] == 0x00 && Buffer[i + 1] == 0x00 && Buffer[i + 2] == 0x01 && ((Buffer[i + 3] & 0x0F) == 5))
                            {
                                int65 = i;
                                break;
                            }
                            i++;
                        }

                        j = int65;

                        byte[] firstByte65 = new byte[j];
                        byte[] senondByte65 = new byte[Buffer.Length - j];
                        Array.Copy(Buffer, firstByte65, j);
                        Array.Copy(Buffer, j, senondByte65, 0, Buffer.Length - j);
                        senondByte65 = zero.Concat(senondByte65).ToArray();
                        Buffer = firstByte65.Concat(senondByte65).ToArray();
                    }

                }
                this.SPS_PPS(this.Buffer);
            }
            catch (Exception ex)
            {
                Console.WriteLine("gb获取高度宽度异常:" + ex.ToString());
            }

            return new MediaFramePacket(this.Buffer, MediaType.VideoES, DateTime.Now.Ticks, this.Framerate, (byte)this.IsKeyFrame, this.Width, this.Height);

        }

        public int InputData(uint dwDataType, byte[] pBuffer, uint dwSize, int width, int height, int isKeyFrame, ushort frameRate)
        {
            this.IsKeyFrame = (byte)isKeyFrame;
            this.Buffer = pBuffer;
            //Marshal.Copy(pBuffer, this.Buffer, 0, (int)dwSize);

            return 0;
        }

        private void SPS_PPS(byte[] data)
        {
            bool spsStartCode = data[0] == 0x00 && data[1] == 0x00 && data[2] == 0x00 && data[3] == 0x01 && (data[4] == 0x67 || data[4] == 0x27);
            if (spsStartCode)
                this.IsKeyFrame = 1;
            else
                this.IsKeyFrame = 0;
            if (initialized)
                return;
            int nal_unit_type = data[4];
            if (data[0] == 0 && data[1] == 0 && data[2] == 0 && data[3] == 1)
            {
                if (nal_unit_type == 0x67 || (nal_unit_type & 0x0F) == 7)
                {

                    var sps_pps = SliceHeader.GetSPS_PPS(data);
                    if (sps_pps == null)
                    {
                        return;
                    }
                    var sps = com.googlecode.mp4parser.h264.model.SeqParameterSet.read(new MemoryStream(sps_pps[0], 1, sps_pps[0].Length - 1));
                    var pps = com.googlecode.mp4parser.h264.model.PictureParameterSet.read(new MemoryStream(sps_pps[1], 1, sps_pps[1].Length - 1));
                    Width = (ushort)((sps.pic_width_in_mbs_minus1 + 1) * 16 - 2 * sps.frame_crop_left_offset - 2 * sps.frame_crop_right_offset);
                    Height = (ushort)((sps.pic_height_in_map_units_minus1 + 1) * 16 - 2 * sps.frame_crop_top_offset - 2 * sps.frame_crop_bottom_offset);
                    this.SPS = sps_pps[0];
                    this.PPS = sps_pps[1];

                    initialized = true;
                }
            }
        }
    }
}
