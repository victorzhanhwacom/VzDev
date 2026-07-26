using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;
using VzDev.InteractiveUtils.ModelMouseEvent;
using VzDev.RenderingUtils.Outline;

namespace VzDev.InteractiveUtils.ModelMouseEvent
{
    /// <summary>
    /// 管理多選狀態，寫入 HighlightRegistry 的 Selected 群組。
    /// 與 HoverHighlightController 職責分離：這裡只處理「點擊累加/覆蓋」，
    /// 不處理 Hover 暫時性高亮。
    /// </summary>
    public class SelectionController : MonoBehaviour
    {
        #region Fields
        [SerializeField] private KeyCode multiSelectKey = KeyCode.LeftControl;

        public UnityEvent onDeselected;
        #endregion

        #region Lifecycle
        private void OnEnable()
        {
            ColliderInteractionSystem.OnMouseClick += HandleClick;
            ColliderInteractionSystem.OnMouseClickEmpty += HandleClickEmpty;
        }

        private void OnDisable()
        {
            ColliderInteractionSystem.OnMouseClick -= HandleClick;
            ColliderInteractionSystem.OnMouseClickEmpty -= HandleClickEmpty;
            HighlightRegistry.Clear(HighlightGroup.Selected);
        }
        #endregion

        #region Handlers
        private void HandleClick(GameObject go)
        {
            if (go == null) return;
            if (!go.TryGetComponent<Renderer>(out var r)) return;

            PruneDestroyedTargets();

            if (Input.GetKey(multiSelectKey))
            {
                // 已經選過的物件再點一次 = 取消選取（toggle）
                if (IsSelected(r))
                    HighlightRegistry.Remove(HighlightGroup.Selected, r);
                else
                    HighlightRegistry.Add(HighlightGroup.Selected, r);
            }
            else
            {
                // 重點修正：先判斷「目前選取集合是否恰好只有這個物件」，
                // 若是，代表這是對同一物件的重複點擊 → 取消選取；
                // 否則才視為「覆蓋式單選」→ 清空後只選這個。
                bool wasSoleSelection = IsSoleSelection(r);

                HighlightRegistry.Clear(HighlightGroup.Selected);

                if (!wasSoleSelection)
                    HighlightRegistry.Add(HighlightGroup.Selected, r);
            }
        }

        [Button]
        public void CancelSelection()
        {
            HighlightRegistry.Clear(HighlightGroup.Selected);
            onDeselected?.Invoke();
        }

        private void HandleClickEmpty()
        {
            // 按住多選鍵時點空白處，維持既有選取不變（符合一般多選慣例）
            if (Input.GetKey(multiSelectKey)) return;
            HighlightRegistry.Clear(HighlightGroup.Selected);
            onDeselected?.Invoke();
        }
        #endregion

        #region Helpers
        private bool IsSelected(Renderer r)
        {
            foreach (var existing in HighlightRegistry.Get(HighlightGroup.Selected))
                if (existing == r) return true;
            return false;
        }

        /// <summary>
        /// 判斷目前 Selected 集合是否「恰好只包含」傳入的這一個 Renderer。
        /// 用於單選模式下辨別「重複點擊同一物件」與「切換到別的物件」。
        /// </summary>
        private bool IsSoleSelection(Renderer r)
        {
            var current = HighlightRegistry.Get(HighlightGroup.Selected);
            int count = 0;
            bool containsR = false;
            foreach (var existing in current)
            {
                count++;
                if (existing == r) containsR = true;
                if (count > 1) break; // 已確定不只一個，可提早結束
            }
            return containsR && count == 1;
        }

        /// <summary>
        /// 惰性清理：每次點擊觸發選取變更前，先移除集合中已被銷毀（null）的殘留參照，
        /// 避免 HashSet 隨場景資產反覆建立/銷毀而持續增長。
        /// </summary>
        private void PruneDestroyedTargets()
        {
            var current = HighlightRegistry.Get(HighlightGroup.Selected);
            System.Collections.Generic.List<Renderer> toRemove = null;
            foreach (var r in current)
            {
                if (r == null)
                {
                    toRemove ??= new System.Collections.Generic.List<Renderer>();
                    toRemove.Add(r);
                }
            }
            if (toRemove == null) return;
            foreach (var r in toRemove)
                HighlightRegistry.Remove(HighlightGroup.Selected, r);
        }
        #endregion
    }
}