using System;
using System.Collections.Generic;
using UnityEngine;

namespace VzDev.ApiExtensions
{
    public static class DatetimeExtension
    {
        #region 轉換成整點/每半小時
        /// <summary>
        /// 取得時間的整點小時，並將分鐘數歸零
        /// </summary>
        public static DateTime RoundedToHour(this DateTime dateTime)
            => new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, 0, 0);
        /// <summary>
        /// 取得時間的整分鐘，並將秒數歸零
        /// </summary>
        public static DateTime RoundedToMinute(this DateTime dateTime, bool isHaveHour = false)
        {
            int minute = isHaveHour ? dateTime.Minute < 30 ? 0 : 30 : dateTime.Minute;
            return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, minute, 0);
        }
        /// <summary>距離下一個整點的剩餘時間。</summary>
        public static TimeSpan TimeUntilNextHour(this DateTime t)
            => t.NextHour() - t;
        /// <summary>下一個整點。例：14:37:12 → 15:00:00</summary>
        public static DateTime NextHour(this DateTime t)
            => t.RoundedToHour().AddHours(1);
        #endregion

        /// <summary>
        /// 將DateTime轉換為UTC格式的字串 (yyyy-MM-ddTHH:mm:ssZ)
        /// </summary>
        public static string ToUTC_String(this DateTime dateTime) => dateTime.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");

        /// <summary>
        /// 將UTC格式的DateTime字串轉換為本地DateTime
        /// </summary>
        public static DateTime FromUTC_String(this string utcDatetimeString)
        {
            if (DateTime.TryParse(utcDatetimeString, out DateTime utcDateTime))
            {
                return utcDateTime.ToLocalTime();
            }
            else
            {
                Debug.LogWarning($"無法解析UTC字串: {utcDatetimeString}");
                return DateTime.MinValue; // 或者其他適當的預設值
            }
        }
    }
}