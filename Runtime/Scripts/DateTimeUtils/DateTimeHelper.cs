using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using Random = System.Random;

namespace VzDev.DateTimeUtils
{
    /// DateTime日期格式
    public static class DateTimeHelper
    {
        /// 將字串轉成DateTime當地時間(LocalTime)
        public static DateTime StringToDateTime(string dateTimeString, DateTime? onFailed = null)
        {
            if (string.IsNullOrWhiteSpace(dateTimeString)) return onFailed ?? DateTime.MinValue; // 西元 1 年 1 月 1 日午夜 0 點
            dateTimeString = dateTimeString.Trim();
            if (DateTime.TryParse(dateTimeString, null, DateTimeStyles.AdjustToUniversal, out var utcTime)) return utcTime.ToLocalTime();
            return onFailed ?? DateTime.MinValue;
        }

        /// 將各個時間單位轉換成總秒數
        public static int CalculatedToTotalSeconds(float hour, float minute, float second) => Mathf.RoundToInt(hour * 60 * 60 + minute * 60 + second);

        /// 取得時間格式化string
        public static string GetTimeFormat(EnumTimeFormat timeFormat) =>
            timeFormat switch
            {
                EnumTimeFormat.時分秒_12小時制 => "tt hh:mm:ss",
                EnumTimeFormat.時分秒_24小時制 => "HH:mm:ss",
                EnumTimeFormat.西元年月日 => "yyyy/MM/dd",
                EnumTimeFormat.星期 => "dddd",
                EnumTimeFormat.星期_縮寫 => "ddd",
                EnumTimeFormat.完整年月日時分秒_12小時制 => "yyyy/MM/dd tt hh:mm:ss",
                EnumTimeFormat.完整年月日時分秒_24小時制 => "yyyy/MM/dd HH:mm:ss",
                _ => string.Empty,
            };

        /// 依照瀏覽的電腦系統換算UTC時間
        public static DateTime GetLoaclTimeBySystem()
        {
            var offsetNow = DateTimeOffset.Now;
            return offsetNow.LocalDateTime;
        }

        /// 2024-11-24 13:33:22
        public static string FullDateTimeFormat => $"{FullDateFormat} {FullTimeFormat}";

        /// 2024-11-24 13:33
        public static string FullDateTimeMinuteFormat => $"{FullDateFormat}\n{HourMinuteFormat}";

        /// 2024-11-24T13:33:22
        public static string FullDateTimeFormatWithT => $"{FullDateFormat}T{FullTimeFormat}";

        /// 2024-11-24
        public static string FullDateFormat => "yyyy-MM-dd";

        /// 2024-11-24 (週六)
        public static string FullDateFormatWithWeekDay(DateTime dateTime) =>
            dateTime.ToString($"{FullDateFormat} (ddd)", new CultureInfo("zh-TW"));

        /// 13:33:22
        public static string FullTimeFormat => "HH:mm:ss";

        /// 13:33
        public static string HourMinuteFormat => "HH:mm";

        /// 檢查目前是否為整點 (分鐘和秒數都為0)
        public static bool isNowOnTheHour
        {
            get
            {
                // 取得當前時間
                DateTime currentTime = DateTime.Now;
                return (currentTime.Minute == 0 && currentTime.Second == 0);
            }
        }

        /// 指定日期是否在日期區間內
        /// <parp>+ to 會自動換算為to天的23:59:59</parp>
        public static bool isDateInBetweenDays(DateTime date, DateTime from, DateTime to)
        {
            from = from.Date;
            to = to.Date.AddDays(1).AddTicks(-1);
            return date >= from && date <= to;
        }

        /// 指定日期是否在今年內
        public static bool isDateInThisYear(DateTime date) => isDateInYear(date, DateTime.Now.Year);

        /// 指定日期是否在指定年份內
        public static bool isDateInYear(DateTime date, int year)
        {
            DateTime from = new DateTime(year, 1, 1);
            DateTime to = from.AddYears(1).AddTicks(-1);
            return isDateInBetweenDays(date, from, to);
        }


        /// 指定日期是否在當月內
        public static bool IsDateInThisMonth(DateTime date) => IsDateInMonth(date, DateTime.Now.Month);

        /// 指定日期是否在指定月份內 {選填：指定年份}
        public static bool IsDateInMonth(DateTime date, int month, int year = -1)
        {
            DateTime from = new DateTime(year == -1 ? DateTime.Now.Year : year, month, 1);
            DateTime to = from.AddMonths(1).AddTicks(-1);
            return isDateInBetweenDays(date, from, to);
        }


        /// 指定日期是否在某天內 {目標時間, 指定哪一天}
        public static bool isDateInDay(DateTime date, DateTime day) => isDateInBetweenDays(date, day, day);


        /// 指定日期是否在今天內
        public static bool isDateInToday(DateTime date) => isDateInDay(date, DateTime.Today);


        /// 從00:00 ~ 24:00每一小時整點的字串列表

        public static List<string> hoursOfDay
        {
            get
            {
                List<string> result = new List<string>();
                // 獲取今天的日期
                DateTime startOfDay = DateTime.Today; //抓到的時間是 00:00
                // 從00:00到23:00的每小時整點時間
                for (int hour = 0; hour <= 23; hour++)
                {
                    DateTime hourlyTime = startOfDay.AddHours(hour);
                    result.Add(hourlyTime.ToString("HH:mm"));
                }

                return result;
            }
        }

