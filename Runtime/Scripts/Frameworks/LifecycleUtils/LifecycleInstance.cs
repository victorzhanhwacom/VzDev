using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;

namespace VzDev.Frameworks.LifecycleUtils
{
    /// <summary>
    /// 個體生命週期轉發器。掛載於單一 GameObject 上，
    /// 依 Unity 生命週期各階段 Invoke 對應的 UnityEvent，
    /// 供 Inspector 手動連線至特定目標物件的方法（例如觸發 UIFadeEffect.Show/Hide）。
    /// 與 GlobalLifecycleBroadcaster 職責互不重疊：
    /// 此類別用於「特定Controller對應特定目標」的一對一/一對少數路由，
    /// 不適合用於大量物件共用的全域廣播情境。
    /// </summary>
    public class LifecycleInstance : MonoBehaviour
    {
        #region Fields
        [Foldout("[Events-Awake]"), SerializeField] private UnityEvent onAwakeEvent;
        [Foldout("[Events-Start]"), SerializeField] private UnityEvent onStartEvent;
        [Foldout("[Events-Enable]"), SerializeField] private UnityEvent<bool> isEnableEvent;
        [Foldout("[Events-Enable]"), SerializeField] private UnityEvent onEnableEvent;
        [Foldout("[Events-Disable]"), SerializeField] private UnityEvent onDisableEvent;
        [Foldout("[Events-Destroy]"), SerializeField] private UnityEvent onDestroyEvent;

        [Foldout("[Settings]"), SerializeField, Tooltip("是否轉發 Update，預設關閉避免不必要的效能開銷")]
        private bool enableUpdateEvent = false;
        [Foldout("[Events-Update]"), SerializeField, ShowIf("enableUpdateEvent")]
        private UnityEvent onUpdateEvent;
        #endregion

        private void Awake() => onAwakeEvent?.Invoke();
        private void Start() => onStartEvent?.Invoke();
        private void OnEnable()
        {
            onEnableEvent?.Invoke();
            isEnableEvent?.Invoke(true);
        }

        private void OnDisable()
        {
            onDisableEvent?.Invoke();
            isEnableEvent?.Invoke(false);
        }
        private void OnDestroy() => onDestroyEvent?.Invoke();

        private void Update()
        {
            if (!enableUpdateEvent) return;
            onUpdateEvent?.Invoke();
        }
    }
}