using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;
using VzDev.ApiExtensions;
using VzDev.Helpers;
using VzDev.UnityAPI.Extensions;

namespace VzDev
{
    /// <summary>
    /// 偵測點擊是否落在指定 UI 範圍內。預設停用，由外部 SetDetecting() 或 enabled 控制，停用時零成本。
    /// </summary>
    [DefaultExecutionOrder(-1000)]   // 在 EventSystem 和其他 UI 腳本之前執行，先記下點擊當下的狀態
    public class MouseClickUIDetector : MonoBehaviour
    {
        #region Events
        [Foldout("[Events]")] public UnityEvent<bool> isClickedUIEvent = new();
        [Foldout("[Events]")] public UnityEvent onClickedUIEvent = new();
        [Foldout("[Events]")] public UnityEvent onClickedOutsideUIEvent = new();
        #endregion

        #region Fields
        [ReadOnly, SerializeField] private bool isClickedUI = false;
        [Foldout("[Settings]"), SerializeField] private bool detectIncludChildren = true;
        [Foldout("[Components]"), SerializeField] private RectTransform[] uiRects;
        private Camera uiCamera;
        #endregion

        private int _enabledFrame = -1;

        private void Awake()
        {
            // 相機一律在執行時重新取得，不依賴編輯模式下存的值
            var canvas = GetComponentInParent<Canvas>();
            if (canvas != null) uiCamera = canvas.GetUICamera();
            OnValidate();
        }

        private void OnValidate()
        {
            if (uiRects == null || uiRects.Length == 0)
                uiRects = new RectTransform[] { GetComponent<RectTransform>() };
        }


        private void OnEnable() => _enabledFrame = Time.frameCount;

        private void Update()
        {
            if (Time.frameCount == _enabledFrame) return;                  // 跳過啟用當幀
            if (!MouseHelper.TryGetPointerDown(out var pos)) return;

            isClickedUI = uiRects != null && uiRects.ContainsScreenPoint(pos, uiCamera, detectIncludChildren);
            isClickedUIEvent.Invoke(isClickedUI);
            (isClickedUI ? onClickedUIEvent : onClickedOutsideUIEvent).Invoke();
        }
    }
}