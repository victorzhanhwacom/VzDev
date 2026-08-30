using NaughtyAttributes;
using UnityEngine;
using UnityEngine.EventSystems;
using VzDev.Helpers;

namespace VzDev.InteractiveUtils.ModelMouseEvent
{
    /// <summary>
    /// 集中式滑鼠互動管理器，取代逐一掛在物件上的 OnMouseDown/OnMouseEnter。
    /// 適合大量設備/U槽場景，效能優化重點：
    ///   1. RaycastNonAlloc：避免每幀 GC Allocation
    ///   2. LayerMask：只偵測可互動物件Collider
    /// </summary>
    public class ColliderInteractionSystem : MonoBehaviour
    {
        #region Fields
        [SerializeField, ReadOnly] private GameObject currentHover;
        [SerializeField, ReadOnly] private GameObject dragTarget;
        [Foldout("[Components]"), SerializeField] private Camera mainCamera;
        [Foldout("[Settings]"), SerializeField, Tooltip("模型對像篩選")] public LayerMask interactableLayer;
        [Foldout("[Settings]"), SerializeField] public float maxDistance = 100f;
        private readonly RaycastHit[] _hitBuffer = new RaycastHit[8];
        #endregion

        #region Events
        // 給外部模組訂閱的事件
        public static event System.Action<GameObject> OnMouseEnter;
        public static event System.Action<GameObject> OnMouseExit;
        public static event System.Action<GameObject> OnMouseClick;
        public static event System.Action<GameObject, Vector3> OnMouseDrag;
        public static event System.Action<GameObject> OnMouseRelease;
        public static event System.Action OnMouseClickEmpty;
        #endregion

        void Awake()
        {
            OnValidate();
            if (EventSystem.current == null)
            {
                Debug.LogWarning("EventSystem is missing in the scene. Please add an EventSystem to handle UI interactions.", this);
            }
        }
        private void OnValidate()
        {
            if (mainCamera == null)
                mainCamera = Camera.main;
        }

        /// <summary>
        /// 失焦瞬間立刻清空 hover / drag 狀態，避免切回來後殘留舊的高亮/拖曳狀態。
        /// </summary>
        private void Update()
        {
            if (!Application.isFocused || MouseHelper.IsPointerOutOfScreen) return;   // 失焦時不處理滑鼠事件，避免誤觸

            // 擋掉 UI 上的滑鼠事件，避免誤觸 3D 物件
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                ClearHover();
                dragTarget = null;
                return;
            }

            // 如果暫時關閉 MouseClick，則不處理滑鼠事件，避免誤觸 3D 物件
            /* if (isMouseInteractable == false)
            {
                return;
            } */

            GameObject hitObj = TryGetHitObject();

            HandleHover(hitObj);

            if (Input.GetMouseButtonDown(0) && isMouseInteractable)
            {
                if (hitObj != null)
                {
                    dragTarget = hitObj;
                    OnMouseClick?.Invoke(hitObj);
                }
                else
                {
                    OnMouseClickEmpty?.Invoke();
                }
            }

            if (Input.GetMouseButton(0) && dragTarget != null)
            {
                Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit dragHit, maxDistance, interactableLayer))
                {
                    OnMouseDrag?.Invoke(dragTarget, dragHit.point);
                }
            }

            if (Input.GetMouseButtonUp(0) && dragTarget != null)
            {
                OnMouseRelease?.Invoke(dragTarget);
                dragTarget = null;
            }
        }

        /// <summary>
        /// 用 RaycastNonAlloc 取代 Raycast，避免 GC Allocation。
        /// 只在 interactableLayer 範圍內偵測，並依距離排序找最近的一個。
        /// </summary>
        private GameObject TryGetHitObject()
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

            int hitCount = Physics.RaycastNonAlloc(
                ray,
                _hitBuffer,
                maxDistance,
                interactableLayer
            );

            if (hitCount == 0)
                return null;

            // 找出距離最近的碰撞結果（所有結果都已經在 maxDistance 範圍內）
            int closestIndex = 0;
            float closestDistance = float.MaxValue;
            for (int i = 0; i < hitCount; i++)
            {
                if (_hitBuffer[i].distance < closestDistance)
                {
                    closestDistance = _hitBuffer[i].distance;
                    closestIndex = i;
                }
            }

            return _hitBuffer[closestIndex].collider.gameObject;
        }

        private void HandleHover(GameObject hitObj)
        {
            if (hitObj != currentHover)
            {
                if (currentHover != null) OnMouseExit?.Invoke(currentHover);
                if (hitObj != null) OnMouseEnter?.Invoke(hitObj);
                currentHover = hitObj;
            }
        }
        private void ClearHover()
        {
            if (currentHover != null)
            {
                OnMouseExit?.Invoke(currentHover);
                currentHover = null;
            }
        }

        #region 設定是否允許MouseClick
        private static bool isMouseInteractable = true;
        public static void SetMouseInteractable(bool isInteractable) => isMouseInteractable = isInteractable;
        #endregion


        #region 供外部模擬觸發（例如 UI Toggle 點位標籤系統）
        /// <summary>
        /// 供外部（例如 ModelToggleBinding）模擬一次「點擊此模型」，
        /// 完整重用既有的 OnMouseClick 事件管線（SelectionController 高亮、
        /// ModelInteractMediator 轉發、AssetDataDisplayDispatcher 面板顯示…），
        /// 不需要在呼叫端重複這些邏輯。
        /// 事件只能在宣告的類別內部 Invoke，這是唯一合法的外部觸發入口。
        /// </summary>
        public static void SimulateClick(GameObject target)
        {
            if (!isMouseInteractable) return; // 如果暫時關閉 MouseClick，則不觸發事件
            if (target == null) return;
            OnMouseClick?.Invoke(target);
        }

        /// <summary>
        /// 供外部（例如 ModelToggleBinding 在 allowSwitchOff 情境下）模擬一次
        /// 「點擊空白處」，完整重用既有的 OnMouseClickEmpty 事件管線
        /// （SelectionController 清空選取、AssetDataDisplayDispatcher 關閉面板…）。
        /// </summary>
        public static void SimulateClickEmpty()
        {
            if (!isMouseInteractable) return; // 如果暫時關閉 MouseClick，則不觸發事件
            OnMouseClickEmpty?.Invoke();
        }
        #endregion
    }
}