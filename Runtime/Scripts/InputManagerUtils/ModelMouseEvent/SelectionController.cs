using UnityEngine;
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
                HighlightRegistry.Clear(HighlightGroup.Selected);
                HighlightRegistry.Add(HighlightGroup.Selected, r);
            }
        }

        private void HandleClickEmpty()
        {
            // 按住多選鍵時點空白處，維持既有選取不變（符合一般多選慣例）
            if (Input.GetKey(multiSelectKey)) return;
            HighlightRegistry.Clear(HighlightGroup.Selected);
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