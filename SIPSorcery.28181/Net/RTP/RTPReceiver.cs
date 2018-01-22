//-----------------------------------------------------------------------------
// Filename: RTPReceiver.cs
//
// Description: Accepts RTP packets and does something useful with them.
// 
// History:
// 14 Feb 2006	Aaron Clauson	Created.
//
// License: 
// Aaron Clauson
//-----------------------------------------------------------------------------

using System;
using System.Net;

#if UNITTEST
using NUnit.Framework;
#endif

namespace SIPSorcery.GB28181.Net
{
    //public class RTPReceiver
    //{
    //    public RTPSink RTPDestSink = null;
    //    public IPEndPoint RTPDestEndPoint = null;
		
    //    public RTPReceiver(RTPSink rtpSink, IPEndPoint dstEndPoint)
    //    {
    //        RTPDestSink = rtpSink;
    //        RTPDestEndPoint = dstEndPoint;
    //    }

    //    public void Receive(byte[] buffer)
    //    {
    //        if(RTPDestSink != null && RTPDestEndPoint != null)
    //        {
    //            RTPDestSink.Send(RTPDestEndPoint, buffer);
    //        }
    //    }
    //}
}
