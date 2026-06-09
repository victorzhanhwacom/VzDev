using UnityEngine;
using System;
using UnityEngine.Events;
using NaughtyAttributes;

namespace VzDev.Mediator
{
    /// <summary>
    /// 根據傳入的整數值，觸發對應的事件，並將其他不匹配的事件設為非活動狀態。
    /// </summary>
    public class ValueSwitcher : MonoBehaviour
    {
        #region Variables
        [SerializeField, ReadOnly] private int receiveValue;
        [SerializeField] private ValueSwitchItem[] switchItems;
        #endregion

        public void SetValue(int value)
        {
            if (receiveValue == value) return;
            receiveValue = value;
            for(int i = 0; i < switchItems.Length; i++)
            {
                bool isActive = switchItems[i].Value == receiveValue;
                switchItems[i].IsActiveEvent.Invoke(isActive);
            }
        }
    }

    [Serializable]
    public class ValueSwitchItem
    {
        public int Value;
        public UnityEvent<bool> IsActiveEvent;
    }
}
