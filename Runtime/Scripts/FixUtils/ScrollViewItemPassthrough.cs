using UnityEngine;
using UnityEngine.EventSystems;

namespace VzDev.FixUtils
{
    /// <summary>
    /// 讓 UI 元件在滑鼠進入/離開時，將事件傳遞給父層的 ScrollRect，使其能正常滾動。
    /// </summary>
    public class ScrollViewItemPassthrough : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler, IInitializePotentialDragHandler
    {
        public void OnPointerEnter(PointerEventData e)
        {
            ExecuteEvents.ExecuteHierarchy(transform.parent.gameObject, e, ExecuteEvents.pointerEnterHandler);
        }

        public void OnPointerExit(PointerEventData e)
        {
            ExecuteEvents.ExecuteHierarchy(transform.parent.gameObject, e, ExecuteEvents.pointerExitHandler);
        }

        // 拖曳開始時，補發 PointerExit 並將事件穿透給父層 ScrollRect
        public void OnBeginDrag(PointerEventData e)
        {
            ExecuteEvents.ExecuteHierarchy(transform.parent.gameObject, e, ExecuteEvents.beginDragHandler);
        }

        public void OnDrag(PointerEventData e)
            => ExecuteEvents.ExecuteHierarchy(transform.parent.gameObject, e, ExecuteEvents.dragHandler);

        public void OnEndDrag(PointerEventData e)
        {
            ExecuteEvents.ExecuteHierarchy(transform.parent.gameObject, e, ExecuteEvents.endDragHandler);
        }

        // 必須實作，否則 ScrollRect 不會收到 BeginDrag
        public void OnInitializePotentialDrag(PointerEventData e)
            => ExecuteEvents.ExecuteHierarchy(transform.parent.gameObject, e, ExecuteEvents.initializePotentialDrag);
    }
}