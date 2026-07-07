using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace VzDev.DebugUtils
{
    /// 事件處理
    public static class EventHelper
    {
        /// 目前是否正在使用輸入框
        public static bool IsUsingInputField
        {
            get
            {
                GameObject selectedObject = EventSystem.current.currentSelectedGameObject;
                if (selectedObject != null)
                {
                    // 檢查是否為 Unity 的 InputField
                    if (selectedObject.GetComponent<InputField>() != null)
                        return true;
                    // 檢查是否為 TextMeshPro 的 TMP_InputField
                    if (selectedObject.GetComponent<TMP_InputField>() != null)
                        return true;
                }
                return false;
            }
        }
        
        /// 目前鼠標是否位於UI組件上
        public static bool IsPointerOverUI() => EventSystem.current.IsPointerOverGameObject();

        /// 是否有Event Invoke對像
        public static bool IsHaveAnyEventListeners(params UnityEventBase[] unityEvents)
        {
            if (unityEvents == null || unityEvents.Length == 0) return false;
            foreach (var evt in unityEvents)
            {
                if (evt != null && evt.GetPersistentEventCount() > 0)
                    return true; // 只要有一個有綁定對象，就回傳 true
            }
            return false;
        }
    }
}
