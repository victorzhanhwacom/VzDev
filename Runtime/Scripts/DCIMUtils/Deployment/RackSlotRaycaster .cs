using UnityEngine;

namespace VzDev.DCIMUtils.RackDeployment
{
    /// <summary>
    /// 上架模式專用的Raycast，只在拖曳設備時啟用（由外部控制enabled/Update開關）。
    /// 與ColliderInteractionSystem職責分離：那邊管一般模型互動，這裡只管「當前指到機櫃第幾U」。
    /// </summary>
    public class RackSlotRaycaster : MonoBehaviour
    {
        [SerializeField] private Camera mainCamera;
        [SerializeField] private LayerMask rackMountLayer;
        [SerializeField] private float maxDistance = 100f;

        /// <summary>
        /// 回傳是否打中機櫃可上架區域，out參數給出對應的RackMountArea與U編號。
        /// </summary>
        public bool TryGetHoveredSlot(out RackMountArea mountArea, out int uIndex)
        {
            mountArea = null;
            uIndex = -1;

            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out RaycastHit hit, maxDistance, rackMountLayer))
                return false;

            if (!hit.collider.TryGetComponent(out mountArea))
                return false;

            uIndex = mountArea.GetUIndexFromWorldPoint(hit.point);
            return true;
        }
    }
}