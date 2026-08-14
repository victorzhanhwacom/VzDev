using NaughtyAttributes;
using UnityEngine;
using VzDev.DCIMUtils.DataUtils;
using VzDev.DCIMUtils.ModelInteractUtils;

namespace VzDev.DCIMUtils.Deployment
{
    /// <summary>
    /// 偵測滑鼠目前指向的機櫃，並計算指向第幾個 U 槽，透過 static event 廣播結果。
    /// U1 定義在機櫃最底部，由下往上編號（機房業界慣例）。
    ///
    /// 【定位說明】這是全域唯一的偵測源，供多個互不相識的模組共用同一份結果
    /// （部署預覽換色、U槽列表 UI、Tooltip 顯示目前指向第幾U…等），
    /// 因此場上只能存在一個 instance，做法對照 GlobalLifecycleBroadcaster。
    ///
    /// 【暫時簡化，待後續補完】
    /// 1. rackLayer 上的 Collider 需為專門對應「機櫃內部可用安裝空間」的獨立 Trigger
    ///    Collider（非機櫃外殼），其世界座標高度範圍即視為可用 U 槽區間。
    /// 2. 假設機櫃僅繞世界 Y 軸旋轉（直立擺放），故直接使用世界座標 Bounds。
    /// </summary>
    public class RackUSlotHoverDetector : MonoBehaviour
    {
        #region Fields
        [Foldout("[Components]"), SerializeField] private Camera mainCamera;
        [Foldout("[Settings]"), SerializeField, Tooltip("機櫃內部可用空間所在的 Layer，只對這個 Layer 做 Raycast")]
        private LayerMask rackLayer;
        [Foldout("[Settings]"), SerializeField] private float maxRayDistance = 100f;

        [SerializeField, ReadOnly, Tooltip("是否有重複的 instance")] private bool isDuplicate = false;

        private readonly RaycastHit[] hitBuffer = new RaycastHit[4];

        [SerializeField, ReadOnly] private DCR_Asset currentRackAsset;
        [SerializeField, ReadOnly] private int currentUIndex = -1;
        #endregion

        #region Public API
        /// <summary>機櫃或 U 槽編號改變時廣播，asset 為 null 代表目前沒有指向任何機櫃。</summary>
        public static event System.Action<DCR_Asset, int, Collider> OnRackUSlotChanged;
        #endregion

        private static RackUSlotHoverDetector instanceRef;

        #region Lifecycle
        private void Awake()
        {
            if (instanceRef != null)
            {
                Debug.LogError(
                    $"{nameof(RackUSlotHoverDetector)} 場景上重複存在，此 instance 將被銷毀：{gameObject.name}", this);
                isDuplicate = true;
                Destroy(gameObject);
                return;
            }
            instanceRef = this;
        }

        private void OnDestroy()
        {
            if (isDuplicate) return;
            if (instanceRef == this) instanceRef = null;
            OnRackUSlotChanged = null; // 場景卸載時清空殘留訂閱，避免下個 instance 載入後舊訂閱端殘留
        }

        private void Update()
        {
            if (isDuplicate) return;

            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            int hitCount = Physics.RaycastNonAlloc(ray, hitBuffer, maxRayDistance, rackLayer);

            if (hitCount == 0)
            {
                ClearCurrent();
                return;
            }

            RaycastHit hit = hitBuffer[GetClosestHitIndex(hitCount)];

            if (!hit.collider.GetComponentInParent<IHasDCIMAsset>().TryGetAsset<DCR_Asset>(out var rackAsset))
            {
                ClearCurrent();
                return;
            }

            int uIndex = CalculateUIndex(hit.collider, hit.point, rackAsset);

            if (rackAsset == currentRackAsset && uIndex == currentUIndex) return;

            currentRackAsset = rackAsset;
            currentUIndex = uIndex;
            OnRackUSlotChanged?.Invoke(currentRackAsset, currentUIndex, hit.collider);
        }
        #endregion

        #region Calculation
        private int CalculateUIndex(Collider rackCollider, Vector3 hitPointWorld, DCR_Asset rackAsset)
        {
            int totalU = rackAsset.u_height_Max;
            Bounds bounds = rackCollider.bounds;
            float usableHeight = bounds.size.y;
            if (usableHeight <= 0f || totalU <= 0) return -1;

            float uHeightWorld = usableHeight / totalU;
            float relativeY = hitPointWorld.y - bounds.min.y;

            int uIndex = Mathf.FloorToInt(relativeY / uHeightWorld) + 1;
            return Mathf.Clamp(uIndex, 1, totalU);
        }

        private int GetClosestHitIndex(int hitCount)
        {
            int closest = 0;
            float closestDistance = float.MaxValue;
            for (int i = 0; i < hitCount; i++)
            {
                if (hitBuffer[i].distance < closestDistance)
                {
                    closestDistance = hitBuffer[i].distance;
                    closest = i;
                }
            }
            return closest;
        }

        private void ClearCurrent()
        {
            if (currentRackAsset == null && currentUIndex == -1) return;
            currentRackAsset = null;
            currentUIndex = -1;
            OnRackUSlotChanged?.Invoke(null, -1, null);
        }
        #endregion
    }
}