using System;
using System.Net;
using System.Net.Sockets;

namespace GB28181.Net
{
    public partial class AudioPSAnalyze
    {
        private const int PS_HDR_LEN = 14;
        private const int SYS_HDR_LEN = 18;
        private const int PSM_HDR_LEN = 24;
        private const int PES_HDR_LEN = 19;
        private const int RTP_HDR_LEN = 12;
        private const byte RTP_VERSION = 2;
    //    private bool bHasPayload;//是否有有效数据
                                         //后加的常量  需要考虑值是否合理
        private const int PS_PES_PAYLOAD_SIZE = 5120;
        private const int RTP_MAX_PACKET_BUFF = 1400;

        /*** 
      *@remark:  音视频数据的打包成ps流，并封装成rtp 
      *@param :  pData      [in] 需要发送的音视频数据 
      *          nFrameLen  [in] 发送数据的长度 
      *          pPacker    [in] 数据包的一些信息，包括时间戳，rtp数据buff，发送的socket相关信息 
      *          stream_type[in] 数据类型 0 视频 1 音频 
      *@return:  0 success others failed 
      */

        public int Gb28181_streampackageForH264(byte[] pData, int nFrameLen, Data_Info_s pPacker, int stream_type)
        {
            try
            {

                byte[] szTempPacketHead = new byte[PS_HDR_LEN + SYS_HDR_LEN + PSM_HDR_LEN];
                int nSizePos = 0;
                int nSize = 0;
                byte[] pBuff = null;
                Array.Clear(szTempPacketHead, 0, szTempPacketHead.Length);

                // 1 package for ps header   
                Gb28181_make_ps_header(szTempPacketHead, nSizePos, pPacker.s64CurPts);



                nSizePos += PS_HDR_LEN;
                //2 system header   
                //if (pPacker.IFrame == 1)
                //{
                // 如果是I帧的话，则添加系统头 
                Gb28181_make_sys_header(szTempPacketHead, nSizePos);

                nSizePos += SYS_HDR_LEN;
                //这个地方我是不管是I帧还是p帧都加上了map的，貌似只是I帧加也没有问题  
                //      gb28181_make_psm_header(szTempPacketHead + nSizePos);  
                //      nSizePos += PSM_HDR_LEN;  
                //}
                // 3 psm头 (也是map)  
                Gb28181_make_psm_header(szTempPacketHead, nSizePos);

                nSizePos += PSM_HDR_LEN;

                //加上rtp发送出去，这样的话，后面的数据就只要分片分包就只有加上pes头和rtp头了  
                //if (gb28181_send_rtp_pack(szTempPacketHead, 0, nSizePos, 0, pPacker) != 0)
                //    return -1;

                // 这里向后移动是为了方便拷贝pes头  
                //这里是为了减少后面音视频裸数据的大量拷贝浪费空间，所以这里就向后移动，在实际处理的时候，要注意地址是否越界以及覆盖等问题  
                pBuff = new byte[szTempPacketHead.Length + pData.Length + PES_HDR_LEN];

                Array.Copy(szTempPacketHead, pBuff, szTempPacketHead.Length);

                Array.Copy(pData, 0, pBuff, szTempPacketHead.Length + PES_HDR_LEN, pData.Length);

                int startIndex = 0;



                //含有有效数据
              //  bHasPayload = true;
                while (nFrameLen > 0)
                {
                    //每次帧的长度不要超过short类型，过了就得分片进循环行发送  
                    nSize = (nFrameLen > PS_PES_PAYLOAD_SIZE) ? PS_PES_PAYLOAD_SIZE : nFrameLen;
                    // 添加pes头  
                    Gb28181_make_pes_header(pBuff, szTempPacketHead.Length, stream_type == 1 ? 0xC0 : 0xE0, nSize, (pPacker.s64CurPts / 1), (pPacker.s64CurPts / 3));


                    //最后在添加rtp头并发送数据  
                    if (Gb28181_send_rtp_pack(pBuff, startIndex, szTempPacketHead.Length + nSize + PES_HDR_LEN, ((nSize == nFrameLen) ? 1 : 0), pPacker) != 0)
                    {
                        Console.WriteLine("gb28181_send_pack failed!\n");
                        return -1;
                    }

                    //分片后每次发送的数据移动指针操作  
                    nFrameLen -= nSize;
                    //这里也只移动nSize,因为在while向后移动的pes头长度，正好重新填充pes头数据  
                    startIndex += nSize;
                }
              //  bHasPayload = false;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + ex.StackTrace);
            }
            return 0;
        }

