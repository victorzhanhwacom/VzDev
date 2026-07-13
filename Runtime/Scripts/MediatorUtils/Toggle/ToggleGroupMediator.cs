using System.Linq;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace VzDev.MediatorUtils
{
    /// 判斷是否ToggleGroup內的任一Toggle被選中，並透過事件傳遞結果
    public class ToggleGroupMediator : MonoBehaviour
    {
        #region Fields

        [Foldout("[Events]")] public UnityEvent<bool> onAnyToggleOn;
        [Foldout("[Events]")] public UnityEvent<Toggle> invokeSelectedToggle;
        [Foldout("[Componetns]"), SerializeField] private ToggleGroup toggleGroup;

        private Toggle selectedToggle; // 儲存目前被選中的 Toggle
        private bool lastStatus; // 儲存上一次的狀態，避免重複觸發事件
        #endregion

        /// <summary>
        /// 當ToggleGroup內的任一Toggle被選中時，觸發此方法。這裡可以取得目前被選中的Toggle。
        /// </summary>
        public void OnToggleValueChanged(bool isOn)
        {
            CheckIfAnySelected();
            GetSelectedToggle(isOn);
        }

        /// 檢查是否有任何 Toggle 被選中，並觸發 onAnyToggleOn 事件
        public void CheckIfAnySelected()
        {
            if (toggleGroup == null)
            {
                Debug.LogError($"[ToggleGroupMediator] `{name}`的ToggleGroup 尚未指定！");
                return;
            }

            bool anyOn = toggleGroup.AnyTogglesOn();
            // 只有當狀態改變時才觸發事件，避免重複觸發
            if (anyOn != lastStatus)
            {
                lastStatus = anyOn;
                onAnyToggleOn?.Invoke(anyOn);
            }
        }

        /// <summary>
        /// 取得目前被選中的Toggle，並觸發事件傳遞該Toggle及其對應的FollowerTarget。
        /// </summary>
        private void GetSelectedToggle(bool isOn)
        {
            if (toggleGroup == null)
            {
                Debug.LogError($"[ToggleGroupMediator] `{name}`的ToggleGroup 尚未指定！");
                return;
            }

            if (!isOn) return; // 忽略被切成 Off 的那次呼叫

            Toggle activeToggle = toggleGroup.ActiveToggles().FirstOrDefault();
            if (activeToggle != null)
            {
                selectedToggle = activeToggle; // 更新目前被選中的 Toggle
                invokeSelectedToggle?.Invoke(selectedToggle); // 觸發事件，傳遞目前被選中的 Toggle
            }
        }

        public void SetTargetToggleOff()
        {
            if (selectedToggle != null)
            {
                selectedToggle.isOn = false;
                selectedToggle = null; // 清除目前的 Toggle
            }
        }

        private void OnDisable() => SetTargetToggleOff();

        private void OnValidate()
        {
            if (toggleGroup == null) toggleGroup = GetComponent<ToggleGroup>();
        }
    }
}