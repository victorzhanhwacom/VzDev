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
    ///
    /// 【架構重點】這裡是唯一「決定選取結果」的地方（重複點擊=取消、Ctrl=多選累加…）。
    /// 下游系統（面板顯示、Toggle 同步）不應該去監聽 ColliderInteractionSystem 的原始點擊事件
    /// 再自己猜測/延後查詢結果，而是應該直接訂閱這裡發出的 OnSoleSelectionChanged——
    /// 這個事件只在「結果確定之後」才發出，帶的是最終結果本身，不是「發生了點擊」這件事，
    /// 因此下游收到後可以直接、同步、正確地反應，不需要任何 LateUpdate 或事件順序上的猜測。
    /// </summary>
    public class SelectionController : MonoBehaviour
    {
        #region Fields
        [SerializeField] private KeyCode multiSelectKey = KeyCode.LeftControl;
        #endregion

        #region Events
        /// <summary>
        /// 選取結果確定後廣播：目前 Selected 集合裡「恰好只有一個」時帶那個 Renderer，
        /// 否則（沒有任何選取，或多選中）帶 null。
        /// 多選（Ctrl+多個物件）的情境下故意帶 null——面板與 Toggle 本質上都是單一焦點的顯示，
        /// 無法呈現「同時顯示多個」，帶 null 代表「沒有唯一確定的焦點」，下游應清空自己的顯示。
        /// </summary>
        public static event System.Action<Renderer> OnSoleSelectionChanged;
        #endregion

        #region 監聽 ColliderInteractionSystem 的點擊事件
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
            RaiseSoleSelectionChanged();
        }

        /// <summary>
        /// 保險清空，避免場景切換/重新載入時，舊的訂閱端 delegate 殘留在這個 static event 上。
        /// 與 GlobalLifecycleBroadcaster.OnDestroy 的清空邏輯是同一種防呆手法。
        /// </summary>
        private void OnDestroy()
        {
            OnSoleSelectionChanged = null;
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

            RaiseSoleSelectionChanged();
        }

        [Button]
        public void CancelSelection()
        {
            HighlightRegistry.Clear(HighlightGroup.Selected);
            RaiseSoleSelectionChanged();
        }

        private void HandleClickEmpty()
        {
            // 按住多選鍵時點空白處，維持既有選取不變（符合一般多選慣例），
            // 什麼都沒改變，所以不廣播事件。
            if (Input.GetKey(multiSelectKey)) return;
            HighlightRegistry.Clear(HighlightGroup.Selected);
            RaiseSoleSelectionChanged();
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

        /// <summary>
        /// 讀取目前 Selected 集合，判斷「是否恰好只有一個」，廣播結果。
        /// 所有會改變 Selected 集合的地方，都必須呼叫這個方法，確保下游收到的永遠是最新結果。
        /// </summary>
        private static void RaiseSoleSelectionChanged()
        {
            var current = HighlightRegistry.Get(HighlightGroup.Selected);
            Renderer sole = null;
            int count = 0;
            foreach (var r in current)
            {
                sole = r;
                count++;
                if (count > 1) break;
            }
            OnSoleSelectionChanged?.Invoke(count == 1 ? sole : null);
        }
        #endregion
    }
}