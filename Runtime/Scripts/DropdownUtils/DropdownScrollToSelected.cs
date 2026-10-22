using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace VzDev.ApiExtensions
{
    /// <summary>
    /// TMP_Dropdown 展開時，自動把列表捲動到目前選取的項目（置中）。
    /// 只在「點擊 / Submit 展開」那一刻執行一次，沒有 Update，平時零成本。
    /// 可直接掛在 Dropdown 上，或用 dropdown.EnableScrollToSelected() 動態加入。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TMP_Dropdown))]
    public sealed class DropdownScrollToSelected : MonoBehaviour, IPointerClickHandler, ISubmitHandler
    {
        const string ListName = "Dropdown List";   // TMP_Dropdown.Show() 產生的列表名稱

        TMP_Dropdown _dropdown;

        void Awake() => _dropdown = GetComponent<TMP_Dropdown>();

        // 同一個 GameObject 上的 handler 會依元件順序執行；本元件排在 TMP_Dropdown 之後，
        // 因此執行到這裡時 Show() 已經同步建立完列表。
        public void OnPointerClick(PointerEventData eventData) => ScrollToSelected();
        public void OnSubmit(BaseEventData eventData) => ScrollToSelected();

        public void ScrollToSelected()
        {
            if (_dropdown == null || !_dropdown.IsExpanded) return;

            // 1) Show() 會對選中的 Toggle 呼叫 Select()，直接拿 EventSystem 的選取物件，免搜尋
            RectTransform item = null;
            EventSystem es = EventSystem.current;
            if (es != null && es.currentSelectedGameObject != null)
                item = es.currentSelectedGameObject.transform as RectTransform;

            ScrollRect scrollRect = item != null ? item.GetComponentInParent<ScrollRect>() : null;

            // 2) 備援：沒有 EventSystem 選取時，用 index 找（只在少數情況才走這裡）
            if (scrollRect == null || !item.IsChildOf(transform))
            {
                Transform list = transform.Find(ListName);
                if (list == null || !list.TryGetComponent(out scrollRect) || scrollRect.content == null) return;
                Transform content = scrollRect.content;
                // content 底下第一個是隱藏的 Item 模板，其後依序為 Item 0..N-1
                int childIndex = content.childCount - _dropdown.options.Count + _dropdown.value;
                if (childIndex < 0 || childIndex >= content.childCount) return;
                item = content.GetChild(childIndex) as RectTransform;
            }

            scrollRect.CenterOnItem(item);
        }
    }

    public static class ScrollRectExtension
    {
        /// <summary>垂直捲動 ScrollRect，讓 item 盡量置中於 viewport（自動 Clamp 在可捲動範圍內）</summary>
        public static void CenterOnItem(this ScrollRect scrollRect, RectTransform item)
        {
            if (scrollRect == null || item == null) return;
            RectTransform content = scrollRect.content;
            if (content == null) return;
            RectTransform viewport = scrollRect.viewport != null ? scrollRect.viewport : (RectTransform)scrollRect.transform;

            float viewH = viewport.rect.height;
            float scrollable = content.rect.height - viewH;
            if (scrollable <= 0f) return;   // 內容沒超出，不需捲動

            // item 中心換算到 content 座標，與 pivot / anchor 設定無關
            Vector3 center = content.InverseTransformPoint(item.TransformPoint(item.rect.center));
            float fromTop = content.rect.yMax - center.y;
            float target = fromTop - viewH * 0.5f;

            scrollRect.StopMovement();
            scrollRect.verticalNormalizedPosition = 1f - Mathf.Clamp01(target / scrollable);
        }
    }
}
