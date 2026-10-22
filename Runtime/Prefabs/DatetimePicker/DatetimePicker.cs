using System;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.UI;

namespace VzDev.DateTimeUtils
{
    /// <summary>
    /// 負責日期時間起迄的選取
    /// </summary>
    public class DateTimePicker : MonoBehaviour
    {
        #region Fields
        public Action<DateTime, DateTime> OnSearchButtonClickedAction;
        [Foldout("[Components]"), SerializeField] private DatePicker datePickerFrom;
        [Foldout("[Components]"), SerializeField] private TimePicker timePickerFrom;
        [Foldout("[Components]"), SerializeField] private DatePicker datePickerTo;
        [Foldout("[Components]"), SerializeField] private TimePicker timePickerTo;
        [Foldout("[Components]"), SerializeField] private Button btnSearch;
        #endregion


        private void OnEnable() => btnSearch.onClick.AddListener(OnSearchButtonClicked);
        private void OnDisable() => btnSearch.onClick.RemoveListener(OnSearchButtonClicked);

        private void OnSearchButtonClicked()
        {
            DateTime fromDateTime = datePickerFrom.SelectedDateTime.Date + timePickerFrom.currentSelectedTime.TimeOfDay;
            DateTime toDateTime = datePickerTo.SelectedDateTime.Date + timePickerTo.currentSelectedTime.TimeOfDay;

            Debug.Log($"搜尋時間範圍: From: {fromDateTime}, To: {toDateTime}");
            OnSearchButtonClickedAction?.Invoke(fromDateTime, toDateTime);
        }
    }
}
