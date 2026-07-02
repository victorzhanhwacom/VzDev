using System;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;

namespace VzDev.MediatorUtils
{
    public class RangeValueMediator : MonoBehaviour
    {
        #region Fields
        [SerializeField, ReadOnly] private float receiveValue = -1f;
        [Foldout("[Settings]"), SerializeField] private RangeValueItem[] rangeItems;

        public void SetValue(int value) => SetValue((float)value);

        public void SetValue(float value)
        {
            if (Mathf.Approximately(receiveValue, value)) return;
            receiveValue = value;

            for (int i = 0; i < rangeItems.Length; i++)
            {
                RangeValueItem item = rangeItems[i];
                if (receiveValue >= item.minValue && receiveValue <= item.maxValue)
                {
                    item.onValueInRange?.Invoke();
                }
            }
        }
        #endregion

        [Serializable]
        public class RangeValueItem
        {
            public float minValue, maxValue;
            public UnityEvent onValueInRange;
        }
    }
}