        /*** 
*@remark:   rtp头的打包，并循环发送数据 
*@param :   pData      [in] 发送的数据地址 
*           nDatalen   [in] 发送数据的长度 
*           mark_flag  [in] mark标志位 
*           curpts     [in] 时间戳 
*           pPacker    [in] 数据包的基本信息 
*@return:   0 success, others failed 
*/
        private uint _timestamp = 320;
        int Gb28181_send_rtp_pack(byte[] databuff, int index, int nDataLen, int mark_flag, Data_Info_s pPacker)
        {
            try
            {
                int nRet = 0;
                int nPlayLoadLen = 0;
                int nSendSize = 0;

                byte[] szRtpHdr = new byte[RTP_HDR_LEN];
                Array.Clear(szRtpHdr, 0, RTP_HDR_LEN);

                if (nDataLen + RTP_HDR_LEN <= RTP_MAX_PACKET_BUFF)// 1460 pPacker指针本来有一个1460大小的buffer数据缓存  
                {
                    // 一帧数据发送完后，给mark标志位置1  
                    Gb28181_make_rtp_header(szRtpHdr, 0, ((mark_flag == 1) ? 1 : 0), ++pPacker.u16CSeq, (pPacker.s64CurPts / 1), pPacker.u32Ssrc);

                    Array.Copy(szRtpHdr, pPacker.szBuff, RTP_HDR_LEN);
                    Array.Copy(databuff, index, pPacker.szBuff, RTP_HDR_LEN, nDataLen);
                    nRet = SendDataBuff(pPacker.szBuff, 0, RTP_HDR_LEN + nDataLen, pPacker);//ZXB： 发送数据 为什么加databuff参数  需不需要加上index
                    if (nRet != (RTP_HDR_LEN + nDataLen))
                    {
                        Console.WriteLine(" udp send error !\n");
                        return -1;
                    }
                    //录制mf
                    //outputData(pPacker.szBuff, 0, RTP_HDR_LEN + nDataLen, bHasPayload, true, true);

                }
                else
                {
                    nPlayLoadLen = RTP_MAX_PACKET_BUFF - RTP_HDR_LEN; // 每次只能发送的数据长度 除去rtp头  
                    Gb28181_make_rtp_header(pPacker.szBuff, 0, 0, ++pPacker.u16CSeq, (pPacker.s64CurPts / 1), pPacker.u32Ssrc);

                    //memcpy(pPacker->szBuff + RTP_HDR_LEN, databuff, nPlayLoadLen);
                    Array.Copy(databuff, index, pPacker.szBuff, RTP_HDR_LEN, nPlayLoadLen);

                    nRet = SendDataBuff(pPacker.szBuff, 0, RTP_HDR_LEN + nPlayLoadLen, pPacker);//ZXB： 发送数据 为什么加databuff参数需不需要加上index
                    if (nRet != (RTP_HDR_LEN + nPlayLoadLen))
                    {
                        Console.WriteLine(" udp send error !\n");
                        return -1;
                    }
                    //录制mf
                    //outputData(pPacker.szBuff, 0, RTP_HDR_LEN + nPlayLoadLen, bHasPayload, true, true);

                    nDataLen -= nPlayLoadLen;
                    index += nPlayLoadLen; // 表明前面到数据已经发送出去        
                    index -= RTP_HDR_LEN; // 用来存放rtp头  
                    while (nDataLen > 0)
                    {
                        if (nDataLen <= nPlayLoadLen)
                        {
                            //一帧数据发送完，置mark标志位  
                            Gb28181_make_rtp_header(databuff, index, mark_flag, ++pPacker.u16CSeq, (pPacker.s64CurPts / 1), pPacker.u32Ssrc);
                            nSendSize = nDataLen;
                        }
                        else
                        {
                            Gb28181_make_rtp_header(databuff, index, 0, ++pPacker.u16CSeq, (pPacker.s64CurPts / 1), pPacker.u32Ssrc);
                            nSendSize = nPlayLoadLen;
                        }
                        nRet = SendDataBuff(databuff, index, RTP_HDR_LEN + nSendSize, pPacker);//ZXB： 发送数据 为什么加databuff参数需不需要加上index
                        if (nRet != (RTP_HDR_LEN + nSendSize))
                        {
                            Console.WriteLine(" udp send error !\n");
                            return -1;
                        }
                        //录制mf
                        //outputData(databuff, index, RTP_HDR_LEN + nSendSize, bHasPayload, true, false);

                        nDataLen -= nSendSize;
                        index += nSendSize;
                        //因为buffer指针已经向后移动一次rtp头长度后，  
                        //所以每次循环发送rtp包时，只要向前移动裸数据到长度即可，这是buffer指针实际指向到位置是  
                        //databuff向后重复的rtp长度的裸数据到位置上   
                    }

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + ex.StackTrace);
            }

            return 0;
        }

