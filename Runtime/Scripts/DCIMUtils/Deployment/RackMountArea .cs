using NaughtyAttributes;
using UnityEngine;

namespace VzDev.DCIMUtils.RackDeployment
{
    /// <summary>
    /// 掛在機櫃的「可上架區域」BoxCollider上，定義U槏的座標換算基準。
    /// Collider.center/size是local space（未套用transform scale），
    /// 所以InverseTransformPoint出來的Y可以直接跟size.y做比例換算，不受scale影響。
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class RackMountArea : MonoBehaviour
    {
        [SerializeField, Tooltip("機櫃總U數")] private int totalU = 42;
        [SerializeField, Tooltip("U1是否在底部（業界標準：由下往上編號）")]
        private bool uNumberingBottomToTop = true;

        private BoxCollider mountCollider;
        private BoxCollider MountCollider => mountCollider ??= GetComponent<BoxCollider>();

        public int TotalU => totalU;

        /// <summary>
        /// 傳入RaycastHit.point（世界座標），回傳1-based U編號，並clamp在合法範圍內。
        /// </summary>
        public int GetUIndexFromWorldPoint(Vector3 worldPoint)
        {
            Vector3 localPoint = transform.InverseTransformPoint(worldPoint);

            Vector3 c = MountCollider.center;
            Vector3 s = MountCollider.size;
            float localBottom = c.y - s.y * 0.5f;
            float localTop = c.y + s.y * 0.5f;

            float t01 = Mathf.InverseLerp(localBottom, localTop, localPoint.y);
            int uIndexFromBottom = Mathf.Clamp(Mathf.FloorToInt(t01 * totalU), 0, totalU - 1);

            return uNumberingBottomToTop
                ? uIndexFromBottom + 1
                : totalU - uIndexFromBottom;
        }

        /// <summary>
        /// 多U設備（例如2U伺服器）上架時，確保設備不會超出機櫃頂部。
        /// baseU是設備底部所在的U數（1-based）。
        /// </summary>
        public int ClampForEquipmentHeight(int baseU, int equipmentUHeight)
        {
            int maxBaseU = totalU - equipmentUHeight + 1;
            return Mathf.Clamp(baseU, 1, Mathf.Max(1, maxBaseU));
        }
    }
}