using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace VzDev.MediatorUtils
{
    /// 判斷是否ToggleGroup內的任一Toggle被選中，並透過事件傳遞結果
    public class ToggleGroupMediator : MonoBehaviour
    {
        #region Variables

        // [SerializeField, Label("子項Toggle自動監聽點擊判斷")] public bool isToggleChildrenAutoListen = true;
        [Foldout("[Events]")] public UnityEvent<bool> onAnyToggleOn;
        [Foldout("[Events]")] public UnityEvent<Toggle> invokeSelectedToggle;
        [Foldout("[Componetns]"), SerializeField] private ToggleGroup toggleGroup;
        [Foldout("[Settings]"), SerializeField] private bool isAutoListen = true;

        private Toggle selectedToggle; // 儲存目前被選中的 Toggle
        private bool lastStatus; // 儲存上一次的狀態，避免重複觸發事件
        #endregion

        private void Start()
        {
            if(isAutoListen) AllTogglesListen();
        }

        /// 取得 ToggleGroup 下的所有 Toggle 元件，并为每个 Toggle 添加监听事件判斷是否點擊
        public void AllTogglesListen()
        {
             // 步驟 1：安全檢查，如果 Inspector 沒拉，才嘗試從自己身上抓
            if (toggleGroup == null)
            {
                TryGetComponent(out toggleGroup);
            }
            // 步驟 2：不論是手動拉的還是自動抓的，只要有成功取得群組，就進行監聽
            if (toggleGroup != null)
            {
                lastStatus = toggleGroup.AnyTogglesOn();
            }
            else
            {
                Debug.LogError($"[ToggleGroupMediator] `{name}` 找不到指定的 ToggleGroup 組件！", this);
            }

            Toggle[] allToggles = toggleGroup.GetComponentsInChildren<Toggle>(true);

            // 2. 透過迴圈，幫每一個 Toggle 綁定監聽事件
            for (int i = 0; i < allToggles.Length; i++)
            {
                Toggle toggle = allToggles[i];
                // 關鍵防呆：只監聽「屬於這個群組」的 Toggle，避免抓到路過的或巢狀的 Toggle
                if (toggle.group == toggleGroup)
                {
                    // 這裡傳入的 isOn 代表該 Toggle 改變後的狀態（true/false）
                    toggle.onValueChanged.AddListener((isOn) =>
                    {
                        if (isOn) selectedToggle = toggle; // ✅ 如果被選中，更新目前的 Toggle
                        CheckIfAnySelected(); ;
                    });
                }
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

                if (anyOn)
                {
                    invokeSelectedToggle?.Invoke(selectedToggle);
                }
            }
        }
        private void OnValidate()
        {
            if (toggleGroup == null) toggleGroup = GetComponent<ToggleGroup>();
        }
    }
}