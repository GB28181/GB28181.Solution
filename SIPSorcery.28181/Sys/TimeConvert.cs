using System;

namespace SIPSorcery.GB28181.Sys
{
    /// <summary>
    /// 日期/时间戳转换
    /// </summary>
    public static class TimeConvert
    {
        /// <summary>
        /// 日期类型转换为时间戳
        /// 返回自1970年以来的秒数
        /// </summary>
        /// <param name="time">时间</param>
        /// <returns></returns>
        public static uint DateToTimeStamp(DateTime date)
        {
            //   DateTime startTime = TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1));

            return (uint)TimeZoneInfo.Local.GetUtcOffset(date).Seconds;

        }

        /// <summary>
        /// 时间戳转换为日期类型
        /// 返回自1970以来的时间
        /// </summary>
        /// <param name="timestamp">时间戳(1147763686)</param>
        /// <returns></returns>
        public static DateTime TimeStampToDate(uint timestamp)
        {
            return TimeZoneInfo.ConvertTimeToUtc(DateTime.Now.AddSeconds(timestamp));
        }
    }
}
