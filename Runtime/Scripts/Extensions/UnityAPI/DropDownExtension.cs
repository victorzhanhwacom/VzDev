using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;

namespace VzDev.ApiExtensions
{
    public static class DropDownExtension
    {

        /// <summary>時間選項的範圍設定（值型別，無 GC）</summary>
        public readonly struct TimeOptionRange
        {
            public readonly long StartTicks;
            public readonly long SpanTicks;     // 已處理跨日，恆 >= 0
            public readonly long StepTicks;
            public readonly int Count;

            public TimeOptionRange(TimeSpan start, TimeSpan end, TimeSpan interval, bool includeEnd = true)
            {
                if (interval.Ticks <= 0)
                    throw new ArgumentOutOfRangeException(nameof(interval), "interval 必須大於 0");

                StartTicks = start.Ticks;
                StepTicks = interval.Ticks;

                long span = end.Ticks - start.Ticks;
                if (span < 0) span += TimeSpan.TicksPerDay;     // 跨日：22:00 → 02:00
                SpanTicks = span;

                long count = span / StepTicks;
                if (includeEnd || span % StepTicks != 0) count++;
                Count = (int)Math.Min(count, int.MaxValue);
            }

            /// <summary>第 index 項的時間（以一天為週期回繞）</summary>
            public TimeSpan GetTime(int index)
            {
                long t = (StartTicks + StepTicks * index) % TimeSpan.TicksPerDay;
                if (t < 0) t += TimeSpan.TicksPerDay;
                return new TimeSpan(t);
            }

            /// <summary>取得最接近 time 的 index（已 Clamp）</summary>
            public int GetNearestIndex(TimeSpan time)
            {
                if (Count <= 0) return -1;
                long offset = time.Ticks - StartTicks;
                if (offset < 0) offset += TimeSpan.TicksPerDay;
                long index = (offset + StepTicks / 2) / StepTicks;   // 四捨五入
                return (int)Math.Min(index, Count - 1);
            }
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

            long ticks = range.StartTicks;
            for (int i = 0; i < count; i++, ticks += range.StepTicks)
            {
                string text = FormatTicks(ticks, format, provider);
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