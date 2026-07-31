using System;

namespace VzDev.DCIM.RevitAssetDataStructure
{
    /// <summary>
    /// 機櫃內所有設備資產設備基底類別
    /// <para>包含：DCR / DCS / DCN / DCE / DCP</para>
    /// </summary>
    [Serializable]
    public class EquipmentAsset : DCIMAsset
    {
        public EquipmentUsageInfo equipmentUsageInfo;

        /// <summary>【配置管理模組新增】上架狀態</summary>
        public EquipmentDeploymentStatus deploymentStatus = EquipmentDeploymentStatus.Unknow;
    }

    /// <summary>
    /// 資產設備的功率/重量/U高資訊
    /// </summary>
    [Serializable]
    public struct EquipmentUsageInfo
    {
        public int power_watt;
        public float weight_kg;
        public int u_height;
    }

    /// <summary>
    /// 資產設備的上架狀態
    /// </summary>
    public enum EquipmentDeploymentStatus
    {
        Unknow, 
        InStock,   // 尚在庫存，未上架
        Deployed,  // 已上架至機櫃
    }
}