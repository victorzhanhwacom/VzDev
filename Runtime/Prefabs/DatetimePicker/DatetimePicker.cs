using System;
using NaughtyAttributes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VzDev.ToolUtils.Calendar;

namespace VzDev.DateTimeUtils
{
    /// <summary>
    /// 日期時間選取器，結合日曆組件CalendarView
    /// </summary>
    public class DatetimePicker : MonoBehaviour
    {
        #region Fields
        [Foldout("[Components]"), SerializeField] private CalendarView calendarView;
        [Foldout("[Components]"), SerializeField] private Toggle toggle;
        [Foldout("[Components]"), SerializeField] private TextMeshProUGUI dateText;
        /// <summary>
        /// 選取的日期時間
        /// </summary>
        public DateTime SelectedDateTime { get; private set; }
        #endregion

        private void OnValidate()
        {
            if (toggle == null) toggle = GetComponent<Toggle>();
            if (calendarView == null) calendarView = GetComponentInChildren<CalendarView>();
        }

        private void Start()
        {
            toggle.isOn = false;
            calendarView.gameObject.SetActive(false);
            calendarView.SelectToday();
        }
        private void OnEnable()
        {
            toggle.onValueChanged.AddListener(OnToggleValueChanged);
            calendarView.OnDateSelected += HandleDateSelected;
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
