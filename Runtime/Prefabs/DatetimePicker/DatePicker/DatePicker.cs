using System;
using NaughtyAttributes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VzDev.ToolUtils.Calendar;
using VzDev.UnityAPI.Extensions;
namespace VzDev.DateTimeUtils
{
    /// <summary>
    /// 日期選取器，結合日曆組件CalendarView
    /// </summary>
    public class DatePicker : MonoBehaviour
    {
        #region Fields
        [Foldout("[Components]"), SerializeField] private CalendarView calendarView;
        [Foldout("[Components]"), SerializeField] private Toggle toggle;
        [Foldout("[Components]"), SerializeField] private TextMeshProUGUI dateText;
        [Foldout("[Components]"), SerializeField] private RectTransform rectTransform;
        /// <summary>
        /// 選取的日期時間
        /// </summary>
        public DateTime SelectedDateTime { get; private set; }
        #endregion

        private void OnValidate()
        {
            if (calendarView == null) calendarView = GetComponentInChildren<CalendarView>();
            if (toggle == null) toggle = GetComponent<Toggle>();
            if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
        }

        private void Start()
        {
            toggle.isOn = false;
            calendarView.gameObject.SetActive(false);
        }
        private void OnEnable()
        {
            toggle.onValueChanged.AddListener(OnToggleValueChanged);
            calendarView.OnDateSelected += HandleDateSelected;
            calendarView.SelectToday();
        }

        private void OnToggleValueChanged(bool isOn) => calendarView.gameObject.SetActive(isOn);

        private void OnDisable()
        {
            toggle.onValueChanged.RemoveListener(OnToggleValueChanged);
            calendarView.OnDateSelected -= HandleDateSelected;
        }
        private void HandleDateSelected(DateTime time)
        {
            SelectedDateTime = time;
            dateText.text = time.ToString("yyyy/MM/dd");
            toggle.isOn = false;
        }
    }
}
