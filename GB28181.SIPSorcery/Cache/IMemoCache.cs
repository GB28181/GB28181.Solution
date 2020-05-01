using System;

namespace GB28181.Cache
{
    /// <summary>
    /// cache device object in memory 
    /// such as camera、sensor、detector
    /// </summary>
    public interface IMemoCache<T>
    {
        /// <summary>
        /// triggered when Item Placed in
        /// </summary>
        event Action<object, T> OnItemAdded;

        /// <summary>
        /// triggered when Item Removed out
        /// </summary>
        event Action<object, T> OnItemRemoved;

        /// <summary>
        /// string Identity 
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// put data into it
        /// </summary>
        /// <param name="deviceKey"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        T PlaceIn(string deviceKey, T value);

        /// <summary>
        /// retrive objectfrom cache
        /// </summary>
        /// <param name="deviceKey"></param>
        /// <returns></returns>
        T FindOut(string deviceKey);


        /// <summary>
        /// Remove some item
        /// </summary>
        /// <param name="deviceKey"></param>
        /// <returns></returns>
        bool Remove(string deviceKey);

        /// <summary>
        /// Empty all Data
        /// </summary>
        /// <returns></returns>
        void Clear();

        /// <summary>
        /// retrive objectfrom and remove it from cache
        /// </summary>
        /// <param name="deviceKey"></param>
        /// <returns></returns>
        T TakeOut(string deviceKey);


    }
}
