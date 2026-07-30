using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using VzDev.DCIM.Deployment;

namespace VzDev.DCIMUtils.RackDeployment
{
    /// <summary>
    /// 2D UI版本的機櫃U槽格，掛載於機櫃側視圖UI（例如機櫃詳細面板裡逐一列出的U槽清單）每一格上。
    /// 與 RackSlotDropTarget（3D版本）功能對等，換算方式簡單很多：格子本身就固定對應某個U槽編號，
    /// 不需要做世界座標轉換。
    /// </summary>
    public class RackSlotCell : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField, Tooltip("此格對應的U槽編號")] private int uSlotNumber;
        [SerializeField, Tooltip("此格所屬的機櫃資料，由生成U槽清單UI時指定")] private DCR_Asset ownerRack;
        [SerializeField] private Image highlightImage;

        public void SetOwnerRack(DCR_Asset rack, int slotNumber)
        {
            ownerRack = rack;
            uSlotNumber = slotNumber;
        }

        public void OnPointerEnter(PointerEventData eventData) => UpdateHighlight();
        public void OnPointerExit(PointerEventData eventData) => SetHighlight(false, true);

        private void UpdateHighlight()
        {
            if (DeploymentSessionController.Instance == null) return;
            int uHeight = DeploymentSessionController.Instance.PendingUHeight;
            bool isFree = RackCapacityEvaluator.IsSlotRangeFree(ownerRack, uSlotNumber, uHeight);
            SetHighlight(true, isFree);
        }

        private void SetHighlight(bool isActive, bool isValid)
        {
            if (highlightImage == null) return;
            highlightImage.gameObject.SetActive(isActive);
            if (isActive)
                highlightImage.color = isValid
                    ? new Color(0.3f, 1f, 0.5f, 0.5f)
                    : new Color(1f, 0.3f, 0.3f, 0.5f);
        }

        /// <summary>放開滑鼠時呼叫，嘗試把這個格子代表的U槽送給Session做合法性檢查與提交</summary>
        public bool TryDropHere()
        {
            if (ownerRack == null || DeploymentSessionController.Instance == null) return false;
            return DeploymentSessionController.Instance.TrySelectTargetSlot(ownerRack, uSlotNumber);
        }
    }
}