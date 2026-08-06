using NaughtyAttributes;
using UnityEngine;
using VzDev.UnityAPI.Extensions;

namespace VzDev.ObjectUtils
{
    /// <summary>
    /// 將 UI 物件的 2D 錨點 (Anchor) 鎖定並跟隨指定的 3D 世界物件座標
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
        private Renderer targetRenderer;

        public float DistanceFromCamera => Vector3.Distance(mainCamera.transform.position, target3DObject.position);

        private Vector3 targetPos, viewportPos;
        private bool isInRange, isInFrontOfCamera;
        private Vector3 screenPos;
        private Vector2 localPos;
        private float scaleX, scaleY;

        private float thresholdSqr = 0.0001f;

        #endregion

        private void Start()
        {
            OnValidate();
            targetRenderer = target3DObject.GetComponent<Renderer>();
        }

        private bool CanSeeTarget(Renderer targetRenderer, Transform eye, Transform target, LayerMask obstacleMask)
        {
            return true;
            // 第一層：Occlusion Culling 快速篩選（效能好，先擋掉大部分不可見的情況）
            if (!targetRenderer.isVisible)
                return false;

            // 第二層：Raycast 精確驗證（只在 isVisible == true 時才做，省效能）
            Vector3 dir = target.position - eye.position;
            if (Physics.Raycast(eye.position, dir.normalized, out RaycastHit hit, dir.magnitude, obstacleMask))
            {
                Debug.DrawLine(eye.position, hit.point, Color.red);
                // Debug.Log($"Raycast hit: {hit.transform.name} (target: {target.name})", this);
                return hit.transform == target; // 打到別的東西 = 仍被擋住
            }
            return true;
        }


        void Update()
        {
            if (target3DObject == null) return;
            //if(targetPos.IsApproximatelySqr(target3DObject.position, thresholdSqr)) return;

            targetPos = target3DObject.position;
            viewportPos = mainCamera.WorldToViewportPoint(targetPos);
            isInRange = isAlwaysVisible || Vector3.Distance(targetPos, mainCamera.transform.position) <= visibleRange;

            isInRange = isInRange && CanSeeTarget(targetRenderer, mainCamera.transform, target3DObject, LayerMask.GetMask("Default"));
            if (!isAlwaysVisible) isInRange = visibleReverse ? !isInRange : isInRange;

            isInFrontOfCamera = viewportPos.z > 0;
            container.SetActive(isInRange && isInFrontOfCamera && target3DObject.gameObject.activeInHierarchy);

            if (container.activeSelf == false) return;

            targetPos = target3DObject.GetModelBoundsCenter() + offsetPos;

            // 1. 轉換 3D 世界座標到螢幕座標
            screenPos = mainCamera.WorldToScreenPoint(targetPos);

            if (screenPos.z < 0) return;

            // 2. 取得 Canvas 尺寸
            scaleX = canvasRect.sizeDelta.x / Screen.width;
            scaleY = canvasRect.sizeDelta.y / Screen.height;

            // 3. 計算相對於 Canvas 的座標
            localPos = new Vector2(
                (screenPos.x - (Screen.width * 0.5f)) * scaleX,
                (screenPos.y - (Screen.height * 0.5f)) * scaleY
            );

            // 4. 設定 UI 位置
            rectTrans.anchoredPosition = localPos;
        }

        /// 設定目標物件
        public void SetTargetObject(Transform target) => target3DObject = target;

        /// 設定可視距離
        public void SetVisibleRange(float range) => visibleRange = range;

        [Button]
        public void OnValidate()
        {
            mainCamera = Camera.main;
            canvasRect = transform.GetComponentInParent<Canvas>(true)?.rootCanvas.transform as RectTransform;
            // canvasRect = transform.GetComponentInParent<Canvas>(true)?.transform as RectTransform;

            rectTrans = transform as RectTransform;
            container = transform.GetChild(0).gameObject;
        }
    }
}