using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using VzDev.ApiExtensions;

namespace VzDev.RaycastUtils
{
    /// <summary>
    /// Raycast管理器，提供點擊和滑鼠懸停事件的處理。
    /// 注意：事件傳出的 List 為內部重複使用的實例，請勿保留參考；需要保存請自行複製。
    /// </summary>
    public class RaycastManager : MonoBehaviour
    {
        #region Events

        [Foldout("[Event]-Click"), Tooltip("Hit&Click到的第一個物件")] public UnityEvent<Transform> OnClickTargetAction;
        [Foldout("[Event]-Click"), Tooltip("Hit&Click到的物件列表")] public UnityEvent<List<Transform>> OnClickTargetsAction;
        [Foldout("[Event]-Hover"), Tooltip("Hit到的第一個物件（離開時為 null）")] public UnityEvent<Transform> OnHoverTargetAction;
        [Foldout("[Event]-Hover"), Tooltip("Hit到的物件列表（離開時為空列表）")] public UnityEvent<List<Transform>> OnHoverTargetsAction;
        [Foldout("[Event]-Hover"), Tooltip("離開的物件（可能已被 Destroy，使用前請 null 檢查）")] public UnityEvent<Transform> OnHoverExitAction;

        #endregion

        #region Fields
        [Foldout("[Settings]"), SerializeField, Min(1)] private int maxHitObjects = 10;
        [Foldout("[Settings]"), SerializeField, Min(1), Tooltip("RaycastNonAlloc 緩衝大小，需 ≥ maxHitObjects，否則可能漏掉較近的物件")]
        private int hitBufferSize = 32;
        [Foldout("[Settings]"), SerializeField] private float rayDistance = 100f;
        [Foldout("[Settings]"), SerializeField] private LayerMask targetLayerMask = ~0;
        [Foldout("[Settings]"), SerializeField] private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore;
        [Foldout("[Settings]"), SerializeField] private bool blockedByUI = true;
        [Foldout("[Settings]"), SerializeField] private Camera mainCamera;
        [Foldout("[Settings]"), SerializeField] private bool drawDebugRay = true;

        [Foldout("[IRaycastTarget]"), SerializeField, Tooltip("true：hit 列表中所有實作 IRaycastTarget 的物件都會收到通知；false：只通知最近的一個")]
        private bool notifyAllHits = true;

        [Foldout("[HitResult]"), SerializeField, ReadOnly] private List<Transform> clickedObjects = new List<Transform>();
        [Foldout("[HitResult]"), SerializeField, ReadOnly] private List<Transform> hoveredObjects = new List<Transform>();

        private const int TargetCacheLimit = 1024;

        private struct HitTarget
        {
            public IRaycastTarget Target;
            public int HitIndex;
        }

        private RaycastHit[] _hitBuffer;
        private int _hitCount;
        private Ray _ray;
        private Transform _lastHoveredObject;

        // Collider InstanceID → IRaycastTarget（null 也快取，代表該 Collider 沒有實作）
        private readonly Dictionary<int, IRaycastTarget> _targetCache = new Dictionary<int, IRaycastTarget>(64);
        private List<HitTarget> _hoveredTargets = new List<HitTarget>(16); // 上一幀
        private List<HitTarget> _currentTargets = new List<HitTarget>(16); // 這一幀

        #endregion

        private void Awake()
        {
            if (mainCamera == null) mainCamera = Camera.main;
            _hitBuffer = new RaycastHit[Mathf.Max(hitBufferSize, maxHitObjects)];
        }

        private void OnValidate()
        {
            if (mainCamera == null) mainCamera = Camera.main;
            if (hitBufferSize < maxHitObjects) hitBufferSize = maxHitObjects;
        }

        private void OnDisable()
        {
            // 停用時結束 hover，避免高亮殘留
            _hitCount = 0;
            HandleHover();
            HandleTargetHover();
        }

