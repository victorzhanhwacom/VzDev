using System;

namespace VzDev.DCIMUtils.DataUtils
{
    /// <summary>
    /// DCR機櫃內所有設備資產基底類別
    /// <para>包含：DCS / DCN / DCE / DCP</para>
    /// </summary>
    [Serializable]
    public class EquipmentAsset : DCIMAsset
    {
        public EquipmentUsageInfo equipmentUsageInfo;

        public DeploymentStatus deploymentStatus = DeploymentStatus.Unknow;
        public int startUIndex; // 部署在機櫃裡的起始 U 位置，未部署時為 0 或 -1
    }

    /// <summary>
    /// 資產設備使用的功率/重量/U高資訊
    /// </summary>
    [Serializable]
    public struct EquipmentUsageInfo
    {
        public int power_watt;
        public float weight_kg;
        public int heightU;
    }

    /// <summary>
    /// 資產設備的上架狀態
    /// </summary>
    public enum DeploymentStatus
    {
        Unknow,
        InStock,   // 尚在庫存，未上架
        Deployed,  // 已上架至機櫃
    }
}