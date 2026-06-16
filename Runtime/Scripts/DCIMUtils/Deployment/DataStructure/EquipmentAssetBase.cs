using System;

namespace VzDev.DCIM.Deployment
{
    /// <summary>
    /// 機櫃內所有設備資產資料基底類別
    /// <para>包含：DCR / DCS / DCN / DCE / DCP</para>
    /// </summary>
    [Serializable]
    public class EquipmentAssetBase : DCIMAssetBase
    {
        /// <summary>
        /// 資產類別: DCR / DCS / DCN / DCE / DCP
        /// </summary>
        public string category;
    }
}