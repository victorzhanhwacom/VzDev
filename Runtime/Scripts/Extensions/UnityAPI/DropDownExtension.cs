using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;

namespace VzDev.ApiExtensions
{
    public static class DropDownExtension
    {
        public enum SelectedTimeMode
        {
            /// <summary>選取最接近現在時間的項目</summary>
            NearestNowTime,
            /// <summary>選取最接近 00:00 的項目</summary>
            NearestStartTime,
            /// <summary>選取最接近 23:59 的項目</summary>
            NearestEndTime,
        }

        /// <summary>
        /// 設定時間選項為 24 小時制，間隔為 30 分鐘
        /// </summary>
        public static TMP_Dropdown SetTimeOptions24Hours(this TMP_Dropdown timeDropdown, TimeSpan intervalTimeSpan = default
            , SelectedTimeMode mode = SelectedTimeMode.NearestNowTime, bool notify = false)
        {
            var range = new TimeOptionRange(
                start: new TimeSpan(0, 0, 0),
                end: new TimeSpan(23, 59, 59),
                interval: intervalTimeSpan == default ? TimeSpan.FromMinutes(30) : intervalTimeSpan
            );
            timeDropdown.SetTimeOptions(range, notify: notify);
            //是否選取最接近現在時間的項目
            switch (mode)
            {
                case SelectedTimeMode.NearestNowTime:
                    TimeSpan nowTime = DateTime.Now.TimeOfDay;
                    timeDropdown.SelectTime(range, nowTime, notify: notify);
                    break;
                case SelectedTimeMode.NearestStartTime:
                    timeDropdown.SelectTime(range, new TimeSpan(0, 0, 0), notify: notify);
                    break;
                case SelectedTimeMode.NearestEndTime:
                    timeDropdown.SelectTime(range, new TimeSpan(23, 59, 59), notify: notify);
                    break;
            }
            return timeDropdown;
        }

        /// <summary>時間選項的範圍設定（值型別，無 GC）</summary>
        public readonly struct TimeOptionRange
        {
            public readonly long StartTicks;
            public readonly long SpanTicks;     // 已處理跨日，恆 >= 0
            public readonly long StepTicks;
            public readonly int Count;
            /// <summary>end 不在間隔格點上且 includeEnd = true 時，最後一筆補上 end 本身（例：23:30 之後補 23:59:59）</summary>
            public readonly bool HasTail;

            public TimeOptionRange(TimeSpan start, TimeSpan end, TimeSpan interval, bool includeEnd = true)
            {
                if (interval.Ticks <= 0)
                    throw new ArgumentOutOfRangeException(nameof(interval), "interval 必須大於 0");

                StartTicks = start.Ticks;
                StepTicks = interval.Ticks;

                long span = end.Ticks - start.Ticks;
                if (span < 0) span += TimeSpan.TicksPerDay;     // 跨日：22:00 → 02:00
                SpanTicks = span;

                long full = span / StepTicks;
                bool onGrid = span % StepTicks == 0;
                // 格點數 = full + 1（含 start）；end 剛好落在格點且不含 end 時，扣掉最後一格
                long count = (onGrid && !includeEnd) ? full : full + 1;
                // end 不在格點上且要含 end → 額外補一筆 end
                HasTail = includeEnd && !onGrid;
                if (HasTail) count++;
                Count = (int)Math.Min(count, int.MaxValue);
            }

            /// <summary>第 index 項相對 start 的偏移 ticks（最後一筆若為補上的 end，回傳 SpanTicks）</summary>
            public long GetOffsetTicks(int index)
            {
                return (HasTail && index == Count - 1) ? SpanTicks : StepTicks * index;
            }

            /// <summary>第 index 項的時間（以一天為週期回繞）</summary>
            public TimeSpan GetTime(int index)
            {
                long t = (StartTicks + GetOffsetTicks(index)) % TimeSpan.TicksPerDay;
                if (t < 0) t += TimeSpan.TicksPerDay;
                return new TimeSpan(t);
            }

            /// <summary>取得最接近 time 的 index（已 Clamp）</summary>
            public int GetNearestIndex(TimeSpan time)
            {
                if (Count <= 0) return -1;
                long offset = time.Ticks - StartTicks;
                if (offset < 0) offset += TimeSpan.TicksPerDay;
                if (offset >= SpanTicks) return Count - 1;      // 超出範圍 → 最後一筆

                if (HasTail)
                {
                    // 最後一個格點到 end 的距離小於 StepTicks，需另外比較
                    int lastGrid = Count - 2;
                    long lastGridOffset = StepTicks * lastGrid;
                    if (offset > lastGridOffset)
                        return (offset - lastGridOffset) * 2 >= SpanTicks - lastGridOffset ? Count - 1 : lastGrid;
                }

                long index = (offset + StepTicks / 2) / StepTicks;   // 四捨五入
                return (int)Math.Min(index, Count - 1);
            }
        }

