using System;
using NaughtyAttributes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace VzDev.ToolUtils.Calendar
{
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

        [Foldout("[Limits]"), Tooltip("Multiple 模式最多選幾天，0 = 不限")]
        [SerializeField, Min(0)] private int maxMultipleCount;
        [Foldout("[Limits]"), Tooltip("Range 模式最多跨幾天，0 = 不限")]
        [SerializeField, Min(0)] private int maxRangeDays;
        [Foldout("[Limits]"), SerializeField] private bool allowDeselectSingle = true;

        [Foldout("[UI References]"), Tooltip("放 42 格的容器（建議掛 GridLayoutGroup, Constraint = Fixed Column Count 7）")]
        [SerializeField] private RectTransform cellContainer;
        [Foldout("[UI References]"), SerializeField] private CalendarDayCell cellPrefab;
        [Foldout("[UI References]"), Tooltip("可選：7 個星期標題")]
        [SerializeField] private TMP_Text[] weekdayLabels = new TMP_Text[7];
        [Foldout("[UI References]"), SerializeField] private TMP_Text titleLabel;
        [Foldout("[UI References]"), SerializeField] private Button prevMonthButton;
        [Foldout("[UI References]"), SerializeField] private Button nextMonthButton;
        [Foldout("[UI References]"), Tooltip("可選：區間模式的提示文字（請選擇開始/結束日期、已選區間）")]
        [SerializeField] private TMP_Text rangeHintLabel;

        /// <summary>選取改變（點擊或程式設定皆會觸發）。</summary>
        public event Action<CalendarSelection> SelectionChanged;
        /// <summary>顯示月份改變，參數為該月 1 號。</summary>
        public event Action<DateTime> MonthChanged;

        /// <summary>自訂停用規則（例如假日、無資料的日期）。設定後請呼叫 <see cref="Refresh"/>。</summary>
        public Func<DateTime, bool> IsDateDisabled;

        public CalendarSelection Selection { get; private set; }
        public DateTime DisplayMonth => _displayMonth;

        private CalendarDayCell[] _cells;
        private Action<int> _onCellClicked;
        private DateTime _displayMonth;
        private DateTime _gridStart;
        private DateTime? _minDate;
        private DateTime? _maxDate;
        private bool _initialized;

        // ───────────── Lifecycle ─────────────

        private void Awake() => EnsureInit();

        private void OnDestroy()
        {
            if (Selection != null) Selection.Changed -= HandleSelectionChanged;
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
            Selection.Changed += HandleSelectionChanged;

            _onCellClicked = HandleCellClicked; // 快取 delegate
            BuildCells();
            ApplyWeekdayLabels();

            if (prevMonthButton != null) prevMonthButton.onClick.AddListener(PrevMonth);
            if (nextMonthButton != null) nextMonthButton.onClick.AddListener(NextMonth);

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
            MonthChanged?.Invoke(_displayMonth);
        }

        public void PrevMonth() => ShowMonth(_displayMonth.AddMonths(-1));
        public void NextMonth() => ShowMonth(_displayMonth.AddMonths(1));
        public void GoToToday() => ShowMonth(DateTime.Today);

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

            DateTime date = _gridStart;
            for (int i = 0; i < CellCount; i++, date = date.AddDays(1))
            {
                var state = CalendarDayState.None;

                if (!date.IsSameMonth(_displayMonth)) state |= CalendarDayState.OtherMonth;
                if (date == today) state |= CalendarDayState.Today;
                if (!date.IsWithin(_minDate, _maxDate) || (IsDateDisabled != null && IsDateDisabled(date)))
                    state |= CalendarDayState.Disabled;

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

            if (prevMonthButton != null)
                prevMonthButton.interactable = !_minDate.HasValue || _displayMonth.CompareMonth(_minDate.Value) > 0;
            if (nextMonthButton != null)
                nextMonthButton.interactable = !_maxDate.HasValue || _displayMonth.CompareMonth(_maxDate.Value) < 0;
        }

        // ───────────── Internal ─────────────

        private void HandleCellClicked(int index)
        {
            DateTime date = _gridStart.AddDays(index);
            if (!date.IsWithin(_minDate, _maxDate)) return;
            if (IsDateDisabled != null && IsDateDisabled(date)) return;

            bool otherMonth = !date.IsSameMonth(_displayMonth);
            if (otherMonth && style.hideOtherMonthDays) return;

            // Click 若有變化會觸發 Selection.Changed → HandleSelectionChanged → Refresh
            Selection.Click(date);

            if (otherMonth && style.jumpMonthOnOtherMonthClick) ShowMonth(date);
        }

        private void HandleSelectionChanged()
        {
            Refresh();
            UpdateRangeHint();
            SelectionChanged?.Invoke(Selection);
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
