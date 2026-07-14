using UnityEngine;
using Debug = VzDev.ToolUtils.Debug;

namespace VzDev.InteractiveUtils.ModelMouseEvent
{
    /// <summary>
    /// 中介者：接收 ColliderInteractionSystem 的滑鼠事件，
    /// 轉呼叫給物件身上實作的對應介面，不涉及任何業務邏輯判斷。
    /// </summary>
    public class ModelInteractMediator : MonoBehaviour
    {
        [SerializeField] private bool logEnabled = false;
        
        private void OnEnable()
        {
            ColliderInteractionSystem.OnMouseClick += HandleModelClick;
            ColliderInteractionSystem.OnMouseEnter += HandleModelEnter;
            ColliderInteractionSystem.OnMouseExit += HandleModelExit;
            ColliderInteractionSystem.OnMouseDrag += HandleModelDrag;
            ColliderInteractionSystem.OnMouseRelease += HandleModelRelease;
        }

        private void OnDisable()
        {
            ColliderInteractionSystem.OnMouseClick -= HandleModelClick;
            ColliderInteractionSystem.OnMouseEnter -= HandleModelEnter;
            ColliderInteractionSystem.OnMouseExit -= HandleModelExit;
            ColliderInteractionSystem.OnMouseDrag -= HandleModelDrag;
            ColliderInteractionSystem.OnMouseRelease -= HandleModelRelease;
        }

        private void HandleModelClick(GameObject targetObject)
        {
            if (targetObject.TryGetComponent<IModelClick>(out var handler))
            {
                handler.OnModelClicked(targetObject);
                Debug.TryLog(logEnabled, $"Model Clicked: {targetObject.name}", this);
            }
        }

        private void HandleModelEnter(GameObject targetObject)
        {
            if (targetObject.TryGetComponent<IModelHover>(out var handler))
            {
                handler.OnHoverEnter(targetObject);
                Debug.TryLog(logEnabled, $"Hover Enter: {targetObject.name}", this);
            }
        }

        private void HandleModelExit(GameObject targetObject)
        {
            if (targetObject.TryGetComponent<IModelHover>(out var handler))
            {
                handler.OnHoverExit(targetObject);
                Debug.TryLog(logEnabled, $"Hover Exit: {targetObject.name}", this);
            }
        }

        private void HandleModelDrag(GameObject targetObject, Vector3 point)
        {
            if (targetObject.TryGetComponent<IModelDrag>(out var handler))
            {
                handler.OnMouseDrag(targetObject, point);
                Debug.TryLog(logEnabled, $"Mouse Drag: {targetObject.name}", this);
            }
        }

        private void HandleModelRelease(GameObject targetObject)
        {
            if (targetObject.TryGetComponent<IModelDrag>(out var handler))
            {
                handler.OnMouseRelease(targetObject);
                Debug.TryLog(logEnabled, $"Mouse Release: {targetObject.name}", this);
            }
        }
    }

    #region Interfaces 欲被點擊對像需實作的介面
    public interface IModelClick
    {
        void OnModelClicked(GameObject clickedObject);
    }
    public interface IModelHover
    {
        void OnHoverEnter(GameObject targetObject);
        void OnHoverExit(GameObject targetObject);
    }
    public interface IModelDrag
    {
        void OnMouseDrag(GameObject targetObject, Vector3 worldPoint);
        void OnMouseRelease(GameObject targetObject);
    }
    #endregion
}