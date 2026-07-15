using UnityEngine;
using NaughtyAttributes;
using System;

namespace VzDev.Frameworks.LifecycleUtils
{
    /// <summary>
    /// 全域生命週期廣播器。場景中只能存在一個 instance，
    /// 透過 static event 統一廣播 Unity 生命週期階段，
    /// 供大量物件共用同一次觸發，避免逐一掛 Update 的呼叫開銷。
    /// 注意：訂閱端必須在 OnEnable/OnDisable 對稱訂閱/取消訂閱，
    /// 否則 static event 持有的 delegate 引用不會隨場景卸載自動清除。
    /// </summary>
    public class GlobalLifecycleBroadcaster : MonoBehaviour
    {
        #region Fields
        [InfoBox("全域生命週期廣播器，透過 static event 統一廣播 Unity 生命週期階段")]
        [SerializeField, ReadOnly, Tooltip("是否有重複的 instance")] private bool isDuplicate = false;
        #endregion

        #region Static Events
        public static event Action OnGlobalAwake;
        public static event Action OnGlobalStart;
        public static event Action OnGlobalEnable;
        public static event Action OnGlobalDisable;
        public static event Action OnGlobalUpdate;
        public static event Action OnGlobalLateUpdate;
        public static event Action OnGlobalFixedUpdate;
        #endregion

        private static GlobalLifecycleBroadcaster instanceRef;

        private void Awake()
        {
            if (instanceRef != null)
            {
                Debug.LogError(
                    $"{nameof(GlobalLifecycleBroadcaster)} 場景上重複存在，" +
                    $"此 instance 將被銷毀：{gameObject.name}", this);
                isDuplicate = true;
                Destroy(gameObject);
                return;
            }
            instanceRef = this;

            OnGlobalAwake?.Invoke();
        }

        private void Start()
        {
            if (isDuplicate) return;
            OnGlobalStart?.Invoke();
        }

        private void OnEnable()
        {
            if (isDuplicate) return;
            OnGlobalEnable?.Invoke();
        }

        private void OnDisable()
        {
            if (isDuplicate) return;
            OnGlobalDisable?.Invoke();
        }

        private void Update()
        {
            if (isDuplicate) return;
            OnGlobalUpdate?.Invoke();
        }

        private void LateUpdate()
        {
            if (isDuplicate) return;
            OnGlobalLateUpdate?.Invoke();
        }

        private void FixedUpdate()
        {
            if (isDuplicate) return;
            OnGlobalFixedUpdate?.Invoke();
        }

        /// <summary>
        /// 場景卸載/物件銷毀時，主動清空所有 static event 上的殘留訂閱，
        /// 避免下個場景載入新的 instance 後，舊訂閱端的 delegate 仍殘留於事件上，
        /// 呼叫到已銷毀物件而拋出 MissingReferenceException。
        /// </summary>
        private void OnDestroy()
        {
            if (isDuplicate) return;
            if (instanceRef == this) instanceRef = null;

            OnGlobalAwake = null;
            OnGlobalStart = null;
            OnGlobalEnable = null;
            OnGlobalDisable = null;
            OnGlobalUpdate = null;
            OnGlobalLateUpdate = null;
            OnGlobalFixedUpdate = null;
        }
    }
}
