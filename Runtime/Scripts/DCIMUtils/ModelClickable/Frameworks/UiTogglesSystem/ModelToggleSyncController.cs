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

            if (model != null && ModelToggleRegistry.TryGetBinding(model, out var binding))
            {
                currentActiveBinding = binding;
                binding.SetActiveWithoutNotify();
                return;
            }

            if (currentActiveBinding == null) return;

            currentActiveBinding.SetInactiveWithoutNotify();
            currentActiveBinding = null;
        }
    }
}