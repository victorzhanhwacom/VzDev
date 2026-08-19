using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;

namespace VzDev.DateTimeUtils
{
    public enum EnumTimeFormat
    {
        時分秒_12小時制, 時分秒_24小時制, 西元年月日, 星期, 星期_縮寫, 完整年月日時分秒_12小時制, 完整年月日時分秒_24小時制
    }

    public enum EnumTime
    {
        時, 分, 秒
    }

    /// <summary>
    /// 常用日期時間顯示格式。對應到 .NET DateTime.ToString() 的格式字串。
    /// 涵蓋不到的需求可選 Custom，改用 customFormat 欄位自行輸入。
    /// </summary>
    public enum DateTimeFormatType
    {
        [InspectorName("月-日 (08-19)")] Date_MMdd,
        [InspectorName("月-日 星期 (08-19 Wed)")] Date_MMdd_ddd,
        [InspectorName("年-月-日 (2026-08-19)")] Date_yyyyMMdd,
        [InspectorName("年-月-日 (2026-08-19)")] Date_yyyyMMdd_Dash,
        [InspectorName("年-月 (2026-08)")] Date_yyyyMM,
        [InspectorName("月 日 長格式 (August 19)")] Date_MMMM_dd,
        [InspectorName("時:分 (14:30)")] Time_HHmm,
        [InspectorName("時:分:秒 (14:30:05)")] Time_HHmmss,
        [InspectorName("年-月-日 時:分")] DateTime_yyyyMMdd_HHmm,
        [InspectorName("月-日 星期 時:分")] DateTime_MMdd_ddd_HHmm,
        [InspectorName("完整：年-月-日 星期 時:分:秒")] DateTime_Full,
        [InspectorName("星期 簡稱 (Wed)")] Weekday_Short,
        [InspectorName("星期 全稱 (Wednesday)")] Weekday_Long,
    }

    public static class DateTimeFormatTypeExtensions
    {
        private static readonly Dictionary<DateTimeFormatType, string> Map = new()
        {
            { DateTimeFormatType.Date_MMdd,               "MM/dd" },
            { DateTimeFormatType.Date_MMdd_ddd,           "MM/dd ddd" },
            { DateTimeFormatType.Date_yyyyMMdd,           "yyyy/MM/dd" },
            { DateTimeFormatType.Date_yyyyMMdd_Dash,      "yyyy-MM-dd" },
            { DateTimeFormatType.Date_yyyyMM,             "yyyy/MM" },
            { DateTimeFormatType.Date_MMMM_dd,            "MMMM dd" },
            { DateTimeFormatType.Time_HHmm,               "HH:mm" },
            { DateTimeFormatType.Time_HHmmss,             "HH:mm:ss" },
            { DateTimeFormatType.DateTime_yyyyMMdd_HHmm,  "yyyy/MM/dd HH:mm" },
            { DateTimeFormatType.DateTime_MMdd_ddd_HHmm,  "MM/dd ddd HH:mm" },
            { DateTimeFormatType.DateTime_Full,           "yyyy/MM/dd ddd HH:mm:ss" },
            { DateTimeFormatType.Weekday_Short,           "ddd" },
            { DateTimeFormatType.Weekday_Long,            "dddd" },
        };

        /// <summary>
        /// 取得對應的 .NET DateTime 格式字串。Custom 一律回傳 null，
        /// 呼叫端須自行改用 customFormat 欄位。
        /// </summary>
        public static string ToFormatString(this DateTimeFormatType type)
            => Map.TryGetValue(type, out var f) ? f : null;
    }
}
