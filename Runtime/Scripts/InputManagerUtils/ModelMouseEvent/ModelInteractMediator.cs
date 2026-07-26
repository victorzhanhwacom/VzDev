using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;
using Debug = VzDev.ToolUtils.Debug;

namespace VzDev.InteractiveUtils.ModelMouseEvent
{
    /// <summary>
    /// 中介者：接收 ColliderInteractionSystem 的滑鼠事件，
    /// 轉呼叫給物件身上實作的對應介面，不涉及任何業務邏輯判斷。
    /// </summary>
    public class ModelInteractMediator : MonoBehaviour
    {
        [SerializeField] private bool logClickEnabled = false;
        [SerializeField] private bool logHoverEnabled = false;
        [SerializeField] private bool logDragEnabled = false;

        [Foldout("[Events]")] public UnityEvent<Transform> OnModelSelected;
        [Foldout("[Events]")] public UnityEvent<Transform> OnModelDeselected;
        private Transform lastSelectedModel;

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
            if (lastSelectedModel != null)
            {
                if (lastSelectedModel == targetObject.transform)
                {
                    OnModelDeselected?.Invoke(lastSelectedModel);
                    lastSelectedModel = null;
                    return;
                }
                else
                {
                    OnModelDeselected?.Invoke(lastSelectedModel);
                    lastSelectedModel = targetObject.transform;
                    OnModelSelected?.Invoke(lastSelectedModel);
                }
            }
            else
            {
                lastSelectedModel = targetObject.transform;
                OnModelSelected?.Invoke(lastSelectedModel);
            }

            if (targetObject.TryGetComponent<IModelClick>(out var handler))
            {
                handler.OnModelClicked(targetObject);
                Debug.Assert(!logClickEnabled, $"Model Clicked: {targetObject.name}");
            }
        }
        public void CancelSelection()
        {
            if (lastSelectedModel != null)
            {
                OnModelDeselected?.Invoke(lastSelectedModel);
                lastSelectedModel = null;
            }
        }

        private void HandleModelEnter(GameObject targetObject)
        {
            if (targetObject.TryGetComponent<IModelHover>(out var handler))
            {
                handler.OnHoverEnter(targetObject);
                Debug.Assert(!logHoverEnabled, $"Hover Enter: {targetObject.name}");
            }
        }

        private void HandleModelExit(GameObject targetObject)
        {
            if (targetObject.TryGetComponent<IModelHover>(out var handler))
            {
                handler.OnHoverExit(targetObject);
                Debug.Assert(!logHoverEnabled, $"Hover Exit: {targetObject.name}");
            }
        }

        private void HandleModelDrag(GameObject targetObject, Vector3 point)
        {
            if (targetObject.TryGetComponent<IModelDrag>(out var handler))
            {
                handler.OnMouseDrag(targetObject, point);
                Debug.Assert(!logDragEnabled, $"Mouse Drag: {targetObject.name}");
            }
        }

        private void HandleModelRelease(GameObject targetObject)
        {
            if (targetObject.TryGetComponent<IModelDrag>(out var handler))
            {
                handler.OnMouseRelease(targetObject);
                Debug.Assert(!logDragEnabled, $"Mouse Release: {targetObject.name}");
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