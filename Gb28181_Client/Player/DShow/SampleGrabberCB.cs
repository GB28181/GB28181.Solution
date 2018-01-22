using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using DirectShowLib;

namespace SLW.WPFClient.DShow
{
    //directshow 数据回调
    [System.Security.SuppressUnmanagedCodeSecurity]
    public class SampleGrabberCB : ISampleGrabberCB
    {
        Action<double, IMediaSample> _fSampleCB = null;
        Action<double, IntPtr, int> _fBufferCB = null;
        public SampleGrabberCB(Action<double, IMediaSample> sampleCB)
        {
            _fSampleCB = sampleCB;
        }
        public SampleGrabberCB(Action<double, IntPtr, int> bufferCB)
        {
            _fBufferCB = bufferCB;
        }

        [PreserveSig]
        int ISampleGrabberCB.SampleCB(double SampleTime, IMediaSample pSample)
        {
            int result = 0;
            if (_fSampleCB != null)
                _fSampleCB(SampleTime, pSample);

            IntPtr pBuff = IntPtr.Zero;
            var size = pSample.GetActualDataLength();
            pSample.GetPointer(out pBuff);
            if (_fBufferCB != null)
                _fBufferCB(SampleTime, pBuff, size);

            Marshal.FinalReleaseComObject(pSample);
            return result;
        }
        [PreserveSig]
        int ISampleGrabberCB.BufferCB(double SampleTime, IntPtr pBuffer, int BufferLen)
        {
            if (_fBufferCB != null)
                _fBufferCB(SampleTime, pBuffer, BufferLen);
            return 0;
        }
    }
}
