using UnityEngine;
using VzDev.InteractiveUtils.ModelMouseEvent;

namespace VzDev.InteractiveUtils.ModelMouseEvent
{
    /// <summary>
    /// 監聽 SelectionController 的選取結果，同步 UI Toggle 的選取狀態。
    ///
    /// 【架構修正】原本監聽 ColliderInteractionSystem 的原始點擊事件，本質上是在監聽
    /// 「發生了什麼點擊」，而不是「選取結果是什麼」，導致需要用 LateUpdate 延後一輪，
    /// 等 SelectionController 處理完之後才查詢 HighlightRegistry 的真實狀態。
    ///
    /// 現在改成只監聽 SelectionController.OnSoleSelectionChanged——這個事件本身就是
    /// 「結果」，收到的當下就是最終正確狀態，不再需要 LateUpdate、不再需要查詢
    /// HighlightRegistry，也完全不受任何事件訂閱順序影響。
    ///
    /// 【外觀同步修正】從 A 切換到 B 時，這裡是唯一知道「上一個作用中的 binding 是誰」
    /// 的地方，因此切換前必須主動呼叫舊 binding 的 SetInactiveWithoutNotify()，
    /// 讓它走正常的 onValueChanged Invoke 路徑（Inspector 上掛的 GraphicColorChanger、
    /// Glow/個體資訊 SetActive 等外觀效果才會正確執行關閉）。
    /// 不能再依賴 ToggleGroup.SetAllTogglesOff(sendCallback:false) 去關閉舊 Toggle——
    /// 那樣會跳過事件，造成舊 Toggle 的外觀殘留。
    /// </summary>
    public class ModelToggleSyncController : MonoBehaviour
    {
        private ModelToggleBinding currentActiveBinding;

        private void OnEnable()
        {
            SelectionController.OnSoleSelectionChanged += HandleSoleSelectionChanged;
        }

        private void OnDisable()
        {
            SelectionController.OnSoleSelectionChanged -= HandleSoleSelectionChanged;
        }

        /// <summary>
        /// selected 為 null 代表：沒有任何選取、多選中沒有唯一焦點、或重複點擊造成取消選取，
        /// 這三種情況對 Toggle 來說都是同一件事：關閉目前的 Toggle。
        /// </summary>
        private void HandleSoleSelectionChanged(Renderer selected)
        {
            GameObject model = selected != null ? selected.gameObject : null;

            ModelToggleBinding newBinding = null;
            if (model != null)
                ModelToggleRegistry.TryGetBinding(model, out newBinding);

            // 目標沒有實際變化（同一個 binding 再次收到相同結果），不重複觸發，
            // 避免多餘的 Invoke 造成外觀效果被無意義地重播一次。
            if (newBinding == currentActiveBinding) return;

            // 切換前先關閉舊的作用中 binding，走正常事件路徑讓它自己的外觀效果關閉。
            if (currentActiveBinding != null)
                currentActiveBinding.SetInactiveWithoutNotify();

            currentActiveBinding = newBinding;

            if (currentActiveBinding != null)
                currentActiveBinding.SetActiveWithoutNotify();
        }
    }
}