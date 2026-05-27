using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;

namespace VzDev.MediatorUtils
{
    /// Bool值中介者
    public class BoolValueMediator : MonoBehaviour
    {
        #region Events
        [Foldout("[Event] Bool反轉"), Tooltip("傳回與輸入值相反的 bool 值")]public UnityEvent<bool> onReversedChanged;

        [Foldout("[Event] 條件觸發")] public UnityEvent onTrueTriggered;
        [Foldout("[Event] 條件觸發")] public UnityEvent onFalseTriggered;

        #endregion

        /// 接收 0 或 1 的整數並轉換為 bool
        public void SetBoolValue01(int value)
        {
            // 使用 Mathf.Clamp 確保安全，或是直接用 value == 1
            SetBoolValue(value > 0);
        }
        
        /// 主要觸發核心
        public void SetBoolValue(bool value)
        {
            // 1. 反轉事件
            onReversedChanged?.Invoke(!value);

            // 2. 根據 true / false 分流觸發
            if (value) onTrueTriggered?.Invoke();
            else onFalseTriggered?.Invoke();
        }
    }
}
