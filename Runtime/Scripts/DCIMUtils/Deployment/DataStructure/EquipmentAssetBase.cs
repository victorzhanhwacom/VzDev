using System;

namespace VzDev.DCIM.Deployment
{
    public enum EquipmentDeploymentStatus
    {
        InStock,   // 尚在庫存，未上架
        Deployed,  // 已上架至機櫃
    }

    /// <summary>
    /// 機櫃內所有設備資產資料基底類別
    /// <para>包含：DCR / DCS / DCN / DCE / DCP</para>
    /// </summary>
    [Serializable]
    public class EquipmentAssetBase : DCIMAsset
    {
        /// <summary>
        /// 資產類別: DCR / DCS / DCN / DCE / DCP
        /// </summary>
        public string category;

        /// <summary>
        /// 電力/重量/U高。【配置管理模組新增】原本 DCS_Asset / DCN_Asset 各自宣告一份同名欄位，
        /// 上移到這裡統一管理，讓 RackCapacityEvaluator 等計算邏輯可以不知道具體子類別，
        /// 直接讀這個共用欄位，之後新增 DCE/DCP 等類別也不需要各自再宣告一次。
        /// </summary>
        public EquipmentPowerInfo equipmentInfo;

        /// <summary>【配置管理模組新增】上架狀態</summary>
        public EquipmentDeploymentStatus deploymentStatus = EquipmentDeploymentStatus.InStock;
        /// <summary>【配置管理模組新增】上架後所在機櫃的起始U槽（1起算），未上架時無意義</summary>
        public int startUSlot;
        /// <summary>【配置管理模組新增】Step4填寫的基本資訊（選填）</summary>
        public string customName;
        public string note;
    }
}