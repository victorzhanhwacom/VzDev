using UnityEngine;

namespace VzDev
{
    /// <summary>
    /// 掛載在「機櫃」物件上。
    /// interiorCollider 是機櫃內部可安裝空間的 BoxCollider（尺寸 = 內部淨空間）。
    /// 提供 MountEquipment()：把任意 Pivot 位置/尺寸不同的設備，依 startUIndex 對齊到正確 U 高度，
    /// 並讓設備前緣貼齊機櫃內部空間的正面。
    /// </summary>
    public class RackMountSlot : MonoBehaviour
    {
        [Header("機櫃內部可安裝空間")]
        [Tooltip("代表機櫃內部淨空間大小/位置的 BoxCollider，建議設為 IsTrigger，僅作為量測參考用")]
        public BoxCollider interiorCollider;

        [Header("U 規格")]
        [Tooltip("每 1U 高度（依場景單位換算，標準 1U = 0.04445 m）")]
        public float uHeight = 0.04445f;
        [Tooltip("機櫃總 U 數，僅供 UI 顯示 / 邊界檢查用")]
        public int totalU = 42;

        [Header("方向設定")]
        [Tooltip("機櫃「正面」是否為 interiorCollider 這個 Transform 的 local +Z（設備會從這一面被看到/插入）")]
        public bool rackFrontIsPositiveZ = true;

        /// <summary>
        /// 上架單一設備。
        /// </summary>
        /// <param name="equipment">要上架的設備 Transform（Pivot 位置可任意）</param>
        /// <param name="startUIndex">起始 U（由下往上，1-based，1 = 最底部第一格）</param>
        /// <param name="uSpan">設備佔用的 U 數（例如 1U、2U 伺服器）</param>
        /// <param name="equipmentFrontIsPositiveZ">此設備模型的正面是否為自己的 local +Z</param>
        public void MountEquipment(Transform equipment, int startUIndex, int uSpan, bool equipmentFrontIsPositiveZ = true)
        {
            if (interiorCollider == null || equipment == null)
            {
                Debug.LogError("[RackMountSlot] interiorCollider 或 equipment 尚未設定");
                return;
            }

            // ---------- Step 1：量測設備「相對於自身 Pivot」的真實外框 ----------
            // 這一步會暫時把 equipment 的旋轉歸零，量完再還原，過程中 equipment 會被設回機櫃朝向
            Bounds localBounds = GetLocalBoundsRelativeToPivot(equipment);
            if (localBounds.size == Vector3.zero)
                return; // 量不到 Renderer，GetLocalBoundsRelativeToPivot 內已印警告

            // ---------- Step 2：機櫃內部空間的世界座標資訊 ----------
            Transform rackT = interiorCollider.transform;
            Vector3 interiorCenterWorld = rackT.TransformPoint(interiorCollider.center);
            Vector3 interiorHalfSize = Vector3.Scale(interiorCollider.size, rackT.lossyScale) * 0.5f;

            Vector3 rackRight = rackT.right;
            Vector3 rackUp = rackT.up;
            Vector3 rackForward = rackFrontIsPositiveZ ? rackT.forward : -rackT.forward;

            // 內部空間「底部中心」世界座標
            Vector3 interiorBottomCenter = interiorCenterWorld - rackUp * interiorHalfSize.y;

            // ---------- Step 3：依 startUIndex 算出目標高度，組出「正面、水平置中、對應U高度底部」的世界座標點 ----------
            float heightFromBottom = (startUIndex - 1) * uHeight;

            Vector3 targetFrontBottomCenter =
                interiorBottomCenter
                + rackUp * heightFromBottom
                + rackForward * interiorHalfSize.z; // 貼齊正面（内部空間深度方向的邊界）

            // ---------- Step 4：套用設備最終旋轉（跟機櫃同朝向），並反推 Pivot 應放的位置 ----------
            equipment.rotation = rackT.rotation;

            // 設備正面在 local space 是 +Z 還是 -Z
            float equipFrontZ = equipmentFrontIsPositiveZ ? localBounds.max.z : localBounds.min.z;

            // Pivot 到「前面、水平中心、底部」這個參考點的向量（local space）
            Vector3 pivotToFrontBottomCenterLocal = new Vector3(
                localBounds.center.x,   // 水平方向：幾何中心，讓設備左右置中
                localBounds.min.y,      // 垂直方向：底部
                equipFrontZ             // 深度方向：正面
            );

            Vector3 pivotToFrontBottomCenterWorld = equipment.rotation * pivotToFrontBottomCenterLocal;

            equipment.position = targetFrontBottomCenter - pivotToFrontBottomCenterWorld;
        }

        /// <summary>
        /// 計算物件「相對於自身 Pivot」的 local bounds（涵蓋所有子物件 Renderer）。
        /// 作法：暫時把旋轉歸零，此時 world AABB 就等同「未旋轉狀態下的 local 外框」，
        /// 再減去 pivot 世界座標，即可得到不受 Pivot 位置影響的相對外框。
        /// </summary>
        private Bounds GetLocalBoundsRelativeToPivot(Transform target)
        {
            Quaternion originalRotation = target.rotation;
            target.rotation = Quaternion.identity;

            Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                Debug.LogWarning($"[RackMountSlot] {target.name} 找不到任何 Renderer，無法計算外框");
                target.rotation = originalRotation;
                return new Bounds(Vector3.zero, Vector3.zero);
            }

            Bounds worldBounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                worldBounds.Encapsulate(renderers[i].bounds);

            // rotation = identity 時，world AABB 的軸向就是 local 軸向，
            // 減去 pivot(position) 即可得到「相對 pivot」的 local 外框
            Vector3 min = worldBounds.min - target.position;
            Vector3 max = worldBounds.max - target.position;

            target.rotation = originalRotation;

            Bounds localBounds = new Bounds();
            localBounds.SetMinMax(min, max);
            return localBounds;
        }

#if UNITY_EDITOR
        [Header("Debug 用途（選填）")]
        public Transform debugEquipment;
        public int debugStartU = 1;
        public int debugUSpan = 1;

        [ContextMenu("Debug/Mount Equipment Now")]
        private void DebugMountNow()
        {
            MountEquipment(debugEquipment, debugStartU, debugUSpan);
        }
#endif
    }
}
