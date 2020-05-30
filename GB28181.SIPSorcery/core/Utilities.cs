//-----------------------------------------------------------------------------
// Filename: Utilities.cs
//
// Description: Useful functions for GB28181 implementation.
//
// History:
// 30 May 2020	Edward	Chen    Updated.
// License: 
// This software is licensed under the BSD License http://www.opensource.org/licenses/bsd-license.php


using System;

namespace GB28181
{
    public static class CallHelpers
    {
        /// <summary>
        /// Create a new CSeq
        /// </summary>
        /// <returns></returns>
        public static int CreateNewCSeq()
        {
            var r = new Random();
            return r.Next(1, ushort.MaxValue);
        }

    }
}
