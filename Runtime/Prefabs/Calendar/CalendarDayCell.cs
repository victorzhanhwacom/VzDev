using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace VzDev.ToolUtils.Calendar
{
    [Flags]
    public enum CalendarDayState : byte
    {
        None = 0,
        OtherMonth = 1 << 0,
        Today = 1 << 1,
        Selected = 1 << 2,
        RangeStart = 1 << 3,
        RangeEnd = 1 << 4,
        InRange = 1 << 5,
        Disabled = 1 << 6,
        /// <summary>區間只選了起點、尚未選終點。</summary>
        RangePending = 1 << 7,
    }

    /// <summary>
    /// 日曆單格。由 CalendarView 預先生成 42 格重複使用，切換月份不會 Instantiate/Destroy。
    /// 只有 (日, 狀態) 改變時才寫入 UI，避免無意義的 Graphic rebuild。
    /// </summary>
    [RequireComponent(typeof(Button))]
    public sealed class CalendarDayCell : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image background;
        [Tooltip("可選：區間的連續底色帶，需為獨立的子物件 Image（全撐滿 anchors）")]
        [SerializeField] private Image rangeBand;
        [SerializeField] private TMP_Text label;
        [Tooltip("可選：顯示「起 / 迄」的小標籤 TMP_Text")]
        [SerializeField] private TMP_Text tagLabel;

        private RectTransform _bandRect;
        private bool _hasBand;
        private bool _hasTag;
        private FontStyles _baseFontStyle; // 保留 Prefab 上原本的 Italic/Underline 等設定
        private bool _dirty = true; // 保證首次一定刷新
        private int _day = -1;
        private CalendarDayState _state;
        private CalendarStyle _style;

        internal void Init(int index, Action<int> onClick)
        {
            if (button == null) button = GetComponent<Button>();
            _hasBand = rangeBand != null;
            if (_hasBand)
            {
                _bandRect = rangeBand.rectTransform;
                // 完全由 anchors 決定大小，清掉 Prefab 上可能殘留的 offset
                _bandRect.offsetMin = Vector2.zero;
                _bandRect.offsetMax = Vector2.zero;
            }
            _baseFontStyle = label.fontStyle & ~FontStyles.Bold;
            _hasTag = tagLabel != null;
            if (_hasTag) tagLabel.enabled = false;

            // 每格只在初始化時建立一次 closure
            button.onClick.AddListener(() => onClick(index));
        }

        /// <summary>強制下次 Apply 重繪（例如更換 Style 時）。</summary>
        internal void Invalidate() => _dirty = true;

        internal void Apply(int day, CalendarDayState state, CalendarStyle style)
        {
            if (!_dirty && day == _day && state == _state && ReferenceEquals(style, _style)) return;
            _dirty = false;
            _day = day;
            _state = state;
            _style = style;

            bool otherMonth = (state & CalendarDayState.OtherMonth) != 0;
            bool disabled = (state & CalendarDayState.Disabled) != 0;
            bool hidden = otherMonth && style.hideOtherMonthDays;

            label.enabled = !hidden;
            button.interactable = !hidden && !disabled;
            if (!hidden) label.SetText(day.ToCachedString());

            bool selected = (state & CalendarDayState.Selected) != 0;
            bool start = (state & CalendarDayState.RangeStart) != 0;
            bool end = (state & CalendarDayState.RangeEnd) != 0;
            bool pending = (state & CalendarDayState.RangePending) != 0;
            bool isEdge = selected || start || end;
            bool inRange = (state & CalendarDayState.InRange) != 0;
            bool today = (state & CalendarDayState.Today) != 0;

            // ── 背景 / 文字顏色
            // 優先序：Hidden > Disabled > 待選終點 > 起點 > 終點 > 選取 > 區間中 > 今天 > 一般
            Color bg, txt;
            if (hidden) { bg = style.normalBg; txt = style.normalText; }
            else if (disabled) { bg = style.normalBg; txt = style.disabledText; }
            else if (pending) { bg = style.rangePendingBg; txt = style.rangePendingText; }
            else if (start) { bg = style.rangeStartBg; txt = style.rangeStartText; }
            else if (end) { bg = style.rangeEndBg; txt = style.rangeEndText; }
            else if (selected) { bg = style.selectedBg; txt = style.selectedText; }
            else if (inRange) { bg = _hasBand ? style.normalBg : style.rangeBand; txt = style.inRangeText; }
            else if (today) { bg = style.todayBg; txt = style.todayText; }
            else { bg = style.normalBg; txt = otherMonth ? style.otherMonthText : style.normalText; }

            if (otherMonth && !hidden && !isEdge) txt.a *= 0.5f;

            background.color = bg;
            label.color = txt;

            bool bold = !hidden && !disabled &&
                        ((isEdge && style.boldSelected) ||
                         (inRange && style.boldInRange) ||
                         (today && style.boldToday));
            FontStyles fs = bold ? _baseFontStyle | FontStyles.Bold : _baseFontStyle;
            if (label.fontStyle != fs) label.fontStyle = fs; // 只在改變時寫入，避免多餘 mesh rebuild

            UpdateBand(state, hidden, style);
            UpdateTag(start, end, pending, hidden, txt, style);
        }

        private void UpdateTag(bool start, bool end, bool pending, bool hidden, Color color, CalendarStyle style)
        {
            if (!_hasTag) return;

            string tag = null;
            HorizontalAlignmentOptions align = HorizontalAlignmentOptions.Center;
            if (!hidden)
            {
                if (start && end)
                {
                    if (pending) { tag = style.rangeStartTag; align = style.rangeStartTagAlign; }
                    else { tag = style.rangeSameDayTag; align = style.rangeSameDayTagAlign; }
                }
                else if (start) { tag = style.rangeStartTag; align = style.rangeStartTagAlign; }
                else if (end) { tag = style.rangeEndTag; align = style.rangeEndTagAlign; }
            }

            bool show = !string.IsNullOrEmpty(tag);
            tagLabel.enabled = show;
            if (!show) return;

            tagLabel.SetText(tag); // 字串來自 Style，不產生 GC
            tagLabel.color = color;
            // 只在對齊真的改變時才寫入，避免多餘的 TMP mesh rebuild
            if (tagLabel.horizontalAlignment != align) tagLabel.horizontalAlignment = align;
        }

        private void UpdateBand(CalendarDayState state, bool hidden, CalendarStyle style)
        {
            if (!_hasBand) return;

            bool start = (state & CalendarDayState.RangeStart) != 0;
            bool end = (state & CalendarDayState.RangeEnd) != 0;
            bool inside = (state & CalendarDayState.InRange) != 0;

            // 起訖同一天或不在區間內 → 不顯示帶子
            bool show = !hidden && (inside || (start ^ end));
            rangeBand.enabled = show;
            if (!show) return;

            rangeBand.color = style.rangeBand;

            // 高度：垂直置中
            float yMin = (1f - style.rangeBandHeight) * 0.5f;
            float yMax = 1f - yMin;

            // 寬度：起點從右側往左延伸、終點從左側往右延伸，中間日撐滿
            float edge = style.rangeEdgeBandWidth;
            float xMin = start ? 1f - edge : 0f;
            float xMax = end ? edge : 1f;

            _bandRect.anchorMin = new Vector2(xMin, yMin);
            _bandRect.anchorMax = new Vector2(xMax, yMax);
        }
    }
}
