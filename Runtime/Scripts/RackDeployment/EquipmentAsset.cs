using System;

namespace VzDev.DCIM.Deployment
{
    /// <summary>
    /// 庫存設備資料。deploymentStatus 由 DeploymentSessionController 在 Step5 確認上架後
    /// 改為 Deployed；庫存清單UI應依此欄位過濾，已上架的設備不應再出現在可選清單中。
    /// </summary>
    [Serializable]
    public class EquipmentAsset : DCIMAsset
    {
        public EquipmentPowerInfo powerInfo;
        public EquipmentDeploymentStatus deploymentStatus = EquipmentDeploymentStatus.InStock;

        /// <summary>
        /// Step4填寫的基本資訊（選填）
        /// </summary>
        public string customName;
        public string note;

        /// <summary>
        /// 上架後記錄目前所在機櫃編號與起始U槽，供之後查詢/卸載使用
        /// </summary>
        public string deployedRackAssetNo;
        public int deployedStartUSlot;
    }
}
