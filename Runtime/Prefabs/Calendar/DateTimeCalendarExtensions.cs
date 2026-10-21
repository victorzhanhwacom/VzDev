using System;

namespace VzDev.ToolUtils.Calendar
{
    /// <summary>
    /// 日曆計算用的 DateTime extension。純函式、無 GC，可直接搬到其他專案使用。
    /// </summary>
    public static class DateTimeCalendarExtensions
    {
        /// <summary>該月 1 號 00:00（保留 Kind）。</summary>
        public static DateTime FirstDayOfMonth(this DateTime d)
            => new DateTime(d.Year, d.Month, 1, 0, 0, 0, d.Kind);

        /// <summary>該月最後一天 00:00。</summary>
        public static DateTime LastDayOfMonth(this DateTime d)
            => new DateTime(d.Year, d.Month, DateTime.DaysInMonth(d.Year, d.Month), 0, 0, 0, d.Kind);

        public static int DaysInMonth(this DateTime d) => DateTime.DaysInMonth(d.Year, d.Month);

        public static bool IsSameDay(this DateTime a, DateTime b) => a.Date == b.Date;

        public static bool IsSameMonth(this DateTime a, DateTime b) => a.Year == b.Year && a.Month == b.Month;

        /// <summary>以「月」為單位比較：a 早於 b 回傳負數、同月 0、晚於 b 正數。</summary>
        public static int CompareMonth(this DateTime a, DateTime b)
            => (a.Year * 12 + a.Month) - (b.Year * 12 + b.Month);

        /// <summary>是否位於 [start, end] 之間（含頭尾、以日為單位、自動處理 start &gt; end）。</summary>
        public static bool IsBetweenDays(this DateTime d, DateTime start, DateTime end)
        {
            DateTime x = d.Date, s = start.Date, e = end.Date;
            if (s > e) { DateTime t = s; s = e; e = t; }
            return x >= s && x <= e;
        }

        /// <summary>兩日期間隔天數（含頭尾），例如同一天回傳 1。</summary>
        public static int InclusiveDayCount(this DateTime start, DateTime end)
        {
            int diff = (int)(end.Date - start.Date).TotalDays;
            return (diff < 0 ? -diff : diff) + 1;
        }

        /// <summary>月曆第一列前面需補幾格上個月的日期。</summary>
        public static int LeadingDays(this DateTime anyDayInMonth, DayOfWeek firstDayOfWeek)
        {
            int offset = (int)anyDayInMonth.FirstDayOfMonth().DayOfWeek - (int)firstDayOfWeek;
            return offset < 0 ? offset + 7 : offset;
        }

        /// <summary>月曆格子(6x7)左上角那一格的日期。</summary>
        public static DateTime CalendarGridStart(this DateTime anyDayInMonth, DayOfWeek firstDayOfWeek)
            => anyDayInMonth.FirstDayOfMonth().AddDays(-anyDayInMonth.LeadingDays(firstDayOfWeek));

        /// <summary>夾在 min/max 之間（以日為單位），null 表示不限制。</summary>
        public static DateTime ClampDay(this DateTime d, DateTime? min, DateTime? max)
        {
            DateTime x = d.Date;
            if (min.HasValue && x < min.Value.Date) return min.Value.Date;
            if (max.HasValue && x > max.Value.Date) return max.Value.Date;
            return x;
        }

        /// <summary>以「月」為單位夾在 min/max 所在月份之間，回傳該月 1 號。null 表示不限制。</summary>
        public static DateTime ClampMonth(this DateTime d, DateTime? min, DateTime? max)
        {
            DateTime m = d.FirstDayOfMonth();
            if (min.HasValue && m.CompareMonth(min.Value) < 0) return min.Value.FirstDayOfMonth();
            if (max.HasValue && m.CompareMonth(max.Value) > 0) return max.Value.FirstDayOfMonth();
            return m;
        }

        public static bool IsWithin(this DateTime d, DateTime? min, DateTime? max)
        {
            DateTime x = d.Date;
            return (!min.HasValue || x >= min.Value.Date) && (!max.HasValue || x <= max.Value.Date);
        }
    }

    /// <summary>
    /// 0~99 的整數字串快取，避免每次刷新日期數字都產生字串 GC。
    /// </summary>
    public static class IntStringCacheExtensions
    {
        private const int Max = 100;
        private static readonly string[] Cache = Build();

        private static string[] Build()
        {
            var arr = new string[Max];
            for (int i = 0; i < Max; i++) arr[i] = i.ToString();
            return arr;
        }

        /// <summary>0~99 走快取，其餘 fallback 到 ToString()。</summary>
        public static string ToCachedString(this int value)
            => (uint)value < Max ? Cache[value] : value.ToString();
    }
}