        int SendDataBuff(byte[] data, int startIndex, int size, Data_Info_s pPacker)
        {
            try
            {
                int sendedCount = 0;
                if (pPacker != null && pPacker.sendSOcket != null)
                {
                    //var start = DateTime.Now;
                    sendedCount = pPacker.sendSOcket.SendTo(data, startIndex, size, SocketFlags.None, pPacker.remotePoint);
                    //if (sendedCount != 12)
                    //    Console.WriteLine(sendedCount);
                    //Thread.Sleep(40);
                    //Thread.Sleep(40);
                    //var end = DateTime.Now.Subtract(start).Milliseconds;
                    //Console.WriteLine("send audio to soldier  " + end);
                }
                return sendedCount;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + ex.StackTrace);
                return 0;
            }
        }
       // int seqNo = 0;
       // int timestamp = 0;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pData"></param>
        /// <param name="marker_flag"></param>
        /// <param name="cseq"></param>
        /// <param name="curpts"></param>
        /// <param name="ssrc"></param>
        /// <returns></returns>
        private int Gb28181_make_rtp_header(byte[] pData, int startIndex, int marker_flag, ushort cseq, long curpts, uint ssrc)
        {
            try
            {
                //BitStream bitsBuffer = new BitStream(RTP_HDR_LEN);
                //if (pData == null)
                //    return -1;
                //bitsBuffer.Write(2, 0, 2); /* rtp version   版本2	*/
                //bitsBuffer.Write(0, 0, 1);  /* rtp padding 	*/
                //bitsBuffer.Write(0, 0, 1); /* rtp extension 	*/
                //bitsBuffer.Write(0, 0, 4);        /* rtp CSRC count */
                //bitsBuffer.Write(marker_flag, 0, 1);/* rtp marker  	*/
                //bitsBuffer.Write(8, 0, 7);         /* rtp payload type*/
                //bitsBuffer.Write(cseq, 0, 16);            /* rtp sequence 	 */
                //bitsBuffer.Write(_timestamp, 0, 32);      /* rtp timestamp 	 */
                //bitsBuffer.Write(ssrc, 0, 32);        /* rtp SSRC	 	 */
               // Array.Copy(bitsBuffer.ToByteArray(), 0, pData, startIndex, RTP_HDR_LEN);

                _timestamp += 320;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + ex.StackTrace);
            }

