using System;
using System.Collections.Generic;

namespace VzDev.ToolUtils.Calendar
{
    public enum CalendarSelectionMode : byte { Single, Multiple, Range }

    /// <summary>
    /// 純 C# 選取狀態，不依賴 UnityEngine，可獨立測試或給其他 UI 使用。
    /// 所有日期一律以 .Date 儲存（去掉時分秒）。
    /// </summary>
    public sealed class CalendarSelection
    {
        public event Action OnChanged;

        private CalendarSelectionMode _mode;
        private DateTime? _single;
        private DateTime? _rangeStart;
        private DateTime? _rangeEnd;
        private readonly HashSet<DateTime> _multiple = new HashSet<DateTime>();
        private readonly List<DateTime> _datesCache = new List<DateTime>(31);
        private bool _datesDirty = true;

        /// <summary>Multiple 模式最多可選幾天，0 = 不限。</summary>
        public int MaxMultipleCount { get; set; }

        /// <summary>Range 模式最多跨幾天(含頭尾)，0 = 不限。超過時會以點擊日重新作為起點。</summary>
        public int MaxRangeDays { get; set; }

        /// <summary>Single 模式點同一天是否取消選取。</summary>
        public bool AllowDeselectSingle { get; set; } = true;

        public CalendarSelection(CalendarSelectionMode mode = CalendarSelectionMode.Single) => _mode = mode;

        public CalendarSelectionMode Mode
        {
            get => _mode;
            set
            {
                if (_mode == value) return;
                ClearInternal();
                _mode = value;
                NotifyChanged();
            }
        }

        // ───────────── 取值（不分模式，統一用法）─────────────

        /// <summary>是否有任何選取。</summary>
        public bool HasSelection => Count > 0;

        /// <summary>選取是否已完成。Range 需起迄都選；其他模式有選就算完成。</summary>
        public bool IsComplete => _mode == CalendarSelectionMode.Range
            ? _rangeStart.HasValue && _rangeEnd.HasValue
            : HasSelection;

        /// <summary>
        /// 最早的選取日。Single = 選取日；Range = 起點；Multiple = 最早一天。未選為 null。
        /// </summary>
        public DateTime? Start
        {
            get
            {
                switch (_mode)
                {
                    case CalendarSelectionMode.Single: return _single;
                    case CalendarSelectionMode.Range: return _rangeStart;
                    default: return _multiple.Count > 0 ? SelectedDates[0] : (DateTime?)null;
                }
            }
        }

        /// <summary>
        /// 最晚的選取日。Single = 選取日；Range = 終點（尚未選終點為 null）；Multiple = 最晚一天。
        /// </summary>
        public DateTime? End
        {
            get
            {
                switch (_mode)
                {
                    case CalendarSelectionMode.Single: return _single;
                    case CalendarSelectionMode.Range: return _rangeEnd;
                    default:
                        var list = SelectedDates;
                        return list.Count > 0 ? list[list.Count - 1] : (DateTime?)null;
                }
            }
        }

        /// <summary>
        /// 所有選取日期（已排序）。Range 會展開起迄之間每一天。
        /// 內部快取，只在選取改變後第一次讀取時重建，不產生 GC；要長期保存請自行複製。
        /// </summary>
        public IReadOnlyList<DateTime> SelectedDates
        {
            get
            {
                if (_datesDirty)
                {
                    GetSelectedDates(_datesCache);
                    _datesDirty = false;
                }
                return _datesCache;
            }
        }

        private void NotifyChanged()
        {
            _datesDirty = true;
            OnChanged?.Invoke();
        }

        /// <summary>Range 模式下已選起點、尚未選終點。</summary>
        public bool IsRangePending => _mode == CalendarSelectionMode.Range && _rangeStart.HasValue && !_rangeEnd.HasValue;

        public int Count
        {
            get
            {
                switch (_mode)
                {
                    case CalendarSelectionMode.Single: return _single.HasValue ? 1 : 0;
                    case CalendarSelectionMode.Multiple: return _multiple.Count;
                    default:
                        if (!_rangeStart.HasValue) return 0;
                        return _rangeEnd.HasValue ? _rangeStart.Value.InclusiveDayCount(_rangeEnd.Value) : 1;
                }
            }
        }

        // ───────────── 使用者點擊 ─────────────

        /// <summary>依目前模式處理一次點擊。回傳選取是否有變化。</summary>
        public bool Click(DateTime date)
        {
            date = date.Date;
            bool changed;
            switch (_mode)
            {
                case CalendarSelectionMode.Single: changed = ClickSingle(date); break;
                case CalendarSelectionMode.Multiple: changed = ClickMultiple(date); break;
                default: changed = ClickRange(date); break;
            }
            if (changed) NotifyChanged();
            return changed;
        }

