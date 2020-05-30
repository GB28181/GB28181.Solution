//-----------------------------------------------------------------------------
// Filename: Utilities.cs
//
// Description: Useful functions for GB28181 implementation.
//
// History:
// 30 May 2020	Edward	Chen    Updated.
// License: 
// BSD 3-Clause "New" or "Revised" License, see included LICENSE.md file.


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