        private static CultureInfo cultureInfo_ENG { get; set; }
        private static CultureInfo cultureInfo_ZH { get; set; }

        public static CultureInfo GetCulture(bool isEng = true)
        {
            if (isEng) return cultureInfo_ENG ??= new CultureInfo("en-US");
            else return cultureInfo_ZH ??= new CultureInfo("enzh-CN");
        }

        /// 依年份取得隨機某一天時間點 {是否限制不超過目前時間}
        public static DateTime GetRandomDateTimeInYear(int year, bool limitToCurrentTime = false)
        {
            Random random = new Random();
            DateTime start = new DateTime(year, 1, 1); // 隨機生成的開始時間
            DateTime end = start.AddYears(1).AddTicks(-1); // 隨機生成的結束時間
            if (limitToCurrentTime)
            {
                end = DateTime.UtcNow; // 限制到目前時間
            }

            // 生成隨機時間
            TimeSpan timeSpan = end - start;
            TimeSpan newSpan = new TimeSpan((long)(random.NextDouble() * timeSpan.Ticks));
            DateTime randomDateTime = start + newSpan;
            return randomDateTime;
        }

        /// 從今天日期隨機某一天時間點 {是否限制不超過目前時間}
        public static DateTime GetRandomDateTimeInToday(bool limitToCurrentTime = true)
        {
            // 獲取當前時間 或 今日午夜12點之前
            DateTime todayEnd = limitToCurrentTime ? DateTime.Now : DateTime.Now.Date.AddDays(1).AddTicks(-1);
            DateTime todayStart = todayEnd.Date; // 今天的開始時間（00:00:00）
            // 計算今天的總秒數
            TimeSpan timeSpan = todayEnd - todayStart;
            int totalSeconds = (int)timeSpan.TotalSeconds;
            Random random = new Random();
            int randomSeconds = random.Next(0, totalSeconds + 1); // 生成隨機秒數
            return todayStart.AddSeconds(randomSeconds);
        }

        /// [格式] - 全球標準時間 2024-12-29T23:49:38.241Z
        public static string Format_GlobalTime => "yyyy-MM-ddTHH:mm:ss.fffZ";


        /// 字串轉換成LocalTime
        public static DateTime StrToLocalTime(string dateTimeString) => DateTime.Parse(dateTimeString).ToLocalTime();

        public static string[] MonthName_ZH => Enum.GetNames(typeof(EnumMonthName_ZH));

        public enum EnumMonthName_ZH
        {
            一月 = 1,
            二月,
            三月,
            四月,
            五月,
            六月,
            七月,
            八月,
            九月,
            十月,
            十一月,
            十二月
        }

        /// 將年份字串轉成整數值 (2025年 → 2025)
        public static int YearStringToInt(string yearName)
        {
            if (yearName.Contains("年", StringComparison.OrdinalIgnoreCase) == false) return 0;
            return int.Parse(yearName.Split("年")[0]);
        }

        /// 將月份字串轉成整數值 (二月 → 2)
        public static int MonthStringToInt(string monthName)
        {
            if (monthName.Contains("月", StringComparison.OrdinalIgnoreCase) == false) return 0;
            Dictionary<string, int> monthMap = new Dictionary<string, int>
            {
                { "一月", 1 }, { "二月", 2 }, { "三月", 3 }, { "四月", 4 },
                { "五月", 5 }, { "六月", 6 }, { "七月", 7 }, { "八月", 8 },
                { "九月", 9 }, { "十月", 10 }, { "十一月", 11 }, { "十二月", 12 }
            };
            return monthMap[monthName];
        }

        /// 取得指定年月份的天數 {１日～３１日}
        public static List<string> DaysInMonth(int year, int month)
            => Enumerable.Range(1, DateTime.DaysInMonth(year, month))
                .Select(day => $"{day}日").ToList();

        /// 產生List時間格式 00:00，01:00，02:00...到目前時間點
        public static List<string> GenerateHourlyTimePoints(DateTime? endTime = null)
        {
            List<string> timePoints = new List<string>();

            DateTime now = endTime ?? DateTime.Now;
            int currentHour = now.Hour; // 現在是第幾小時

            for (int hour = 0; hour <= currentHour; hour++)
            {
                string timeString = hour.ToString("D2") + ":00"; // 轉成兩位數字格式
                timePoints.Add(timeString);
            }

            return timePoints;
        }

        public static List<string> GenerateHourlyTimePointsWithUTC0Formate(DateTime? endTime = null)
        {
            List<string> result = new List<string>();
            List<string> timePoints = GenerateHourlyTimePoints(endTime);

            // 假設要指定的日期是 2025-04-27
            DateTime baseDate = DateTime.Now;

            foreach (var time in timePoints)
            {
                // 解析 "HH:mm"
                if (TimeSpan.TryParse(time, out TimeSpan timeSpan))
                {
                    // 把 baseDate 加上小時數
                    DateTime localDateTime = baseDate.Date + timeSpan;

                    // 假設 baseDate 是本地時間，要轉成 UTC
                    DateTime utcDateTime = TimeZoneInfo.ConvertTimeToUtc(localDateTime);

                    // 轉成 ISO8601 格式 "yyyy-MM-ddTHH:mm:ssZ"
                    string iso8601 = utcDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ");

                    result.Add(iso8601);
                }
                else
                {
                    Debug.LogWarning($"無法解析時間字串: {time}");
                }
            }

            return result;
        }
    }
}