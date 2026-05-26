using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace VzDev.ScrollUtils
{
    /// 偵測ScrollRect停止事件
    [RequireComponent(typeof(ScrollRect))]
    public class ScrollRectEndDetector : MonoBehaviour, IEndDragHandler, IBeginDragHandler
    {
        [Foldout("[Events]")] public UnityEvent onScrollEnd;

        [Foldout("[Components]"), SerializeField]
        private ScrollRect scrollRect;

        private bool _isDragging = false;
        private bool _hasFiredEndEvent = true; // Default to true to prevent firing on start
        private const float VelocityThreshold = 0.1f;
        private const float SqrThreshold = VelocityThreshold * VelocityThreshold;

        private void Start() => OnValidate();

        private void OnValidate()
        {
            if (scrollRect == null) scrollRect = GetComponent<ScrollRect>();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            _isDragging = true;
            _hasFiredEndEvent = false;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            _isDragging = false;

            // If inertia is disabled and content is within bounds, end immediately
            if (!scrollRect.inertia && IsContentRelaxed())
            {
                FireEndEvent();
            }
        }

        private void Update()
        {
            // If user is actively dragging or event already fired, skip update
            if (_isDragging || _hasFiredEndEvent) return;

            // Monitor both velocity (Inertia) and position adjustment (Elastic snapback)
            if (scrollRect.velocity.sqrMagnitude <= SqrThreshold && IsContentRelaxed())
            {
                FireEndEvent();
            }
        }

        private void FireEndEvent()
        {
            _hasFiredEndEvent = true;
            scrollRect.velocity = Vector2.zero; // Clean snap
            onScrollEnd?.Invoke();
        }

        /// Checks if the content has finished its elastic snapback behavior.
        private bool IsContentRelaxed()
        {
            if (scrollRect.movementType != ScrollRect.MovementType.Elastic) return true;

            // When Elastic snapback is running, Unity forces velocity even without inertia.
            // If velocity is effectively zero, the content has returned to its restricted bounds.
            return scrollRect.velocity.sqrMagnitude <= SqrThreshold;
        }

        private void OnDisable()
        {
            _isDragging = false;
            _hasFiredEndEvent = true;
        }
    }
}