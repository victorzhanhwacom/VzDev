using System;
using UnityEngine;
using VzDev.DCIM.Deployment;

namespace VzDev.DCIMUtils.RackDeployment
{
    /// <summary>
    /// 對應你要接的具體設備子類別。目前只有 DCS(伺服主機)/DCN(網路設備) 兩種有掛ModelComponent
    /// （見 ServerComponent/NetworkDeviceComponent），之後新增 DCE/DCP 類別時，這裡加一個enum值，
    /// CreateAssetInstance() 加一個 case，再新增對應的 XxxComponent 即可。
    /// </summary>
    public enum EquipmentCategoryType
    {
        DCS,
        DCN,
    }

    /// <summary>
    /// 庫存清單一個項目對應的「目錄資料」，目前先在 Inspector 上寫死填值，
    /// 之後接上真實後端時，這個類別可以整個換成從API回傳資料建構，
    /// 其他系統（DeviceListItemView/DeploymentSessionController/DeployedModelSpawner）不需要改。
    /// </summary>
    [Serializable]
    public class EquipmentCatalogEntry
    {
        [Tooltip("庫存清單UI上顯示的名稱")]
        public string displayName;
        [Tooltip("用於產生臨時assetNo的前綴，例如 CRAC、UPS")]
        public string assetNoPrefix;
        [Tooltip("對應的具體設備子類別")]
        public EquipmentCategoryType categoryType;
        [Tooltip("上架完成後實際要Instantiate的模型Prefab，需掛對應的 ServerComponent/NetworkDeviceComponent")]
        public GameObject modelPrefab;
        [Tooltip("該種設備的電力/重量/U高")]
        public EquipmentPowerInfo powerInfo;
        [Tooltip("庫存清單UI用的圖示，選用")]
        public Sprite icon;

        /// <summary>
        /// 依 categoryType 建立對應的具體資產實體，並帶入本目錄的固定資料（名稱/電力重量U高）與
        /// 一個臨時產生的assetNo（庫存項目目前沒有真實後端ID，先用GUID湊一個，
        /// 之後接後端時這裡改成用後端回傳的assetNo即可）。
        /// </summary>
        public EquipmentAssetBase CreateAssetInstance()
        {
            EquipmentAssetBase asset = categoryType switch
            {
                EquipmentCategoryType.DCS => new DCS_Asset(),
                EquipmentCategoryType.DCN => new DCN_Asset(),
                _ => new DCS_Asset(),
            };

            asset.category = categoryType.ToString();
            asset.equipmentInfo = powerInfo;
            asset.modelInfo = new ModelInfo();
            asset.assetInfo = new AssetInfo
            {
                assetName = displayName,
                assetNo = $"{assetNoPrefix}_{Guid.NewGuid():N}"[..12],
            };

            return asset;
        }
    }
}