        private void Update()
        {
            if (mainCamera == null) return;
            HandleRaycastHit();
            HandleHover();
            HandleTargetHover();   // 先 Enter 再 Click，與 UGUI 順序一致
            HandleClickEvents();
            DrawDebugRay();
        }

        /// <summary>
        /// 執行期在物件上新增 / 移除 IRaycastTarget 元件後呼叫，清除查找快取。
        /// </summary>
        public void ClearTargetCache() => _targetCache.Clear();

        /// <summary>
        /// 處理RaycastHit，結果存於 _hitBuffer[0.._hitCount)，依距離排序
        /// </summary>
        private void HandleRaycastHit()
        {
            _ray = mainCamera.ScreenPointToRay(Input.mousePosition);

            // 是否被UI阻擋，若是則不進行射線檢測
            if (blockedByUI && IsPointerOverUI())
            {
                _hitCount = 0;
                return;
            }

            // 進行射線檢測，使用 RaycastNonAlloc 避免 GC
            int count = Physics.RaycastNonAlloc(_ray, _hitBuffer, rayDistance, targetLayerMask, triggerInteraction);

            // 插入排序：n 很小時最快，且零 GC
            for (int i = 1; i < count; i++)
            {
                RaycastHit key = _hitBuffer[i];
                int j = i - 1;
                while (j >= 0 && _hitBuffer[j].distance > key.distance)
                {
                    _hitBuffer[j + 1] = _hitBuffer[j];
                    j--;
                }
                _hitBuffer[j + 1] = key;
            }

            _hitCount = count < maxHitObjects ? count : maxHitObjects;
        }

        /// <summary>
        /// 處理點擊動作
        /// </summary>
        private void HandleClickEvents()
        {
            if (_hitCount == 0 || !Input.GetMouseButtonDown(0)) return;

            FillTransforms(clickedObjects);
            OnClickTargetAction?.Invoke(clickedObjects[0]);
            OnClickTargetsAction?.Invoke(clickedObjects);

            // HandleTargetHover 已在本幀收集好目標（存在 _hoveredTargets）
            for (int i = 0; i < _hoveredTargets.Count; i++)
            {
                HitTarget t = _hoveredTargets[i];
                if (CanInvoke(t.Target))
                    t.Target.OnRaycastClick(in _hitBuffer[t.HitIndex], t.HitIndex);
            }
        }

        /// <summary>
        /// 處理Hover動作（Enter / Exit）
        /// </summary>
        private void HandleHover()
        {
            Transform current = _hitCount > 0 ? GetHitTransform(0) : null;

            // ReferenceEquals：舊目標被 Destroy 時也能正確送出 Exit
            if (ReferenceEquals(current, _lastHoveredObject))
            {
                // 第一個沒變，只同步列表內容（零 GC），不觸發事件
                if (current != null) FillTransforms(hoveredObjects);
                return;
            }

            if (!ReferenceEquals(_lastHoveredObject, null))
            {
                Transform exited = _lastHoveredObject;
                _lastHoveredObject = null;
                hoveredObjects.Clear();

                OnHoverExitAction?.Invoke(exited);
                if (current == null)
                {
                    OnHoverTargetAction?.Invoke(null);
                    OnHoverTargetsAction?.Invoke(hoveredObjects);
                }
            }

            if (current != null)
            {
                _lastHoveredObject = current;
                FillTransforms(hoveredObjects);
                OnHoverTargetAction?.Invoke(current);
                OnHoverTargetsAction?.Invoke(hoveredObjects);
            }
        }

