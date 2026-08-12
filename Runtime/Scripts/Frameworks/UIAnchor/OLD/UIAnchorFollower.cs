using NaughtyAttributes;
using UnityEngine;
using VzDev.Frameworks.LifecycleUtils;
using VzDev.UnityAPI.Extensions;

namespace VzDev.ObjectUtils
{
    /// <summary>
    /// 將 UI 物件的 2D 錨點 (Anchor) 鎖定並跟隨指定的 3D 世界物件座標。
    ///
    /// 【效能修正】原本每個實例各自掛 Update()，數量一多（機櫃感測點 200+）
    /// 造成大量逐一的 Update 訊息分派開銷。改為訂閱 GlobalLifecycleBroadcaster.OnGlobalUpdate，
    /// 全部實例共用同一次觸發，不需要另外維護 Registry。
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class UIAnchorFollower : MonoBehaviour
    {
        /// 依指定3D物件之座標，轉成螢幕2D座標進行定位
        /// + 因為跟攝影機視角有關，所以不能直接加offset偏移值於座標上
        #region Variables

        [Label("定位目標物件"), SerializeField, Required] private Transform target3DObject;

        [Foldout("[Settings]"), SerializeField] private Vector3 offsetPos = Vector3.up * 0.1f;

        [Foldout("[Settings]"), SerializeField] private bool isAlwaysVisible = false;
        [Foldout("[Settings]"), SerializeField, HideIf(nameof(isAlwaysVisible))] private float visibleRange = 20f;
        [Foldout("[Settings]"), SerializeField, HideIf(nameof(isAlwaysVisible))] private bool visibleReverse = false;

        [Foldout("[Components]"), SerializeField] private Camera mainCamera;
        [Foldout("[Components]"), SerializeField] private RectTransform rectTrans, canvasRect;
        [Foldout("[Components]"), SerializeField] private GameObject container;

        public Transform Target3DObject => target3DObject;

        public float DistanceFromCamera => Vector3.Distance(mainCamera.transform.position, target3DObject.position);

        // 快取平方後的可視距離，Tick() 內比較時避免每幀重複做乘法
        private float visibleRangeSqr;

        // 上一次寫入的狀態，避免同值重複觸發 SetActive / anchoredPosition 寫入
        private bool lastActive;
        private Vector2 lastAnchoredPos;
        private bool hasLastAnchoredPos;

        #endregion

        private void Awake()
        {
            RefreshVisibleRangeSqr();
            OnValidate();
        }

        private void OnEnable()
        {
            GlobalLifecycleBroadcaster.OnGlobalUpdate += Tick;
        }

        private void OnDisable()
        {
            GlobalLifecycleBroadcaster.OnGlobalUpdate -= Tick;

            // 場景卸載/物件停用時，主動收起顯示，避免下次啟用前殘留舊狀態
            if (container != null && container.activeSelf)
            {
                container.SetActive(false);
                lastActive = false;
            }
            hasLastAnchoredPos = false;
        }

        private void Tick()
        {
            if (target3DObject == null || mainCamera == null) return;

            Vector3 targetPos = target3DObject.position;

            bool inRange = isAlwaysVisible ||
                (targetPos - mainCamera.transform.position).sqrMagnitude <= visibleRangeSqr;
            if (!isAlwaysVisible && visibleReverse) inRange = !inRange;

            Vector3 screenPos = mainCamera.WorldToScreenPoint(target3DObject.GetModelBoundsCenter() + offsetPos);
            bool isInFrontOfCamera = screenPos.z > 0f;

            bool visible = inRange && isInFrontOfCamera && target3DObject.gameObject.activeInHierarchy;

            if (visible != lastActive)
            {
                container.SetActive(visible);
                lastActive = visible;
            }
            if (!visible) return;

            // 2. 取得 Canvas 尺寸（用 rect.size，滿版 Canvas 下 sizeDelta 會是 (0,0)）
            Vector2 canvasSize = canvasRect.rect.size;
            float scaleX = canvasSize.x / Screen.width;
            float scaleY = canvasSize.y / Screen.height;

            // 3. 計算相對於 Canvas 的座標
            Vector2 localPos = new Vector2(
                (screenPos.x - (Screen.width * 0.5f)) * scaleX,
                (screenPos.y - (Screen.height * 0.5f)) * scaleY
            );

            // 4. 設定 UI 位置（相同座標不重複寫入，減少不必要的 Layout/Canvas 更新）
            if (!hasLastAnchoredPos || localPos != lastAnchoredPos)
            {
                rectTrans.anchoredPosition = localPos;
                lastAnchoredPos = localPos;
                hasLastAnchoredPos = true;
            }
        }

        /// 設定目標物件
        public void SetTargetObject(Transform target) => target3DObject = target;

        /// 設定可視距離
        public void SetVisibleRange(float range)
        {
            visibleRange = range;
            RefreshVisibleRangeSqr();
        }

        private void RefreshVisibleRangeSqr() => visibleRangeSqr = visibleRange * visibleRange;

        [Button]
        public void OnValidate()
        {
            mainCamera = Camera.main;
            canvasRect = transform.GetComponentInParent<Canvas>(true)?.rootCanvas.transform as RectTransform;

            rectTrans = transform as RectTransform;
            container = transform.GetChild(0).gameObject;

            RefreshVisibleRangeSqr();
        }
    }
}