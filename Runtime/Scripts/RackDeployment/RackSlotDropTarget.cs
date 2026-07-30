using UnityEngine;
using VzDev.DCIM.Deployment;
using VzDev.DCIMUtils.ModelInteractUtils;

namespace VzDev.DCIMUtils.RackDeployment
{
    /// <summary>
    /// 掛載於機櫃3D模型上，負責把「拖曳懸停在機櫃上的世界座標」換算成對應的U槽編號，
    /// 顯示落點預覽（合法/不合法用不同顏色），並在放開滑鼠時呼叫 DeploymentSessionController
    /// 做合法性檢查與提交。
    ///
    /// U槽編號換算假設：機櫃模型原點在底部中心，U1從底部算起，slotPitchMeters為每U實際高度
    /// （標準機櫃 1U = 0.04445m，若模型比例/原點位置不同，請調整 slotPitchMeters 或改寫換算方式）。
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class RackSlotDropTarget : MonoBehaviour
    {
        [SerializeField, Tooltip("每U實際高度（公尺），標準機櫃為0.04445m")]
        private float slotPitchMeters = 0.04445f;
        [SerializeField, Tooltip("U槽落點預覽用的指示物件，建議用扁平方塊，Inspector先關閉")]
        private GameObject slotPreviewIndicator;

        private DCR_Asset rackAsset;
        private int previewStartUSlot;

        private void Awake()
        {
            if (TryGetComponent<IHasDCIMAsset>(out var provider))
                rackAsset = provider.GetAsset() as DCR_Asset;

            if (slotPreviewIndicator != null) slotPreviewIndicator.SetActive(false);
        }

        /// <summary>由 SelectedEquipmentDragHandle 每次OnDrag懸停在本機櫃上時呼叫，更新落點預覽</summary>
        public void UpdatePreview(Vector3 worldHitPoint)
        {
            if (rackAsset == null || DeploymentSessionController.Instance == null) return;

            float localY = transform.InverseTransformPoint(worldHitPoint).y;
            int uHeight = DeploymentSessionController.Instance.PendingUHeight;
            int rawSlot = Mathf.FloorToInt(localY / slotPitchMeters) + 1;
            int maxStart = Mathf.Max(1, rackAsset.rackPowerInfo.u_height_Max - uHeight + 1);
            previewStartUSlot = Mathf.Clamp(rawSlot, 1, maxStart);

            bool isFree = RackCapacityEvaluator.IsSlotRangeFree(rackAsset, previewStartUSlot, uHeight);
            SetPreviewActive(true, isFree);
        }

        public void SetPreviewActive(bool isActive, bool isValid = true)
        {
            if (slotPreviewIndicator == null) return;
            slotPreviewIndicator.SetActive(isActive);
            if (!isActive) return;

            // 合法/不合法顏色示意；正式版建議改走 MaterialStateService 統一管理材質切換
            if (slotPreviewIndicator.TryGetComponent<Renderer>(out var renderer))
                renderer.material.color = isValid
                    ? new Color(0.3f, 1f, 0.5f, 0.5f)
                    : new Color(1f, 0.3f, 0.3f, 0.5f);

            float previewY = (previewStartUSlot - 1) * slotPitchMeters;
            slotPreviewIndicator.transform.localPosition = new Vector3(0f, previewY, 0f);
        }

        /// <summary>放開滑鼠時呼叫，嘗試把目前預覽的U槽區段送給Session做合法性檢查與提交</summary>
        public bool TryDropHere()
        {
            if (rackAsset == null || DeploymentSessionController.Instance == null) return false;
            return DeploymentSessionController.Instance.TrySelectTargetSlot(rackAsset, previewStartUSlot);
        }
    }
}