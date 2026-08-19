using System;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;

namespace VzDev.DateTimeUtils
{
    public class DateTimeMediator : MonoBehaviour
    {
        [Foldout("[Events]")] public UnityEvent<string> onDateTimeString;
        [Foldout("[Settings]"), SerializeField] private DateTimeFormatType formatType = DateTimeFormatType.Date_MMdd_ddd;

        [Foldout("[Settings]"), SerializeField] private bool isEng = true;
        
        private bool isEventSubscribed => onDateTimeString.GetPersistentEventCount() > 0;

        /// <summary>
        /// 取得目前時間
        /// </summary>
        [Button, ShowIf("isEventSubscribed")]
        public void GetNowDateTime() => OnReceive(DateTime.Now);
        /// <summary>
        /// 接收時間並轉換成字串格式
        /// </summary>
        public void OnReceive(DateTime datetime)
        {
            string formattedDateTime = datetime.ToString(formatType.ToFormatString(), DateTimeHelper.GetCulture(isEng));
            onDateTimeString?.Invoke(formattedDateTime);
        }
    }
}
