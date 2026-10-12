using System;
using System.Collections.Generic;
using UnityEngine;

namespace VzDev.ApiExtensions
{

    public static class DatetimeExtension
    {
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