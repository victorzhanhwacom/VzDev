using UnityEngine;
using VzDev.RenderingUtils.Outline;

namespace VzDev.InteractiveUtils.ModelMouseEvent
{
    /// <summary>
    /// 純呈現層：把 ColliderInteractionSystem 的 Hover 事件轉換成 HighlightRegistry 的
    /// Hover 群組狀態。不涉及選取（Selected）邏輯，兩者職責分離。
    /// </summary>
    public class HoverHighlightController : MonoBehaviour
    {
        #region Lifecycle
        private void OnEnable()
        {
            ColliderInteractionSystem.OnMouseEnter += HandleEnter;
            ColliderInteractionSystem.OnMouseExit += HandleExit;
        }

        private void OnDisable()
        {
            ColliderInteractionSystem.OnMouseEnter -= HandleEnter;
            ColliderInteractionSystem.OnMouseExit -= HandleExit;
            // 物件被停用時，主動清空 Hover 群組，避免殘留輪廓
            HighlightRegistry.SetSingle(HighlightGroup.Hover, null);
        }
        #endregion

        #region Handlers
        private void HandleEnter(GameObject go)
        {
            if (go == null) return;
            if (go.TryGetComponent<Renderer>(out var r))
                HighlightRegistry.SetSingle(HighlightGroup.Hover, r);
        }

        private void HandleExit(GameObject go)
        {
            HighlightRegistry.SetSingle(HighlightGroup.Hover, null);
        }
        #endregion
    }
}