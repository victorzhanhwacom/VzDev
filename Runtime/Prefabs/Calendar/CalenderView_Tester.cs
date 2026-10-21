using System;
using System.Collections.Generic;
using UnityEngine;
using VzDev.ToolUtils.Calendar;

namespace VzDev
{
    public class CalenderView_Tester : MonoBehaviour
    {
        public CalendarView calendarView;

        private void OnEnable()
        {
            if (calendarView != null)
            {
                calendarView.OnDateSelected += OnDateSelected;
                calendarView.OnDatesSelected += OnDatesSelected;
                calendarView.OnRangeSelected += OnRangeSelected;
            }
        }

        private void OnDatesSelected(IReadOnlyList<DateTime> list)
        {
            Debug.Log($"OnDatesSelected: {string.Join(", ", list)}");
        }

        private void OnRangeSelected(DateTime time1, DateTime time2)
        {
            Debug.Log($"OnRangeSelected: {time1} ~ {time2}");
        }

        private void OnDateSelected(DateTime time)
        {
            Debug.Log($"OnDateSelected: {time}");
        }
    }
}
