using System;
using System.Globalization;
using NaughtyAttributes;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using VzDev.NetLibrary.Extensions;
using VzDev.UnityAPI.Extensions;

namespace VzDev.StringUtils
{
    public class StringMediator : MonoBehaviour
    {
        #region Fields
        [SerializeField, ReadOnly] private string receivedValue;
        [Foldout("[Events]")] public UnityEvent<bool> isHaveValueEvent;
        [Foldout("[Events]")] public UnityEvent<int> onIntFormat;
        [Foldout("[TypeCasting]")] public UnityEvent<string> onJsonFormat;
        [Foldout("[Settings]"), SerializeField] private bool isAutoTrim = true;
        #endregion

        public void SetValue(string txt)
        {
            if (!NotifyHasValue(txt)) return;

            var newValue = isAutoTrim ? txt.Trim() : txt;

            // 修正:改用值比較(Ordinal),而非永遠為 false 的 ReferenceEquals
            if (string.Equals(receivedValue, newValue, StringComparison.Ordinal)) return;

            receivedValue = newValue;

            if (onJsonFormat != null)
            {
                onJsonFormat.Invoke(receivedValue.TryToJsonFormat(out var formatted)
               ? formatted
               : txt);
            }
            if(onIntFormat != null)
            {
                onIntFormat.Invoke( int.TryParse(receivedValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedInt) ? parsedInt : 0);
            }
        }

        #region 判斷文字組件的內容是否有值(純查詢,無副作用)
        /// <summary>純查詢,不觸發 isHaveValueEvent</summary>
        public bool HasValue(string txt) => txt.IsValueExist();
        public bool HasValue(TMP_InputField component) => component != null && HasValue(component.text);
        public bool HasValue(TextMeshProUGUI component) => component != null && HasValue(component.text);
        #endregion

        #region 判斷文字組件的內容是否有值(會廣播 isHaveValueEvent)
        /// <summary>查詢並廣播結果,供需要事件通知的呼叫端使用</summary>
        private bool NotifyHasValue(string txt)
        {
            bool isValueExist = HasValue(txt);
            isHaveValueEvent?.Invoke(isValueExist);
            return isValueExist;
        }

        public bool NotifyHasValue(TMP_InputField component) => component != null && NotifyHasValue(component.text);
        public bool NotifyHasValue(TextMeshProUGUI component) => component != null && NotifyHasValue(component.text);
        #endregion
    }
}