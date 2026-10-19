using System;
using System.Linq;
using VzDev.DebugUtils;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;
using VzDev.ApiExtensions;
using Gizmos = VzDev.ToolUtils.Gizmos;

namespace VzDev.RaycastUtils
{
    /// Raycast射線 互動處理
    public class RaycastManager_OLD : MonoBehaviour
    {

        #region Events 

        [Foldout("[Settings]"), SerializeField]  private Camera mainCamera;
        #endregion


        #region Fields
      
        private Transform _lastHoveredObject;
        private Vector3 _hitPoint;
        #endregion


        private void OnValidate()
        {
            if (mainCamera == null) mainCamera = Camera.main;
        }


        /// Debug畫線, default即可代表“使用目前滑鼠位置”
        private void DebugDrawLineCheck(Ray ray = default) => Gizmos.DrawRay(ray, rayDistance, Color.red, 1f);



        [Label("是否顯示RaycastHit經過的物件列表"), SerializeField]
        private bool isShowRaycastHitObjects;

        [Label("[資料項]"), SerializeField, ShowIf(nameof(isShowRaycastHitObjects))]
        private Transform[] raycastHitObjects;

        [Foldout("[Event] - RayCast目標物件/Hit點坐標(Click)")]
        public UnityEvent<Transform> onRaycastHitTargetWithClick = new();

        [Foldout("[Event] - RayCast目標物件/Hit點坐標(Click)")]
        public UnityEvent<Vector3> onRaycastHitPointWithClick = new();

        [Foldout("[Event] - RayCast目標物件/Hit點坐標(Click)")]
        public UnityEvent onRaycastHitClickEmpty = new();

        #region 模擬MouseOver/MouseExit

        private bool isSimulateMouseOverExit;

        [Foldout("[Event] - RayCast目標物件/Hit點坐標(MouseOver)"), ShowIf(nameof(isSimulateMouseOverExit))]
        public UnityEvent<Transform> onRaycastHitTarget = new();
        [Foldout("[Event] - RayCast目標物件/Hit點坐標(MouseOver)"), ShowIf(nameof(isSimulateMouseOverExit))]
        public UnityEvent<Vector3> onRaycastHitPoint = new();
        [Foldout("[Event] - RayCast目標物件/Hit點坐標(MouseOver)"), ShowIf(nameof(isSimulateMouseOverExit))]
        public UnityEvent<Transform, Vector3> onRaycastHitTargetAndPoint = new();


        [Foldout("[Event] - RayCast經過物件群(MouseOver/Click)"), ShowIf(nameof(isSimulateMouseOverExit))]
        public UnityEvent<Transform[]> onRaycastHitObjects, onRaycastHitObjectsWithClick = new();

        [Foldout("[Event] - RayCast目標物件群(最後MouseExit)"), ShowIf(nameof(isSimulateMouseOverExit))]
        public UnityEvent onMouseExitTarget = new();


        [Foldout("[設定]"), SerializeField] private int maxHitObjects = 10;
        [Foldout("[設定]"), SerializeField] private float rayDistance = 100f;
        [Foldout("[設定]"), SerializeField] private LayerMask targetLayerMask = ~0;
        [Foldout("[設定]"), SerializeField] private bool isDebugDrawLine = true;
        [Foldout("[設定]"), SerializeField, Label("位置指標(選填)")] private Transform hitPointIndicator;

   

        #endregion



        /// 從畫面中心發射射線，取得命中物件 (Viewport 0 ~ 1)
        private Transform[] GetRaycastHitObjectsFromCenter()
        {
            Ray viewportRay = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0)); // 中心射線
            return GetRaycastHitObjects(viewportRay);
        }

        /// 從指定螢幕座標（如滑鼠位置）發射射線 (Screen Resolution)
        private Transform[] GetRaycastHitObjectsFromScreen(Vector2 screenPosition)
        {
            Ray screenRay = mainCamera.ScreenPointToRay(screenPosition);
            return GetRaycastHitObjects(screenRay);
        }

        /// 取得RaycastHit並回傳排序後的物件列表
        private Transform[] GetRaycastHitObjects(Ray ray)
        {
            RaycastHit[] hits = new RaycastHit[maxHitObjects];
            int hitCount = Physics.RaycastNonAlloc(ray, hits, rayDistance, targetLayerMask);

            if (hitCount == 0)
            {
                _hitPoint = Vector3.zero;
                return Array.Empty<Transform>();
            }
            _hitPoint = hits[0].point;

            hits.SortByOriginDistance(hitCount);
            return hits.CloneToTransformArray(hitCount);
        }

        private void Update()
        {
            bool isClick = Input.GetMouseButtonDown(0);

            if (isClick && GetRaycastHitObjectsFromScreen(Input.mousePosition).Length == 0)
            {
                onRaycastHitClickEmpty?.Invoke();
            }


            // 無論是否模擬 MouseOver，只要點擊就要處理事件
            if (isSimulateMouseOverExit || isClick)
            {

                Vector3 screenPos = Input.mousePosition;
                if (float.IsNaN(screenPos.x) || float.IsNaN(screenPos.y) ||
                    float.IsInfinity(screenPos.x) || float.IsInfinity(screenPos.y))
                {
                    Debug.LogWarning($"Invalid screenPos: {screenPos}", this);
                    return;
                }

                raycastHitObjects = GetRaycastHitObjectsFromScreen(screenPos);
                if (raycastHitObjects.Length == 0)
                {
                    onMouseExitTarget?.Invoke();
                    return;
                }

                if (hitPointIndicator != null) hitPointIndicator.position = _hitPoint;

                Transform firstHitObject = raycastHitObjects.First();

                if (!EventHelper.IsPointerOverUI())
                {
                    // 模擬 MouseOver 邏輯
                    if (isSimulateMouseOverExit)
                    {
                        if (_lastHoveredObject != null && _lastHoveredObject != firstHitObject)
                        {
                            onMouseExitTarget?.Invoke();
                        }

                        if (firstHitObject != null)
                        {

                            onRaycastHitTarget?.Invoke(firstHitObject);
                            onRaycastHitObjects?.Invoke(raycastHitObjects);
                            onRaycastHitPoint?.Invoke(_hitPoint);
                            onRaycastHitTargetAndPoint?.Invoke(firstHitObject, _hitPoint);
                        }

                        _lastHoveredObject = firstHitObject;
                    }

                    // 點擊邏輯（不受 isSimulateMouseOver 限制）
                    if (isClick && firstHitObject != null)
                    {
                        onRaycastHitTargetWithClick?.Invoke(firstHitObject);
                        onRaycastHitObjectsWithClick?.Invoke(raycastHitObjects);
                        onRaycastHitPointWithClick?.Invoke(_hitPoint);
                        onRaycastHitTargetAndPoint?.Invoke(firstHitObject, _hitPoint);
                    }
                }
                else
                {
                    // 滑鼠在 UI 上，還原 _lastHoveredObject 狀態
                    if (_lastHoveredObject != null)
                    {
                        onMouseExitTarget?.Invoke(); //先註解，因為會反覆Invoke
                        _lastHoveredObject = null;
                    }
                }
            }

            if (isDebugDrawLine) DebugDrawLineCheck();
        }


    }
}