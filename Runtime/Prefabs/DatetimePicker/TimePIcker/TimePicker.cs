using System;
using NaughtyAttributes;
using TMPro;
using UnityEngine;
using VzDev.ApiExtensions;
using static VzDev.ApiExtensions.DropDownExtension;

namespace VzDev.DateTimeUtils
{
    /// <summary>
    /// 時間選取器，使用 TMP_Dropdown 顯示時間選項
    /// </summary>
    public class TimePicker : MonoBehaviour
    {
        #region Fields
        [Foldout("[Settings]"), SerializeField] private int intervalMinutes = 30;
        [Foldout("[Settings]"), SerializeField] private SelectedTimeMode selectedTimeMode = SelectedTimeMode.NearestNowTime;
        [Foldout("[Components]"), SerializeField] private TMP_Dropdown dropdown;
        private TimeSpan timeSpanInterval;
#if UNITY_EDITOR
        [Button]
        private void BuildTimeOptions()
        {
            if (intervalMinutes == 0) return;
            dropdown.ClearOptions();
            timeSpanInterval = TimeSpan.FromMinutes(intervalMinutes);
            dropdown.SetTimeOptions24Hours(timeSpanInterval, selectedTimeMode);
        }
#endif
        public DateTime currentSelectedTime
        {
            get
            {
                string timeString = dropdown.options[dropdown.value].text;
                if (DateTime.TryParse(timeString, out DateTime time))
                {
                    return time;
                }
                return DateTime.MinValue;
            }
        }

        private int originSelectedIndex = -1;
        #endregion

        private void Start()
        {
            originSelectedIndex = dropdown.value;
        }

        private void OnEnable()
        {
            dropdown.value = originSelectedIndex > -1 ? originSelectedIndex : dropdown.value;
        }

        private void OnValidate()
        {
            if (dropdown == null) dropdown = GetComponent<TMP_Dropdown>();
        }
    }
}