        /// <summary>展開列表時自動捲動到目前選取的項目（只在展開當下執行一次，無每幀成本）</summary>
        public static TMP_Dropdown EnableScrollToSelected(this TMP_Dropdown dropdown)
        {
            if (dropdown == null) throw new ArgumentNullException(nameof(dropdown));
            if (!dropdown.TryGetComponent(out DropdownScrollToSelected _))
                dropdown.gameObject.AddComponent<DropdownScrollToSelected>();
            return dropdown;
        }

        public const string DefaultFormat = "HH:mm:ss";

        /// <summary>建立時間列表。重複呼叫時會重用既有 OptionData，只重新產生字串。</summary>
        public static void SetTimeOptions(this TMP_Dropdown dropdown, in TimeOptionRange range,
            string format = DefaultFormat, IFormatProvider provider = null, bool notify = false)
        {
            if (dropdown == null) throw new ArgumentNullException(nameof(dropdown));
            if (provider == null) provider = CultureInfo.InvariantCulture;

            List<TMP_Dropdown.OptionData> options = dropdown.options;   // 直接操作內部 List，不經 AddOptions 複製
            int count = range.Count;

            if (options.Count > count)
                options.RemoveRange(count, options.Count - count);

            for (int i = 0; i < count; i++)
            {
                string text = FormatTicks(range.StartTicks + range.GetOffsetTicks(i), format, provider);
                if (i < options.Count)
                {
                    TMP_Dropdown.OptionData o = options[i];
                    o.text = text;
                    o.image = null;
                }
                else
                {
                    options.Add(new TMP_Dropdown.OptionData(text));
                }
            }

            int value = count == 0 ? 0 : Math.Min(dropdown.value, count - 1);
            if (notify) dropdown.value = value;
            else dropdown.SetValueWithoutNotify(value);
            dropdown.RefreshShownValue();   // value 沒變時 SetValue 不會刷新，需手動呼叫
        }

        public static void SetTimeOptions(this TMP_Dropdown dropdown, TimeSpan start, TimeSpan end, TimeSpan interval,
            bool includeEnd = true, string format = DefaultFormat, bool notify = false)
        {
            dropdown.SetTimeOptions(new TimeOptionRange(start, end, interval, includeEnd), format, null, notify);
        }

        /// <summary>以運算取得目前選取的時間，不解析字串</summary>
        public static TimeSpan GetSelectedTime(this TMP_Dropdown dropdown, in TimeOptionRange range)
        {
            return range.GetTime(dropdown.value);
        }

        /// <summary>選取最接近 time 的項目</summary>
        public static void SelectTime(this TMP_Dropdown dropdown, in TimeOptionRange range, TimeSpan time, bool notify = false)
        {
            int index = range.GetNearestIndex(time);
            if (index < 0) return;
            if (notify) dropdown.value = index;
            else dropdown.SetValueWithoutNotify(index);
        }

        static string FormatTicks(long ticks, string format, IFormatProvider provider)
        {
            long t = ticks % TimeSpan.TicksPerDay;
            if (t < 0) t += TimeSpan.TicksPerDay;
            return new DateTime(t).ToString(format, provider);   // 用 DateTime 格式化，可用 "HH:mm"、"hh:mm tt" 等
        }
    }
}