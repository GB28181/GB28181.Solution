namespace Win.MediaServer.Media.TS.Rtsp
{
    using System;

    public delegate void StreamDataCallBack(int nPort, IntPtr pBuffer, uint dataLen, uint dataType, int nUser);
    public delegate int fCallBack(IntPtr param, IntPtr data, uint dataLen, string mimeType, int isKeyFrame, uint pts);

}