            return 0;
        }

        /*** 
       *@remark:   ps头的封装,里面的具体数据的填写已经占位，可以参考标准 
       *@param :   pData  [in] 填充ps头数据的地址 
       *           s64Src [in] 时间戳 
       *@return:   0 success, others failed 
       */
        private int Gb28181_make_ps_header(byte[] pData, int startIndex, ulong s64Scr)
        {
            try
            {
                ulong lScrExt = (s64Scr) % 100;
                //s64Scr = s64Scr / 100;
                // 这里除以100是由于sdp协议返回的video的频率是90000，帧率是25帧/s，所以每次递增的量是3600,  
                // 所以实际你应该根据你自己编码里的时间戳来处理以保证时间戳的增量为3600即可，  
                //如果这里不对的话，就可能导致卡顿现象了  
                //bits_buffer_s bitsBuffer;
                //bitsBuffer.i_size = PS_HDR_LEN;
                //bitsBuffer.i_data = 0;
                //bitsBuffer.i_mask = 0x80; // 二进制：10000000 这里是为了后面对一个字节的每一位进行操作，避免大小端夸字节字序错乱  
                //bitsBuffer.p_data = (unsigned char*)(pData);
                //memset(bitsBuffer.p_data, 0, PS_HDR_LEN);
                //BitStream bitsBuffer = new BitStream(PS_HDR_LEN);
                //bitsBuffer.Write(0x000001BA, 0, 32);//bits_write(&bitsBuffer, 32, 0x000001BA);            /*start codes*/
                //bitsBuffer.Write(1, 0, 2);//bits_write(&bitsBuffer, 2, 1);                     /*marker bits '01b'*/
                //bitsBuffer.Write((s64Scr >> 30) & 0x07, 0, 3);//bits_write(&bitsBuffer, 3, (s64Scr >> 30) & 0x07);     /*System clock [32..30]*/
                //bitsBuffer.Write(1, 0, 1);//bits_write(&bitsBuffer, 1, 1);                     /*marker bit*/
                //bitsBuffer.Write((s64Scr >> 15) & 0x7FFF, 0, 15);//bits_write(&bitsBuffer, 15, (s64Scr >> 15) & 0x7FFF);   /*System clock [29..15]*/
                //bitsBuffer.Write(1, 0, 1);//bits_write(&bitsBuffer, 1, 1);                     /*marker bit*/
                //bitsBuffer.Write(s64Scr & 0x7fff, 0, 15);//bits_write(&bitsBuffer, 15, s64Scr & 0x7fff);         /*System clock [29..15]*/
                //bitsBuffer.Write(1, 0, 1);//bits_write(&bitsBuffer, 1, 1);                     /*marker bit*/
                //bitsBuffer.Write(lScrExt & 0x01ff, 0, 9);//bits_write(&bitsBuffer, 9, lScrExt & 0x01ff);        /*System clock [14..0]*/
                //bitsBuffer.Write(1, 0, 1);//bits_write(&bitsBuffer, 1, 1);                     /*marker bit*/
                //bitsBuffer.Write((255) & 0x3fffff, 0, 22);//bits_write(&bitsBuffer, 22, (255) & 0x3fffff);        /*bit rate(n units of 50 bytes per second.)*/
                //bitsBuffer.Write(3, 0, 2);//bits_write(&bitsBuffer, 2, 3);                     /*marker bits '11'*/
                //bitsBuffer.Write(0x1f, 0, 5);//bits_write(&bitsBuffer, 5, 0x1f);                  /*reserved(reserved for future use)*/
                //bitsBuffer.Write(0, 0, 3);//bits_write(&bitsBuffer, 3, 0);                     /*stuffing length*/

                //Array.Copy(bitsBuffer.ToByteArray(), 0, pData, startIndex, PS_HDR_LEN);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + ex.StackTrace);
            }

            return 0;
        }

        /*** 
*@remark:   sys头的封装,里面的具体数据的填写已经占位，可以参考标准 
*@param :   pData  [in] 填充ps头数据的地址 
*@return:   0 success, others failed 
*/
        private int Gb28181_make_sys_header(byte[] pData, int startIndex)
        {
            try
            {
                //bits_buffer_s bitsBuffer;
                //bitsBuffer.i_size = SYS_HDR_LEN;
                //bitsBuffer.i_data = 0;
                //bitsBuffer.i_mask = 0x80;
                //bitsBuffer.p_data = (unsigned char*)(pData);
                //memset(bitsBuffer.p_data, 0, SYS_HDR_LEN);
                /*system header*/
                //BitStream bitsBuffer = new BitStream(SYS_HDR_LEN);
                //bitsBuffer.Write(0x000001BB, 0, 32);//bits_write(&bitsBuffer, 32, 0x000001BB);   /*start code*/
                //bitsBuffer.Write(SYS_HDR_LEN - 6, 0, 16);//bits_write(&bitsBuffer, 16, SYS_HDR_LEN - 6);/*header_length 表示次字节后面的长度，后面的相关头也是次意思*/
                //bitsBuffer.Write(1, 0, 1);//bits_write(&bitsBuffer, 1, 1);            /*marker_bit*/
                //bitsBuffer.Write(50000, 0, 22);//bits_write(&bitsBuffer, 22, 50000);        /*rate_bound*/
                //bitsBuffer.Write(1, 0, 1);//bits_write(&bitsBuffer, 1, 1);            /*marker_bit*/
                //bitsBuffer.Write(1, 0, 6);//bits_write(&bitsBuffer, 6, 1);            /*audio_bound*/
                //bitsBuffer.Write(0, 0, 1);//bits_write(&bitsBuffer, 1, 0);            /*fixed_flag */
                //bitsBuffer.Write(1, 0, 1);//bits_write(&bitsBuffer, 1, 1);            /*CSPS_flag */
                //bitsBuffer.Write(1, 0, 1);//bits_write(&bitsBuffer, 1, 1);            /*system_audio_lock_flag*/
                //bitsBuffer.Write(1, 0, 1);//bits_write(&bitsBuffer, 1, 1);            /*system_video_lock_flag*/
                //bitsBuffer.Write(1, 0, 1);//bits_write(&bitsBuffer, 1, 1);            /*marker_bit*/
                //bitsBuffer.Write(1, 0, 5);//bits_write(&bitsBuffer, 5, 1);            /*video_bound*/
                //bitsBuffer.Write(0, 0, 1);//bits_write(&bitsBuffer, 1, 0);            /*dif from mpeg1*/
                //bitsBuffer.Write(0x7F, 0, 7);//bits_write(&bitsBuffer, 7, 0x7F);         /*reserver*/
                //                             /*audio stream bound*/
                //bitsBuffer.Write(0xC0, 0, 8);//bits_write(&bitsBuffer, 8, 0xC0);         /*stream_id*/
                //bitsBuffer.Write(3, 0, 2);//bits_write(&bitsBuffer, 2, 3);            /*marker_bit */
                //bitsBuffer.Write(0, 0, 1);//bits_write(&bitsBuffer, 1, 0);            /*PSTD_buffer_bound_scale*/
                //bitsBuffer.Write(512, 0, 13);//bits_write(&bitsBuffer, 13, 512);          /*PSTD_buffer_size_bound*/
                //                             /*video stream bound*/
                //bitsBuffer.Write(0xE0, 0, 8);//bits_write(&bitsBuffer, 8, 0xE0);         /*stream_id*/
                //bitsBuffer.Write(3, 0, 2);//bits_write(&bitsBuffer, 2, 3);            /*marker_bit */
                //bitsBuffer.Write(1, 0, 1);//bits_write(&bitsBuffer, 1, 1);            /*PSTD_buffer_bound_scale*/
                //bitsBuffer.Write(2048, 0, 13);//bits_write(&bitsBuffer, 13, 2048);         /*PSTD_buffer_size_bound*/

                //Array.Copy(bitsBuffer.ToByteArray(), 0, pData, startIndex, SYS_HDR_LEN);

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + ex.StackTrace);
            }

            return 0;
        }

        //       /***
        // *@remark:   psm头的封装,里面的具体数据的填写已经占位，可以参考标准
        // *@param :   pData  [in] 填充ps头数据的地址
        // *@return:   0 success, others failed
        //*/
        private int Gb28181_make_psm_header(byte[] pData, int startIndex)
        {
            try
            {
                //BitStream bitsBuffer = new BitStream(PSM_HDR_LEN);

                ////bitsBuffer.i_size = PSM_HDR_LEN;
                ////bitsBuffer.i_data = 0;
                ////bitsBuffer.i_mask = 0x80;
                ////bitsBuffer.p_data = (unsigned char*)(pData);
                //// memset(bitsBuffer.p_data, 0, PS_SYS_MAP_SIZE);
                //bitsBuffer.Write(0x000001, 0, 24);  /*start code*/
                //bitsBuffer.Write(0xBC, 0, 8);       /*map stream id*/
                //bitsBuffer.Write(18, 0, 16);        /*program stream map length*/
                //bitsBuffer.Write(1, 0, 1);          /*current next indicator */
                //bitsBuffer.Write(3, 0, 2);          /*reserved*/
                //bitsBuffer.Write(0, 0, 5);          /*program stream map version*/
                //bitsBuffer.Write(0x7F, 0, 7);       /*reserved */
                //bitsBuffer.Write(1, 0, 1);          /*marker bit */
                //bitsBuffer.Write(0, 0, 16);          /*programe stream info length*/
                //bitsBuffer.Write(8, 0, 16);         /*elementary stream map length	is*/

                ///*video*/
                //bitsBuffer.Write(0x1B, 0, 8);       /*stream_type*/
                //bitsBuffer.Write(0xE0, 0, 8);       /*elementary_stream_id*/
                //bitsBuffer.Write(0, 0, 16);         /*elementary_stream_info_length */

                ///*audio*/
                //bitsBuffer.Write(0x90, 0, 8);       /*stream_type*/
                //bitsBuffer.Write(0xC0, 0, 8);       /*elementary_stream_id*/
                //bitsBuffer.Write(0, 0, 16);         /*elementary_stream_info_length is*/

                ///*crc (2e b9 0f 3d)*/
                //bitsBuffer.Write(0x8C, 0, 8);       /*crc (24~31) bits*/
                //bitsBuffer.Write(0x2E, 0, 8);       /*crc (16~23) bits*/
                //bitsBuffer.Write(0xE6, 0, 8);       /*crc (8~15) bits*/
                //bitsBuffer.Write(0xD9, 0, 8);        /*crc (0~7) bits*/

                //Array.Copy(bitsBuffer.ToByteArray(), 0, pData, startIndex, PSM_HDR_LEN);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + ex.StackTrace);
            }


            return 0;
        }

        /***
      *@remark:   pes头的封装,里面的具体数据的填写已经占位，可以参考标准
      *@param :   pData      [in] 填充ps头数据的地址
      *           stream_id  [in] 码流类型
      *           paylaod_len[in] 负载长度
      *           pts        [in] 时间戳
      *           dts        [in]
      *@return:   0 success, others failed
     */
        private int Gb28181_make_pes_header(byte[] pData, int startIndex, int stream_id, int payload_len, ulong pts, ulong dts)
        {
            try
            {
                //BitStream bitsBuffer = new BitStream(PES_HDR_LEN);

                ////bits_buffer_s bitsBuffer;
                ////bitsBuffer.i_size = PES_HDR_LEN;
                ////bitsBuffer.i_data = 0;
                ////bitsBuffer.i_mask = 0x80;
                ////bitsBuffer.p_data = (unsigned char*)(pData);
                ////memset(bitsBuffer.p_data, 0, PES_HDR_LEN);
                ///*system header*/
                //bitsBuffer.Write(0x000001, 0, 24);  /*start code*/
                //bitsBuffer.Write(stream_id, 0, 8);    /*streamID*/
                //bitsBuffer.Write((payload_len) + 13, 0, 16);    /*packet_len*/ //指出pes分组中数据长度和该字节后的长度和

                //bitsBuffer.Write(2, 0, 2);//   bits_write(&bitsBuffer, 2, 2);      /*'10'*/
                //bitsBuffer.Write(0, 0, 2); // bits_write(&bitsBuffer, 2, 0);      /*scrambling_control*/
                //bitsBuffer.Write(0, 0, 1);//  bits_write(&bitsBuffer, 1, 0);      /*priority*/
                //bitsBuffer.Write(0, 0, 1); //bits_write(&bitsBuffer, 1, 0);      /*data_alignment_indicator*/
                //bitsBuffer.Write(0, 0, 1); //bits_write(&bitsBuffer, 1, 0);      /*copyright*/
                //bitsBuffer.Write(0, 0, 1); //bits_write(&bitsBuffer, 1, 0);      /*original_or_copy*/
                //bitsBuffer.Write(1, 0, 1); //bits_write(&bitsBuffer, 1, 1);      /*PTS_flag*/
                //bitsBuffer.Write(1, 0, 1); //bits_write(&bitsBuffer, 1, 1);      /*DTS_flag*/
                //bitsBuffer.Write(0, 0, 1);//bits_write(&bitsBuffer, 1, 0);      /*ESCR_flag*/
                //bitsBuffer.Write(0, 0, 1);//bits_write(&bitsBuffer, 1, 0);      /*ES_rate_flag*/
                //bitsBuffer.Write(0, 0, 1);//bits_write(&bitsBuffer, 1, 0);      /*DSM_trick_mode_flag*/
                //bitsBuffer.Write(0, 0, 1);//bits_write(&bitsBuffer, 1, 0);      /*additional_copy_info_flag*/
                //bitsBuffer.Write(0, 0, 1);//bits_write(&bitsBuffer, 1, 0);      /*PES_CRC_flag*/
                //bitsBuffer.Write(0, 0, 1);//bits_write(&bitsBuffer, 1, 0);      /*PES_extension_flag*/
                //bitsBuffer.Write(10, 0, 8);//bits_write(&bitsBuffer, 8, 10);     /*header_data_length*/

                //// 指出包含在 PES 分组标题中的可选字段和任何填充字节所占用的总字节数。该字段之前
                ////的字节指出了有无可选字段。

                ///*PTS,DTS*/
                //bitsBuffer.Write(3, 0, 4);//bits_write(&bitsBuffer, 4, 3);                    /*'0011'*/
                //bitsBuffer.Write(((pts) >> 30) & 0x07, 0, 3);//bits_write(&bitsBuffer, 3, ((pts) >> 30) & 0x07);     /*PTS[32..30]*/
                //bitsBuffer.Write(1, 0, 1);//bits_write(&bitsBuffer, 1, 1);
                //bitsBuffer.Write(((pts) >> 15) & 0x7FFF, 0, 15);//bits_write(&bitsBuffer, 15, ((pts) >> 15) & 0x7FFF);    /*PTS[29..15]*/
                //bitsBuffer.Write(1, 0, 1);//bits_write(&bitsBuffer, 1, 1);
                //bitsBuffer.Write((pts) & 0x7FFF, 0, 15);//bits_write(&bitsBuffer, 15, (pts) & 0x7FFF);          /*PTS[14..0]*/
                //bitsBuffer.Write(1, 0, 1);//bits_write(&bitsBuffer, 1, 1);
                //bitsBuffer.Write(1, 0, 4);//bits_write(&bitsBuffer, 4, 1);                    /*'0001'*/
                //bitsBuffer.Write(((dts) >> 30) & 0x07, 0, 3);//bits_write(&bitsBuffer, 3, ((dts) >> 30) & 0x07);     /*DTS[32..30]*/
                //bitsBuffer.Write(1, 0, 1);//bits_write(&bitsBuffer, 1, 1);
                //bitsBuffer.Write(((dts) >> 15) & 0x7FFF, 0, 15);//bits_write(&bitsBuffer, 15, ((dts) >> 15) & 0x7FFF);    /*DTS[29..15]*/
                //bitsBuffer.Write(1, 0, 1);//bits_write(&bitsBuffer, 1, 1);
                //bitsBuffer.Write((dts) & 0x7FFF, 0, 15);//bits_write(&bitsBuffer, 15, (dts) & 0x7FFF);          /*DTS[14..0]*/
                //bitsBuffer.Write(1, 0, 1);//bits_write(&bitsBuffer, 1, 1);

                //Array.Copy(bitsBuffer.ToByteArray(), 0, pData, startIndex, PES_HDR_LEN);

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + ex.StackTrace);
            }

            return 0;
        }
    }
    public class Data_Info_s
    {
        public UInt16 u16CSeq = 0;
        public UInt32 s64CurPts = 0;
        public UInt32 u32Ssrc = 0;

        public byte[] szBuff = new byte[1400];
        public byte IFrame;

        public Socket sendSOcket = null;
        public EndPoint remotePoint = null;

    }
}
