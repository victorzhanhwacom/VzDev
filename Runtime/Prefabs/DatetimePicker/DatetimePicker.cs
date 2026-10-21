using System;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.UI;

namespace VzDev.DateTimeUtils
{
    public class DateTimePicker : MonoBehaviour
    {
         #region Fields
        [Foldout("[Components]"), SerializeField] private DatePicker datePickerFrom;
        [Foldout("[Components]"), SerializeField] private TimePicker timePickerFrom;
        [Foldout("[Components]"), SerializeField] private DatePicker datePickerTo;
        [Foldout("[Components]"), SerializeField] private TimePicker timePickerTo;
        [Foldout("[Components]"), SerializeField] private Button btnSearch;
       // [Foldout("[Components]"), SerializeField] private WebAPI_CallerBase_DeviceHistory  webAPI_CallerBase_DeviceHistory;

        #endregion


        private void OnEnable()
        {
            btnSearch.onClick.AddListener(OnSearchButtonClicked);
        }
        private void OnDisable()
        {
            btnSearch.onClick.RemoveListener(OnSearchButtonClicked);
        }

        private void OnSearchButtonClicked()
        {
            DateTime fromDateTime = datePickerFrom.SelectedDateTime.Date + timePickerFrom.currentSelectedTime.TimeOfDay;
            DateTime toDateTime = datePickerTo.SelectedDateTime.Date + timePickerTo.currentSelectedTime.TimeOfDay;

            Debug.Log($"搜尋時間範圍: From: {fromDateTime}, To: {toDateTime}");

            // 在這裡可以呼叫 WebAPI_CallerBase_DeviceHistory 來查詢資料
            // webAPI_CallerBase_DeviceHistory.GetDeviceHistory(tagIds, fromDateTime.ToString("o"), toDateTime.ToString("o"), onSuccess, onFailure);
        }
    }
}