        private bool ClickSingle(DateTime date)
        {
            if (_single.HasValue && _single.Value == date)
            {
                if (!AllowDeselectSingle) return false;
                _single = null;
                return true;
            }
            _single = date;
            return true;
        }

        private bool ClickMultiple(DateTime date)
        {
            if (_multiple.Remove(date)) return true;
            if (MaxMultipleCount > 0 && _multiple.Count >= MaxMultipleCount) return false;
            return _multiple.Add(date);
        }

        private bool ClickRange(DateTime date)
        {
            // 尚無起點，或區間已完成 → 重新開始
            if (!_rangeStart.HasValue || _rangeEnd.HasValue)
            {
                _rangeStart = date;
                _rangeEnd = null;
                return true;
            }

            DateTime start = _rangeStart.Value;
            if (MaxRangeDays > 0 && start.InclusiveDayCount(date) > MaxRangeDays)
            {
                _rangeStart = date; // 超過上限 → 以此日重新當起點
                return true;
            }

            if (date < start) { _rangeStart = date; _rangeEnd = start; }
            else _rangeEnd = date;
            return true;
        }

        // ───────────── 程式設定 ─────────────

        public void SetSingle(DateTime date)
        {
            ForceMode(CalendarSelectionMode.Single);
            _single = date.Date;
            NotifyChanged();
        }

        public void SetRange(DateTime start, DateTime end)
        {
            ForceMode(CalendarSelectionMode.Range);
            DateTime s = start.Date, e = end.Date;
            if (s > e) { DateTime t = s; s = e; e = t; }
            _rangeStart = s;
            _rangeEnd = e;
            NotifyChanged();
        }

        /// <summary>批次加入多選日期（傳 List 避免 IEnumerable 的 enumerator GC）。</summary>
        public void SetMultiple(List<DateTime> dates, bool clearFirst = true)
        {
            ForceMode(CalendarSelectionMode.Multiple);
            if (clearFirst) _multiple.Clear();
            for (int i = 0; i < dates.Count; i++)
            {
                if (MaxMultipleCount > 0 && _multiple.Count >= MaxMultipleCount) break;
                _multiple.Add(dates[i].Date);
            }
            NotifyChanged();
        }

        public void Clear()
        {
            if (Count == 0) return;
            ClearInternal();
            NotifyChanged();
        }

        private void ForceMode(CalendarSelectionMode mode)
        {
            ClearInternal();
            _mode = mode;
        }

        private void ClearInternal()
        {
            _single = null;
            _rangeStart = null;
            _rangeEnd = null;
            _multiple.Clear();
        }

        // ───────────── 查詢 ─────────────

        public bool TryGetSingle(out DateTime date)
        {
            date = _single.GetValueOrDefault();
            return _mode == CalendarSelectionMode.Single && _single.HasValue;
        }

        /// <summary>取得完整區間（起訖都已選）。</summary>
        public bool TryGetRange(out DateTime start, out DateTime end)
        {
            start = _rangeStart.GetValueOrDefault();
            end = _rangeEnd.GetValueOrDefault();
            return _mode == CalendarSelectionMode.Range && _rangeStart.HasValue && _rangeEnd.HasValue;
        }

        /// <summary>把所有已選日期依序填入 buffer（會先 Clear）。由呼叫端持有 buffer 以避免 GC。</summary>
        public void GetSelectedDates(List<DateTime> buffer)
        {
            buffer.Clear();
            switch (_mode)
            {
                case CalendarSelectionMode.Single:
                    if (_single.HasValue) buffer.Add(_single.Value);
                    break;

                case CalendarSelectionMode.Multiple:
                    foreach (var d in _multiple) buffer.Add(d); // HashSet 的 struct enumerator，無 GC
                    buffer.Sort();
                    break;

                default:
                    if (!_rangeStart.HasValue) break;
                    DateTime end = _rangeEnd ?? _rangeStart.Value;
                    for (DateTime d = _rangeStart.Value; d <= end; d = d.AddDays(1)) buffer.Add(d);
                    break;
            }
        }

        public bool IsSelected(DateTime date)
        {
            date = date.Date;
            switch (_mode)
            {
                case CalendarSelectionMode.Single: return _single.HasValue && _single.Value == date;
                case CalendarSelectionMode.Multiple: return _multiple.Contains(date);
                default: return GetRangeRole(date) != RangeRole.None;
            }
        }

        public enum RangeRole : byte { None, Start, End, Inside, StartAndEnd }

        public RangeRole GetRangeRole(DateTime date)
        {
            if (_mode != CalendarSelectionMode.Range || !_rangeStart.HasValue) return RangeRole.None;

            date = date.Date;
            DateTime s = _rangeStart.Value;
            DateTime e = _rangeEnd ?? s;

            if (date == s) return date == e ? RangeRole.StartAndEnd : RangeRole.Start;
            if (date == e) return RangeRole.End;
            return date > s && date < e ? RangeRole.Inside : RangeRole.None;
        }
    }
}