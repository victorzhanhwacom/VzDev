using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using VzDev.DCIM.Deployment;

namespace VzDev.DCIMUtils.RackDeployment
{
    /// <summary>
    /// 掛載於獨立在ScrollView外的「已選中設備」預覽物件（例如 Listitem_ICTDevice_Selected）。
    /// 清單裡每一項（DeviceListItemView）只負責Toggle選取，選取當下就已經呼叫
    /// DeploymentSessionController.BeginDeployment 進入Step1/2；本類別只負責「使用者把這個
    /// 已選中的預覽物件拖到機櫃/U槽上」這個動作本身，完全不重複選取邏輯。
    ///
    /// 拖放失敗（沒放到合法目標）時不會取消Session，讓使用者可以再拖一次；
    /// 顯示/隱藏本物件則完全跟隨 DeploymentSessionController 的事件，不需要外部呼叫控制。
    /// </summary>
    public class SelectedEquipmentDragHandle : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private Canvas rootCanvas;
        [SerializeField, Tooltip("3D場景拖放偵測用的Raycast Camera，2D UI版本不需要設定")]
        private Camera worldRaycastCamera;
        [SerializeField, Tooltip("3D場景中機櫃模型所在的LayerMask")]
        private LayerMask rackLayer;
        [SerializeField, Tooltip("拖曳時跟隨滑鼠顯示的圖示，Instantiate在rootCanvas底下，放開時自動銷毀")]
        private GameObject dragIconPrefab;
        [SerializeField, Tooltip("實際顯示用的節點，沒有任何選取時會被隱藏；本物件的Toggle/EventTrigger可留在外層")]
        private GameObject displayRoot;

        private GameObject dragIconInstance;
        private RackSlotDropTarget currentHover3D;
        private RackSlotCell currentHoverCell;
        private readonly List<RaycastResult> raycastResultsBuffer = new();

        private void OnEnable()
        {
            DeploymentSessionController.OnEquipmentSelected += HandleEquipmentSelected;
            DeploymentSessionController.OnSessionCancelled += HandleSessionEnded;
            DeploymentSessionController.OnDeploymentCompleted += HandleDeploymentCompleted;
            SetDisplayVisible(false);
        }

        private void OnDisable()
        {
            DeploymentSessionController.OnEquipmentSelected -= HandleEquipmentSelected;
            DeploymentSessionController.OnSessionCancelled -= HandleSessionEnded;
            DeploymentSessionController.OnDeploymentCompleted -= HandleDeploymentCompleted;
        }

        /// <summary>
        /// 這裡只處理顯示/隱藏；名稱/圖示要顯示什麼，交給你的顯示用UI腳本自己也訂閱
        /// OnEquipmentSelected 取 equipment.assetInfo.assetName / 目錄圖示（可從
        /// EquipmentCatalogRegistry.TryGetEntry(equipment.assetInfo.assetNo, out entry) 拿 entry.icon）。
        /// </summary>
        private void HandleEquipmentSelected(EquipmentAssetBase equipment) => SetDisplayVisible(equipment != null);
        private void HandleSessionEnded() => SetDisplayVisible(false);
        private void HandleDeploymentCompleted(DeploymentRecord record) => SetDisplayVisible(false);

        private void SetDisplayVisible(bool visible)
        {
            if (displayRoot != null) displayRoot.SetActive(visible);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (DeploymentSessionController.Instance == null || !DeploymentSessionController.Instance.IsAwaitingSlotSelection) return;

            if (dragIconPrefab != null && rootCanvas != null)
                dragIconInstance = Instantiate(dragIconPrefab, rootCanvas.transform);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (dragIconInstance != null)
                dragIconInstance.transform.position = eventData.position;

            UpdateHover3D(eventData);
            UpdateHoverCell(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (dragIconInstance != null) Destroy(dragIconInstance);

            if (currentHover3D != null) currentHover3D.TryDropHere();
            else if (currentHoverCell != null) currentHoverCell.TryDropHere();
            // 沒放到合法目標：不呼叫CancelDeployment，Session維持在等待選定狀態，可以再拖一次

            ClearHoverState();
        }

        private void UpdateHover3D(PointerEventData eventData)
        {
            if (worldRaycastCamera == null) { currentHover3D = null; return; }

            Ray ray = worldRaycastCamera.ScreenPointToRay(eventData.position);
            if (Physics.Raycast(ray, out RaycastHit hit, 1000f, rackLayer)
                && hit.collider.TryGetComponent<RackSlotDropTarget>(out var target))
            {
                if (currentHover3D != target)
                {
                    currentHover3D?.SetPreviewActive(false);
                    currentHover3D = target;
                }
                currentHover3D.UpdatePreview(hit.point);
            }
            else
            {
                currentHover3D?.SetPreviewActive(false);
                currentHover3D = null;
            }
        }

        /// <summary>
        /// 2D UI版本的懸停高亮由 RackSlotCell 自己的 IPointerEnter/ExitHandler 更新，
        /// 這裡只需要在放開滑鼠時，讀取EventSystem回報「目前滑鼠正上方的Cell是哪個」即可。
        /// </summary>
        private void UpdateHoverCell(PointerEventData eventData)
        {
            raycastResultsBuffer.Clear();
            EventSystem.current.RaycastAll(eventData, raycastResultsBuffer);
            currentHoverCell = null;
            foreach (var r in raycastResultsBuffer)
            {
                if (r.gameObject.TryGetComponent<RackSlotCell>(out var cell))
                {
                    currentHoverCell = cell;
                    break;
                }
            }
        }

        private void ClearHoverState()
        {
            currentHover3D?.SetPreviewActive(false);
            currentHover3D = null;
            currentHoverCell = null;
        }
    }
}