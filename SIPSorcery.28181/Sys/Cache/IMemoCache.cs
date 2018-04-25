using System;
using System.Collections.Generic;
using System.Text;

namespace SIPSorcery.GB28181.Sys.Cache
{
    /// <summary>
    /// cache device object in memory 
    /// such as camera、sensor、detector
    /// </summary>
    public interface IMemoCache<T>
    {
        /// <summary>
        /// string Identity 
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// put some data into 
        /// </summary>
        /// <param name="deviceKey"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        string PlaceIn(string deviceKey, T value);

        /// <summary>
        /// retrive objectfrom cache
        /// </summary>
        /// <param name="deviceKey"></param>
        /// <returns></returns>
        T FetchOut(string deviceKey);



    }
}
