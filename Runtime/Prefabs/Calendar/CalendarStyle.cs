using NaughtyAttributes;
using TMPro;
using UnityEngine;

namespace VzDev.ToolUtils.Calendar
{
    /// <summary>日曆外觀設定，美術/企劃可自行建立多套主題。</summary>
    [CreateAssetMenu(menuName = "VzDev/Calendar Style", fileName = "CalendarStyle")]
    public sealed class CalendarStyle : ScriptableObject
    {
        [Foldout("[Text]")] public string monthTitleFormat = "yyyy年 M月";
        [Foldout("[Text]"), Tooltip("索引 0 = 星期日")] public string[] weekdayNames = { "日", "一", "二", "三", "四", "五", "六" };

        [Foldout("[Normal]")] public Color normalBg = new Color(1f, 1f, 1f, 0f);
        [Foldout("[Normal]")] public Color normalText = new Color(0.15f, 0.15f, 0.15f);
        [Foldout("[Normal]")] public Color otherMonthText = new Color(0.15f, 0.15f, 0.15f, 0.3f);
        [Foldout("[Normal]")] public Color disabledText = new Color(0.5f, 0.5f, 0.5f, 0.35f);

        [Foldout("[Today]")] public Color todayBg = new Color(0.2f, 0.55f, 1f, 0.15f);
        [Foldout("[Today]")] public Color todayText = new Color(0.2f, 0.55f, 1f);

        [Foldout("[Selected (Single / Multiple)]")] public Color selectedBg = new Color(0.2f, 0.55f, 1f);
        [Foldout("[Selected (Single / Multiple)]")] public Color selectedText = Color.white;

        [Foldout("[Range Start / End]")] public Color rangeStartBg = new Color(0.1f, 0.45f, 0.95f);
        [Foldout("[Range Start / End]")] public Color rangeStartText = Color.white;
        [Foldout("[Range Start / End]")] public Color rangeEndBg = new Color(0.95f, 0.5f, 0.1f);
        [Foldout("[Range Start / End]")] public Color rangeEndText = Color.white;
        [Foldout("[Range Start / End]"), Tooltip("只選了起點、尚未選終點時，起點格的顏色")] public Color rangePendingBg = new Color(0.1f, 0.45f, 0.95f, 0.5f);
        [Foldout("[Range Start / End]")] public Color rangePendingText = Color.white;

        [Foldout("[In Range]")] public Color rangeBand = new Color(0.2f, 0.55f, 1f, 0.2f);
        [Foldout("[In Range]")] public Color inRangeText = new Color(0.1f, 0.3f, 0.7f);
        [Foldout("[In Range]"), Tooltip("RangeBand 高度，佔格子高度的比例（垂直置中）"), Range(0.05f, 1f)] public float rangeBandHeight = 0.7f;
        [Foldout("[In Range]"), Tooltip("起迄日 RangeBand 佔格子寬度的比例。0.5 = 從中間開始；越大越往外延伸"), Range(0f, 1f)] public float rangeEdgeBandWidth = 0.7f;

        [Foldout("[Range Tags]"), Tooltip("需在 Cell 指定 tagLabel")] public string rangeStartTag = "起";
        [Foldout("[Range Tags]")] public string rangeEndTag = "迄";
        [Foldout("[Range Tags]"), Tooltip("起訖為同一天")] public string rangeSameDayTag = "起迄";
        [Foldout("[Range Tags]")] public HorizontalAlignmentOptions rangeStartTagAlign = HorizontalAlignmentOptions.Left;
        [Foldout("[Range Tags]")] public HorizontalAlignmentOptions rangeEndTagAlign = HorizontalAlignmentOptions.Right;
        [Foldout("[Range Tags]")] public HorizontalAlignmentOptions rangeSameDayTagAlign = HorizontalAlignmentOptions.Center;

        [Foldout("[Range Hint]"), Tooltip("需在 View 指定 rangeHintLabel")] public string hintSelectStart = "請選擇開始日期";
        [Foldout("[Range Hint]")] public string hintSelectEnd = "請選擇結束日期";
        [Foldout("[Range Hint]"), Tooltip("{0}=起 {1}=迄 {2}=天數")] public string hintRangeFormat = "{0:yyyy/MM/dd} ~ {1:yyyy/MM/dd}（共 {2} 天）";

        [Foldout("[Bold]")] public bool boldToday = true;
        [Foldout("[Bold]"), Tooltip("單選/多選的選取日、區間起迄日")] public bool boldSelected = true;
        [Foldout("[Bold]"), Tooltip("區間中間的日期")] public bool boldInRange = false;

        [Foldout("[Behaviour]"), Tooltip("非本月的日期不顯示")] public bool hideOtherMonthDays = false;
        [Foldout("[Behaviour]"), Tooltip("點擊非本月日期時自動切換月份")] public bool jumpMonthOnOtherMonthClick = true;
    }
}