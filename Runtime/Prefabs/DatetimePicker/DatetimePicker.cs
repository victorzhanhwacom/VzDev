using System;
using NaughtyAttributes;
using TMPro;
using UnityEngine;
using static VzDev.ApiExtensions.DropDownExtension;

namespace VzDev
{
    public class DatetimePicker : MonoBehaviour
    {
        [Foldout("[Components]"), SerializeField] private TMP_Dropdown dateDropDown, timeDropdown;

        [Button]
        private void SetTimeOptions()
        {
            var range = new TimeOptionRange(
                start: new TimeSpan(0, 0, 0),
                end: new TimeSpan(23, 59, 59),
                interval: new TimeSpan(0, 30, 0)
            );
            timeDropdown.SetTimeOptions(range);
        }
    }
}
