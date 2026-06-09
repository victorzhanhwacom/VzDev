using System;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;
namespace VzDev.MediatorUtils
{
    /// <summary>
    /// 一個簡單的剩餘時間/數值中介者，適用於倒數計時器、生命值等場景。
    /// </summary>
    public class RemainingMediator : MonoBehaviour
    {
        #region Variables
        [SerializeField, ReadOnly] private float remaining;
        [Foldout("[Events]"), SerializeField] private UnityEvent<int> onValueChanged;
        [Foldout("[Events]"), SerializeField] private UnityEvent<float> onValueChangedF;
        [Foldout("[Events]"), SerializeField] private UnityEvent onValueZero;
        [Foldout("[Settings]"), SerializeField] private int totalValue = 60;
        #endregion

        public void SetTotalValue(int value)
        {
            totalValue = value;
            remaining = totalValue;
        }

        /// <summary>
        /// 調整剩餘數值，並觸發相應事件 (正值增加/負值減少)。
        /// </summary>
        public void Adjust(int value)
        {
            remaining += value;
            CheckRemaining();
        }

        private void CheckRemaining()
        {
            remaining = Mathf.Clamp(remaining, 0, totalValue);
            onValueChanged?.Invoke((int)remaining);
            onValueChangedF?.Invoke(remaining);
            if (remaining <= 0)
            {
                onValueZero?.Invoke();
            }
        }

        private void OnValidate() => ResetRemaining();
        private void Awake() => ResetRemaining();
        public void ResetRemaining() => remaining = totalValue;
    }
}