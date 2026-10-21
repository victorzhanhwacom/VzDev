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
        [Foldout("[Components]"), SerializeField] private TMP_Dropdown dropdown;
        [Foldout("[Settings]"), SerializeField] private int intervalMinutes = 30;
        private TimeSpan timeSpanInterval;
#if UNITY_EDITOR
        [Button]
        private void BuildTimeOptions()
        {
            if (intervalMinutes == 0) return;
            dropdown.ClearOptions();
            timeSpanInterval = TimeSpan.FromMinutes(intervalMinutes);
            dropdown.SetTimeOptions24Hours(timeSpanInterval);
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
        #endregion

        private void OnValidate()
        {
            if (dropdown == null) dropdown = GetComponent<TMP_Dropdown>();
        }
    }
}
