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
        
        private ValueSwitchItem currentActiveItem;
        #endregion

        public void SetValue(int value)
        {
            if (receiveValue == value) return;
            if (currentActiveItem != null)
            {
                currentActiveItem.IsActiveEvent?.Invoke(false);
                currentActiveItem.OnFalse?.Invoke();
            }

            receiveValue = value;
            for(int i = 0; i < switchItems.Length; i++)
            {
                if(switchItems[i].Value == receiveValue)
                {
                    /// 僅觸發對應的事件
                    switchItems[i].IsActiveEvent.Invoke(true);
                    switchItems[i].OnTrue?.Invoke();
                    currentActiveItem = switchItems[i];
                }
            }
        }
    }

    [Serializable]
    public class ValueSwitchItem
    {
        public int Value;
        public UnityEvent<bool> IsActiveEvent;
        public UnityEvent OnTrue, OnFalse;
    }
}
