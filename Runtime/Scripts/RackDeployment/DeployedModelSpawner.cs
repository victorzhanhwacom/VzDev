using UnityEngine;
using VzDev;
using VzDev.DCIM.Deployment;

namespace VzDev.DCIMUtils.RackDeployment
{
    /// <summary>
    /// 監聽 Step5 上架完成事件，依 DeploymentRecord.assignment.equipmentAssetNo 反查
    /// EquipmentCatalogRegistry 找到對應的 modelPrefab，Instantiate 到機櫃底下正確的U槽高度，
    /// 並依目錄的 categoryType 掛上對應的 ServerComponent/NetworkDeviceComponent 設定資料。
    ///
    /// 拖曳/選取U槽階段（RackSlotDropTarget/RackSlotCell）只顯示「預覽指示物」，不會真的生成模型，
    /// 只有Step5確認上架成功後，這裡才真正把模型放進場景——避免拖曳中途取消時，
    /// 場景裡多出一個沒卡進機櫃、需要額外清理的孤兒模型。
    ///
    /// 位置換算方式與 RackSlotDropTarget.UpdatePreview 對稱（同一套 slotPitchMeters/原點假設：
    /// 機櫃原點在底部中心，U1從底部算起），兩處若之後改成不同的機櫃原點慣例，記得同步調整。
    /// </summary>
    public class DeployedModelSpawner : MonoBehaviour
    {
        [SerializeField, Tooltip("需與 RackSlotDropTarget 使用相同數值，兩處都是把U槽換算成本地座標高度")]
        private float slotPitchMeters = 0.04445f;

        private void OnEnable() => DeploymentSessionController.OnDeploymentCompleted += HandleDeploymentCompleted;
        private void OnDisable() => DeploymentSessionController.OnDeploymentCompleted -= HandleDeploymentCompleted;

        private void HandleDeploymentCompleted(DeploymentRecord record)
        {
            if (!EquipmentCatalogRegistry.TryGetEntry(record.assignment.equipmentAssetNo, out var entry))
            {
                Debug.LogWarning($"[{nameof(DeployedModelSpawner)}] 找不到目錄項目，可能是assetNo沒有先登記：{record.assignment.equipmentAssetNo}", this);
                return;
            }
            if (entry.modelPrefab == null)
            {
                Debug.LogWarning($"[{nameof(DeployedModelSpawner)}] 目錄項目 {entry.displayName} 沒有指定modelPrefab", this);
                return;
            }

            GameObject rackObject = FindRackObjectByAssetNo(record.rackAssetNo);
            if (rackObject == null)
            {
                Debug.LogWarning($"[{nameof(DeployedModelSpawner)}] 找不到機櫃GameObject：{record.rackAssetNo}", this);
                return;
            }

            var instance = Instantiate(entry.modelPrefab, rackObject.transform);
            float localY = (record.assignment.startUSlot - 1) * slotPitchMeters;
            instance.transform.localPosition = new Vector3(0f, localY, 0f);
            instance.transform.localRotation = Quaternion.identity;

            AssignComponentData(instance, entry, record);
        }

        /// <summary>
        /// 依目錄的categoryType決定要用哪個具體ModelComponent設定資料。之後新增DCE/DCP類別時，
        /// 這裡加一個case、掛對應的新Component即可，其他邏輯不用動。
        /// </summary>
        private void AssignComponentData(GameObject instance, EquipmentCatalogEntry entry, DeploymentRecord record)
        {
            var equipmentInfo = new EquipmentPowerInfo
            {
                power_watt = record.assignment.powerWatt,
                weight_kg = record.assignment.weightKg,
                u_height = record.assignment.uHeight,
            };
            var assetInfo = new AssetInfo
            {
                assetName = record.assignment.equipmentAssetName,
                assetNo = record.assignment.equipmentAssetNo,
            };

            switch (entry.categoryType)
            {
                case EquipmentCategoryType.DCS:
                    if (instance.TryGetComponent<ServerComponent>(out var serverComp))
                    {
                        serverComp.SetData(BuildAsset<DCS_Asset>(assetInfo, equipmentInfo, entry, record));
                    }
                    break;
                case EquipmentCategoryType.DCN:
                    if (instance.TryGetComponent<NetworkDeviceComponent>(out var netComp))
                    {
                        netComp.SetData(BuildAsset<DCN_Asset>(assetInfo, equipmentInfo, entry, record));
                    }
                    break;
            }
        }

        private static T BuildAsset<T>(AssetInfo assetInfo, EquipmentPowerInfo equipmentInfo,
            EquipmentCatalogEntry entry, DeploymentRecord record) where T : EquipmentAssetBase, new()
        {
            return new T
            {
                assetInfo = assetInfo,
                equipmentInfo = equipmentInfo,
                category = entry.categoryType.ToString(),
                deploymentStatus = EquipmentDeploymentStatus.Deployed,
                startUSlot = record.assignment.startUSlot,
                customName = record.customName,
                note = record.note,
            };
        }

        /// <summary>
        /// 先用最簡單的方式：遍歷 RackRegistry 已註冊的機櫃比對 assetNo。
        /// 機櫃數量到200+時如果這裡變成效能瓶頸，改成 RackRegistry 額外維護一份
        /// assetNo→GameObject 的查表即可，呼叫端介面不用變。
        /// </summary>
        private GameObject FindRackObjectByAssetNo(string rackAssetNo)
        {
            foreach (var rackObject in RackRegistry.AllRackObjects)
            {
                if (RackRegistry.TryGetRackAsset(rackObject, out var rack) && rack.assetInfo?.assetNo == rackAssetNo)
                    return rackObject;
            }
            return null;
        }
    }
}