        /// <summary>
        /// 處理 IRaycastTarget 的 Hover：比對上一幀與這一幀的目標集合，送出 Exit / Enter
        /// </summary>
        private void HandleTargetHover()
        {
            CollectTargets(_currentTargets);

            // 先 Exit：上一幀有、這一幀沒有
            for (int i = 0; i < _hoveredTargets.Count; i++)
            {
                IRaycastTarget prev = _hoveredTargets[i].Target;
                if (IndexOf(_currentTargets, prev) < 0 && CanInvoke(prev))
                    prev.OnRaycastHoverExit();
            }

            // 再 Enter：這一幀有、上一幀沒有
            for (int i = 0; i < _currentTargets.Count; i++)
            {
                HitTarget cur = _currentTargets[i];
                if (IndexOf(_hoveredTargets, cur.Target) < 0 && CanInvoke(cur.Target))
                    cur.Target.OnRaycastHoverEnter(in _hitBuffer[cur.HitIndex], cur.HitIndex);
            }

            // 交換兩個 List，避免配置
            (_hoveredTargets, _currentTargets) = (_currentTargets, _hoveredTargets);
        }

        /// <summary>
        /// 從 hit buffer 收集實作 IRaycastTarget 的目標（已去重，同一目標保留最近的 hitIndex）
        /// </summary>
        private void CollectTargets(List<HitTarget> result)
        {
            result.Clear();
            int limit = notifyAllHits ? _hitCount : (_hitCount > 0 ? 1 : 0);

            for (int i = 0; i < limit; i++)
            {
                IRaycastTarget target = GetTarget(_hitBuffer[i].collider);
                if (target == null) continue;
                if (IndexOf(result, target) >= 0) continue; // 同一目標有多個子 Collider

                result.Add(new HitTarget { Target = target, HitIndex = i });
            }
        }

        private IRaycastTarget GetTarget(Collider col)
        {
            int id = col.GetInstanceID();

            if (_targetCache.TryGetValue(id, out IRaycastTarget cached))
            {
                // null（沒有實作）或仍存活 → 直接用；已被 Destroy（fake null）→ 重新查找
                if (ReferenceEquals(cached, null) || (cached as UnityEngine.Object) != null)
                    return cached;
            }

            if (_targetCache.Count >= TargetCacheLimit) _targetCache.Clear();

            IRaycastTarget target = col.GetComponentInParent<IRaycastTarget>();
            _targetCache[id] = target;
            return target;
        }

        // 小集合（≤ maxHitObjects）線性搜尋比 HashSet 更快，且零 GC
        private static int IndexOf(List<HitTarget> list, IRaycastTarget target)
        {
            for (int i = 0; i < list.Count; i++)
                if (ReferenceEquals(list[i].Target, target)) return i;
            return -1;
        }

        // 已 Destroy 或停用的元件不通知（與 UGUI EventSystem 行為一致）
        private static bool CanInvoke(IRaycastTarget target)
        {
            return target is Behaviour b && b != null && b.isActiveAndEnabled;
        }

        /// <summary>
        /// 依照hitbuffer內容，填充Transform列表，並清空原有內容。列表為內部重複使用的實例，請勿保留參考；需要保存請自行複製。
        /// </summary>
        private void FillTransforms(List<Transform> list)
        {
            list.Clear();
            for (int i = 0; i < _hitCount; i++) list.Add(GetHitTransform(i));
        }

        // 用 collider.transform：hit.transform 在有 Rigidbody 時會回傳 Rigidbody 的物件
        private Transform GetHitTransform(int index) => _hitBuffer[index].collider.transform;

        private static bool IsPointerOverUI()
        {
            EventSystem es = EventSystem.current;
            return es != null && es.IsPointerOverGameObject();
        }

        /// <summary>
        /// 在Unity Editor中繪製射線和命中點的調試信息
        /// </summary>
        private void DrawDebugRay()
        {
#if UNITY_EDITOR
            if (!drawDebugRay) return;

            if (_hitCount > 0)
            {
                Vector3 point = _hitBuffer[0].point;
                Debug.DrawLine(_ray.origin, point, Color.blue);
                point.DrawCircle(0.2f, Color.blue);
                point.DrawCircle(0.1f, Color.red);
                _ray.origin.DrawWireSphere(0.1f, Color.blue);
            }
            else
            {
                Debug.DrawRay(_ray.origin, _ray.direction * rayDistance, Color.gray);
                _ray.origin.DrawWireSphere(0.1f, Color.gray);
            }
#endif
        }
    }
}