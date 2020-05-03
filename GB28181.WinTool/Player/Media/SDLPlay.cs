using Helpers;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Tao.Sdl;


namespace GB28181.WinTool.Media
{
    public class SDLPlay
    {
        private Sdl.AudioSpecCallbackDelegate audioCallBack = null;
 
        private Queue<byte[]> audioQueue = new Queue<byte[]>();
        private byte[] lastAudioData = new byte[0];
        private int lastAudioPoint = 0;
        private int audioDataSize = 0;
 
        private bool isFirstAudioCallBack = true;
        static SDLPlay()
        {
            Sdl.SDL_Init(0x0010 | 0x0020 | 0x0021);
        }
        public void InitAudio(int freq, int samples, int channels)
        {
            audioCallBack = new Sdl.AudioSpecCallbackDelegate(AudioCallBack);
             
            Sdl.SDL_AudioSpec s1 = new Sdl.SDL_AudioSpec()
            {
                freq = freq,
                samples = (short)samples,
                channels = (byte)channels,
                format = 0x0010,
                callback = Marshal.GetFunctionPointerForDelegate(audioCallBack),
            };

            Sdl.SDL_AudioSpec s2 = new Sdl.SDL_AudioSpec();
            var p1 = FunctionEx.StructToIntPtr(s1);
            var p2 = FunctionEx.StructToIntPtr(s2);
            var cs=Sdl.SDL_OpenAudio(p1, p2);
            
        }
        public void AudioPlay()
        {
            Sdl.SDL_PauseAudio(0);
        }
        public void AudioPause()
        {
            Sdl.SDL_PauseAudio(1);
          
        }
        public void CloseAudio() {
            Sdl.SDL_PauseAudio(1);
            Sdl.SDL_CloseAudio();
        }
        public void AudioPlay(byte[] data)
        {
            lock (audioQueue)
            {
                audioQueue.Enqueue(data);
                audioDataSize += data.Length;
            }
        }
        private void AudioCallBack(IntPtr userdata, IntPtr stream, int len)
        {
            if (isFirstAudioCallBack)
            {
                lock (audioQueue)
                {
                    audioQueue.Clear();
                    audioDataSize = 0;
                }
                isFirstAudioCallBack = false;
            }
            Console.WriteLine(audioDataSize);
            if (audioDataSize + (lastAudioData.Length - lastAudioPoint) < len) 
                return;
            byte[] buff = new byte[len];
            int read = 0;
            while (read < len)
            {
                int need = len - read;
                if (lastAudioData == null || lastAudioData.Length == 0)
                {
                    lock (audioQueue)
                    {
                        lastAudioData = audioQueue.Dequeue();
                        audioDataSize -= lastAudioData.Length;
                        lastAudioPoint = 0;
                    }
                }
                //当前数组已包含所需字段数
                if (lastAudioData.Length - lastAudioPoint >= need)
                {
                    Array.Copy(lastAudioData, lastAudioPoint, buff, read, need);
                    lastAudioPoint += need;
                    read += need;
                    if (lastAudioPoint == lastAudioData.Length)
                    {
                        lastAudioData = new byte[0]; lastAudioPoint = 0;
                    }
                }
                else
                {
                    Array.Copy(lastAudioData, lastAudioPoint, buff, read, lastAudioData.Length - lastAudioPoint);
                    read += lastAudioData.Length - lastAudioPoint;
                    lastAudioData = new byte[0];
                    lastAudioPoint = 0;
                }

            }
            //var pBuff = FunctionEx.BytesToIntPtr(buff);

            //var result = Marshal.AllocHGlobal(buff.Length);
            //Marshal.Copy(buff, 0, result, buff.Length);
            Marshal.Copy(buff, 0, stream, buff.Length);
            //SDL.SDL_MixAudio(stream, pBuff, len,100);
            return;
        }
    }
}
