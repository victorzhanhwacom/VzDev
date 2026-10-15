using System;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;
using VzDev.ApiExtensions;

namespace VzDev.ToolUtils
{
    /// <summary>
    /// 每到整點時發送 UnityEvent（參數為當下的小時 0~23）。
    /// 使用真實時間（DateTime），不受 Time.timeScale 影響。
    /// </summary>
    public class HourlyChecker : MonoBehaviour
    {
        [Foldout("[Events]")] public UnityEvent<DateTime> triggerEventOnHour;
        private DateTime? lastHour;


        /// <summary>
        /// 檢查是否到達新的整點小時，若是則觸發事件。
        /// </summary>
        public void CheckHourly()
        {
            if (lastHour == null)
            {
                lastHour = DateTime.Now.RoundedToHour();
                return;
            }

            DateTime currentHour = DateTime.Now.RoundedToHour();
            if (currentHour > lastHour)
            {
                lastHour = currentHour;
                TriggerEvent(currentHour);
            }
            else if (currentHour < lastHour)
            {
                // 系統時間被往回調整，重新對齊，不觸發
                lastHour = currentHour;
            }
        }
        private void TriggerEvent(DateTime hour) => triggerEventOnHour?.Invoke(hour);

        [Button]
        private void TestTriggerEvent() => TriggerEvent(DateTime.Now.RoundedToHour());
    }
}
