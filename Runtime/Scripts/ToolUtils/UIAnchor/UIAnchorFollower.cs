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

        [Label("定位目標物件"), SerializeField] private Transform target3DObject;

        [Foldout("[設定]"), SerializeField] private Vector3 offsetPos = Vector3.up * 0.1f;

        [Foldout("[設定]"), SerializeField] private float visibleRange = 20f;

        [Foldout("[組件]"), SerializeField] private Camera mainCamera;
        [Foldout("[組件]"), SerializeField] private RectTransform rectTrans, canvasRect;
        [Foldout("[組件]"), SerializeField] private GameObject container;

        public Transform Target3DObject => target3DObject;
        public float DistanceFromCamera => Vector3.Distance(mainCamera.transform.position, target3DObject.position);

        #endregion

        void Update()
        {
            Vector3 targetPos = target3DObject.position;
            Vector3 viewportPos = mainCamera.WorldToViewportPoint(targetPos);
            bool isInRange = Vector3.Distance(targetPos, mainCamera.transform.position) <= visibleRange;
            bool isInFrontOfCamera = viewportPos.z > 0;
            container.SetActive(isInRange && isInFrontOfCamera);

            if (container.activeSelf == false) return;

            targetPos = target3DObject.GetModelBoundsCenter() + offsetPos;

            // 1. 轉換 3D 世界座標到螢幕座標
            Vector3 screenPos = mainCamera.WorldToScreenPoint(targetPos);

            if (screenPos.z < 0) return;

            // 2. 取得 Canvas 尺寸
            float scaleX = canvasRect.sizeDelta.x / Screen.width;
            float scaleY = canvasRect.sizeDelta.y / Screen.height;

            // 3. 計算相對於 Canvas 的座標
            Vector2 localPos = new Vector2(
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

        private void Start() => OnValidate();

        [Button]
        public void OnValidate()
        {
            mainCamera = Camera.main;
            canvasRect = transform.GetComponentInParent<Canvas>(true)?.transform as RectTransform;

            rectTrans = transform as RectTransform;
            container = transform.GetChild(0).gameObject;
            /*  if (transform.TryGetComponentInParent(out PositionTo2DPointSorter sorter))
                 sorter.AddToSortList(this); */
        }
    }
}