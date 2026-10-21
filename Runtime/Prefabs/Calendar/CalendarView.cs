using System;
using System.Collections.Generic;
using NaughtyAttributes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace VzDev.ToolUtils.Calendar
{
    public enum TodayButtonAction : byte
    {
        /// <summary>只跳到今天所在月份。</summary>
        JumpOnly,
        /// <summary>跳到今天所在月份並選取今天（Range 模式視為一次點擊）。</summary>
        JumpAndSelect,
    }

    /// <summary>
    /// 日曆 UI。固定 42 格(6x7)物件池，切換月份只改資料不生成物件。
    /// 選取邏輯全部委派給 <see cref="CalendarSelection"/>。
    /// </summary>
    public sealed class CalendarView : MonoBehaviour
    {
        private const int CellCount = 42;

        [Foldout("[Setup]"), SerializeField] private CalendarStyle style;
        [Foldout("[Setup]"), SerializeField] private CalendarSelectionMode mode = CalendarSelectionMode.Single;
        [Foldout("[Setup]"), SerializeField] private DayOfWeek firstDayOfWeek = DayOfWeek.Sunday;

        [Foldout("[Limits]"), Tooltip("Multiple 模式最多選幾天，0 = 不限"), SerializeField, Min(0)] private int maxMultipleCount;
        [Foldout("[Limits]"), Tooltip("Range 模式最多跨幾天，0 = 不限"), SerializeField, Min(0)] private int maxRangeDays;
        [Foldout("[Limits]"), SerializeField] private bool allowDeselectSingle = true;
        [Foldout("[Limits]"), Tooltip("是否可選擇今天以後的日期"), SerializeField] private bool allowFutureDates = true;

        [Foldout("[UI References]"), Tooltip("放 42 格的容器（建議掛 GridLayoutGroup, Constraint = Fixed Column Count 7）"), SerializeField] private RectTransform cellContainer;
        [Foldout("[UI References]"), SerializeField] private CalendarDayCell cellPrefab;
        [Foldout("[UI References]"), Tooltip("可選：7 個星期標題"), SerializeField] private TMP_Text[] weekdayLabels = new TMP_Text[7];
        [Foldout("[UI References]"), SerializeField] private TMP_Text titleLabel;
        [Foldout("[UI References]"), SerializeField] private Button prevMonthButton;
        [Foldout("[UI References]"), SerializeField] private Button nextMonthButton;
        [Foldout("[UI References]"), SerializeField] private Button prevYearButton;
        [Foldout("[UI References]"), SerializeField] private Button nextYearButton;
        [Foldout("[UI References]"), Tooltip("可選：回到今天"), SerializeField] private Button todayButton;
        [Foldout("[UI References]"), SerializeField] private TodayButtonAction todayButtonAction = TodayButtonAction.JumpOnly;
        [Foldout("[UI References]"), Tooltip("可選：選取目前顯示月份（Range / Multiple 模式有效）"), SerializeField] private Button selectMonthButton;
        [Foldout("[UI References]"), Tooltip("可選：區間模式的提示文字（請選擇開始/結束日期、已選區間）"), SerializeField] private TMP_Text rangeHintLabel;

        // ───────────── 簡易事件（依模式觸發，一般情況訂閱這些即可） ─────────────

        /// <summary>Single：選取了某一天。</summary>
        public event Action<DateTime> OnDateSelected;

        /// <summary>
        /// Multiple：目前所有已選日期（由小到大）。
        /// 注意：這是元件內部重複使用的 buffer，下次選取改變時內容會被覆寫；若要保存請自行複製。
        /// </summary>
        public event Action<IReadOnlyList<DateTime>> OnDatesSelected;

        /// <summary>Range：起訖都選好時觸發（只選了起點不會觸發）。</summary>
        public event Action<DateTime, DateTime> OnRangeSelected;

        /// <summary>
        /// 從「有有效選取」變為「沒有有效選取」時觸發。
        /// 包含：取消選取、Clear()、切換模式；Range 模式下已完成的區間被重新點選起點也算。
        /// </summary>
        public event Action OnSelectionCleared;

        // ───────────── 進階事件 ─────────────

        /// <summary>選取任何改變都會觸發（含 Range 只選了起點的中間狀態），需要完整狀態時使用。</summary>
        public event Action<CalendarSelection> OnSelectionChanged;
        /// <summary>顯示月份改變，參數為該月 1 號。</summary>
        public event Action<DateTime> OnMonthChanged;

        /// <summary>自訂停用規則（例如假日、無資料的日期）。設定後請呼叫 <see cref="Refresh"/>。</summary>
        public Func<DateTime, bool> IsDateDisabled;

        public CalendarSelection Selection { get; private set; }
        public DateTime DisplayMonth => _displayMonth;

        // 取值捷徑，等同 Selection.xxx
        public bool HasSelection => Selection != null && Selection.HasSelection;
        public bool IsSelectionComplete => Selection != null && Selection.IsComplete;
        public DateTime? StartDate => Selection?.Start;
        public DateTime? EndDate => Selection?.End;
        public IReadOnlyList<DateTime> SelectedDates => Selection != null ? Selection.SelectedDates : Array.Empty<DateTime>();

        private CalendarDayCell[] _cells;
        private Action<int> _onCellClicked;
        private DateTime _displayMonth;
        private DateTime _gridStart;
        private DateTime? _minDate;
        private DateTime? _maxDate;
        private bool _initialized;
        private readonly List<DateTime> _monthBuffer = new List<DateTime>(31); // 多選整月用，重複使用避免 GC
        private readonly List<DateTime> _selectedBuffer = new List<DateTime>(31); // OnDatesSelected 用，重複使用避免 GC
        private bool _hadValidSelection;

        // ───────────── Lifecycle ─────────────

        private void Awake() => EnsureInit();

        private void OnDestroy()
        {
            if (Selection != null) Selection.OnChanged -= HandleSelectionChanged;
        }

        private void EnsureInit()
        {
            if (_initialized) return;
            _initialized = true;

            Selection = new CalendarSelection(mode)
            {
                MaxMultipleCount = maxMultipleCount,
                MaxRangeDays = maxRangeDays,
                AllowDeselectSingle = allowDeselectSingle,
            };
            Selection.OnChanged += HandleSelectionChanged;

            _onCellClicked = HandleCellClicked; // 快取 delegate
            BuildCells();
            ApplyWeekdayLabels();

            if (prevMonthButton != null) prevMonthButton.onClick.AddListener(PrevMonth);
            if (nextMonthButton != null) nextMonthButton.onClick.AddListener(NextMonth);
            if (prevYearButton != null) prevYearButton.onClick.AddListener(PrevYear);
            if (nextYearButton != null) nextYearButton.onClick.AddListener(NextYear);
            if (todayButton != null) todayButton.onClick.AddListener(HandleTodayButton);
            if (selectMonthButton != null) selectMonthButton.onClick.AddListener(SelectDisplayMonth);

            _displayMonth = DateTime.Today.FirstDayOfMonth();
            Refresh();
            UpdateRangeHint();
        }

        private void BuildCells()
        {
            _cells = new CalendarDayCell[CellCount];

            // 支援場景中已預先擺好的格子：容器下有幾個用幾個，不足再 Instantiate
            int existing = 0;
            for (int i = 0; i < cellContainer.childCount && existing < CellCount; i++)
            {
                var c = cellContainer.GetChild(i).GetComponent<CalendarDayCell>();
                if (c != null) _cells[existing++] = c;
            }
            for (int i = existing; i < CellCount; i++)
                _cells[i] = Instantiate(cellPrefab, cellContainer, false);

            for (int i = 0; i < CellCount; i++)
                _cells[i].Init(i, _onCellClicked);
        }

        private void ApplyWeekdayLabels()
        {
            if (weekdayLabels == null) return;
            for (int i = 0; i < weekdayLabels.Length && i < 7; i++)
            {
                if (weekdayLabels[i] == null) continue;
                weekdayLabels[i].text = style.weekdayNames[((int)firstDayOfWeek + i) % 7];
            }
        }

        // ───────────── Public API ─────────────

        public CalendarSelectionMode Mode
        {
            get { EnsureInit(); return Selection.Mode; }
            set { EnsureInit(); mode = value; Selection.Mode = value; }
        }

        /// <summary>設定可選日期範圍，null = 不限制。超出範圍的已選日期不會自動清除。</summary>
        public void SetDateLimits(DateTime? min, DateTime? max)
        {
            EnsureInit();
            _minDate = min?.Date;
            _maxDate = max?.Date;
            Refresh();
        }

        public void ShowMonth(DateTime anyDayInMonth)
        {
            EnsureInit();
            DateTime m = anyDayInMonth.FirstDayOfMonth();
            if (m == _displayMonth) return;
            _displayMonth = m;
            Refresh();
            OnMonthChanged?.Invoke(_displayMonth);
        }

        /// <summary>是否可選擇今天以後的日期。與 SetDateLimits 的 max 取較早者。</summary>
        public bool AllowFutureDates
        {
            get => allowFutureDates;
            set
            {
                if (allowFutureDates == value) return;
                allowFutureDates = value;
                Refresh();
            }
        }

        public void PrevMonth() => ShowMonth(_displayMonth.AddMonths(-1));
        public void NextMonth() => ShowMonth(_displayMonth.AddMonths(1));

        /// <summary>前一年。若超出可選範圍，停在最早可選的月份。</summary>
        public void PrevYear() => ShowMonth(_displayMonth.AddYears(-1).ClampMonth(_minDate, EffectiveMax(DateTime.Today)));

        /// <summary>下一年。若超出可選範圍，停在最晚可選的月份。</summary>
        public void NextYear() => ShowMonth(_displayMonth.AddYears(1).ClampMonth(_minDate, EffectiveMax(DateTime.Today)));
        public void GoToToday() => ShowMonth(DateTime.Today);

        /// <summary>跳到今天並選取今天。今天若被停用則只跳轉。</summary>
        public void SelectToday()
        {
            EnsureInit();
            DateTime today = DateTime.Today;
            ShowMonth(today);
            if (IsDisabled(today, EffectiveMax(today))) return;

            switch (Selection.Mode)
            {
                case CalendarSelectionMode.Single:
                    // 用 SetSingle 而非 Click，避免今天已選時被 toggle 取消
                    if (!Selection.IsSelected(today)) Selection.SetSingle(today);
                    break;
                case CalendarSelectionMode.Multiple:
                    if (!Selection.IsSelected(today)) Selection.Click(today);
                    break;
                default:
                    Selection.Click(today); // 當作起點或終點
                    break;
            }
        }

        /// <summary>選取目前顯示的月份。</summary>
        public void SelectDisplayMonth() => SelectMonth(_displayMonth);

        /// <summary>
        /// 選取指定月份中所有可選的日期。
        /// Range：設為該月第一個～最後一個可選日（受 MaxRangeDays 限制）。
        /// Multiple：選取該月所有未停用的日期（受 MaxMultipleCount 限制）。
        /// Single：不支援，直接 return。
        /// </summary>
        public bool SelectMonth(DateTime anyDayInMonth)
        {
            EnsureInit();
            if (Selection.Mode == CalendarSelectionMode.Single) return false;

            DateTime? max = EffectiveMax(DateTime.Today);
            if (!TryGetSelectableMonthRange(anyDayInMonth, max, out DateTime start, out DateTime end))
                return false;

            ShowMonth(anyDayInMonth);

            if (Selection.Mode == CalendarSelectionMode.Range)
            {
                if (Selection.MaxRangeDays > 0 && start.InclusiveDayCount(end) > Selection.MaxRangeDays)
                    end = start.AddDays(Selection.MaxRangeDays - 1);
                Selection.SetRange(start, end);
            }
            else
            {
                _monthBuffer.Clear();
                for (DateTime d = start; d <= end; d = d.AddDays(1))
                    if (IsDateDisabled == null || !IsDateDisabled(d)) _monthBuffer.Add(d);
                Selection.SetMultiple(_monthBuffer); // 超過 MaxMultipleCount 的部分會被截掉
            }
            return true;
        }

        public TodayButtonAction TodayAction
        {
            get => todayButtonAction;
            set => todayButtonAction = value;
        }

        public void SetStyle(CalendarStyle newStyle)
        {
            EnsureInit();
            if (newStyle == null || newStyle == style) return;
            style = newStyle;
            ApplyWeekdayLabels();
            for (int i = 0; i < CellCount; i++) _cells[i].Invalidate();
            Refresh();
            UpdateRangeHint();
        }

        /// <summary>重算 42 格狀態。只在月份/選取/限制改變時呼叫，非每幀。</summary>
        public void Refresh()
        {
            if (!_initialized) return;

            _gridStart = _displayMonth.CalendarGridStart(firstDayOfWeek);
            DateTime today = DateTime.Today; // 每次 Refresh 只取一次
            bool rangeMode = Selection.Mode == CalendarSelectionMode.Range;
            bool rangePending = Selection.IsRangePending;
            DateTime? max = EffectiveMax(today); // 每次 Refresh 只算一次

            DateTime date = _gridStart;
            for (int i = 0; i < CellCount; i++, date = date.AddDays(1))
            {
                var state = CalendarDayState.None;

                if (!date.IsSameMonth(_displayMonth)) state |= CalendarDayState.OtherMonth;
                if (date == today) state |= CalendarDayState.Today;
                if (IsDisabled(date, max)) state |= CalendarDayState.Disabled;

                if (rangeMode)
                {
                    switch (Selection.GetRangeRole(date))
                    {
                        case CalendarSelection.RangeRole.Start: state |= CalendarDayState.RangeStart; break;
                        case CalendarSelection.RangeRole.End: state |= CalendarDayState.RangeEnd; break;
                        case CalendarSelection.RangeRole.Inside: state |= CalendarDayState.InRange; break;
                        case CalendarSelection.RangeRole.StartAndEnd:
                            state |= CalendarDayState.RangeStart | CalendarDayState.RangeEnd;
                            if (rangePending) state |= CalendarDayState.RangePending;
                            break;
                    }
                }
                else if (Selection.IsSelected(date))
                {
                    state |= CalendarDayState.Selected;
                }

                _cells[i].Apply(date.Day, state, style);
            }

            if (titleLabel != null) titleLabel.text = _displayMonth.ToString(style.monthTitleFormat);

            // 年按鈕與月按鈕同條件：只要還能往前/往後就可按，跳太遠會被 ClampMonth 夾住
            bool canPrev = !_minDate.HasValue || _displayMonth.CompareMonth(_minDate.Value) > 0;
            bool canNext = !max.HasValue || _displayMonth.CompareMonth(max.Value) < 0;
            if (prevMonthButton != null) prevMonthButton.interactable = canPrev;
            if (nextMonthButton != null) nextMonthButton.interactable = canNext;
            if (prevYearButton != null) prevYearButton.interactable = canPrev;
            if (nextYearButton != null) nextYearButton.interactable = canNext;
            if (todayButton != null)
                todayButton.interactable = today.IsWithin(_minDate, max); // 今天超出限制範圍就停用
            if (selectMonthButton != null)
                selectMonthButton.interactable = Selection.Mode != CalendarSelectionMode.Single
                                                 && TryGetSelectableMonthRange(_displayMonth, max, out _, out _);
        }

        // ───────────── Internal ─────────────

        /// <summary>實際生效的最大日期：不允許未來日期時，取 maxDate 與 today 較早者。</summary>
        private DateTime? EffectiveMax(DateTime today)
        {
            if (allowFutureDates) return _maxDate;
            return _maxDate.HasValue && _maxDate.Value < today ? _maxDate : today;
        }

        /// <summary>
        /// 取得某月實際可選的第一天與最後一天（已套用 min/max 與 IsDateDisabled）。整月都不可選則回傳 false。
        /// </summary>
        private bool TryGetSelectableMonthRange(DateTime anyDayInMonth, DateTime? max, out DateTime start, out DateTime end)
        {
            start = anyDayInMonth.FirstDayOfMonth().ClampDay(_minDate, max);
            end = anyDayInMonth.LastDayOfMonth().ClampDay(_minDate, max);

            // 被夾到別的月份 → 這個月完全在範圍外
            if (!start.IsSameMonth(anyDayInMonth) || !end.IsSameMonth(anyDayInMonth) || start > end)
                return false;

            // 頭尾跳過 IsDateDisabled 的日期（例如月初剛好是假日）
            if (IsDateDisabled != null)
            {
                while (start <= end && IsDateDisabled(start)) start = start.AddDays(1);
                while (end >= start && IsDateDisabled(end)) end = end.AddDays(-1);
            }
            return start <= end;
        }

        private bool IsDisabled(DateTime date, DateTime? max)
            => !date.IsWithin(_minDate, max) || (IsDateDisabled != null && IsDateDisabled(date));

        private void HandleTodayButton()
        {
            if (todayButtonAction == TodayButtonAction.JumpAndSelect) SelectToday();
            else GoToToday();
        }

        private void HandleCellClicked(int index)
        {
            DateTime date = _gridStart.AddDays(index);
            if (IsDisabled(date, EffectiveMax(DateTime.Today))) return;

            bool otherMonth = !date.IsSameMonth(_displayMonth);
            if (otherMonth && style.hideOtherMonthDays) return;

            // Click 若有變化會觸發 Selection.OnChanged → HandleSelectionChanged → Refresh
            Selection.Click(date);

            if (otherMonth && style.jumpMonthOnOtherMonthClick) ShowMonth(date);
        }

        private void HandleSelectionChanged()
        {
            Refresh();
            UpdateRangeHint();
            DispatchTypedEvents();
            OnSelectionChanged?.Invoke(Selection);
        }

        /// <summary>依目前模式觸發型別明確的事件。</summary>
        private void DispatchTypedEvents()
        {
            bool valid;
            switch (Selection.Mode)
            {
                case CalendarSelectionMode.Single:
                    valid = Selection.TryGetSingle(out DateTime date);
                    if (valid) OnDateSelected?.Invoke(date);
                    break;

                case CalendarSelectionMode.Multiple:
                    valid = Selection.Count > 0;
                    if (valid && OnDatesSelected != null) // 沒人訂閱就不必填 buffer
                    {
                        Selection.GetSelectedDates(_selectedBuffer);
                        OnDatesSelected(_selectedBuffer);
                    }
                    break;

                default:
                    valid = Selection.TryGetRange(out DateTime start, out DateTime end);
                    if (valid) OnRangeSelected?.Invoke(start, end);
                    break;
            }

            if (!valid && _hadValidSelection) OnSelectionCleared?.Invoke();
            _hadValidSelection = valid;
        }

        /// <summary>只在選取改變時呼叫（非每次換月），字串配置量可忽略。</summary>
        private void UpdateRangeHint()
        {
            if (rangeHintLabel == null) return;

            if (Selection.Mode != CalendarSelectionMode.Range)
            {
                rangeHintLabel.text = string.Empty;
                return;
            }

            if (Selection.TryGetRange(out DateTime s, out DateTime e))
                rangeHintLabel.text = string.Format(style.hintRangeFormat, s, e, s.InclusiveDayCount(e));
            else
                rangeHintLabel.text = Selection.IsRangePending ? style.hintSelectEnd : style.hintSelectStart;
        }
    }
}