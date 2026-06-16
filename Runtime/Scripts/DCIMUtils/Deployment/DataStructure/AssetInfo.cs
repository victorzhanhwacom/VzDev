using System;

namespace VzDev.DCIM.Deployment
{
    /// <summary>
    /// 資產資料 (機櫃/設備資產類)
    /// <para>+ 資產名稱 / 資產編號 / 資產類別</para>
    /// </summary>
    [Serializable]
    public class AssetInfo
    {
        /// <summary>
        /// 資產名稱
        /// </summary>
        public string assetName;
        /// <summary>
        /// 資產編號
        /// </summary>
        public string assetNo;
    }


    [Serializable]  
    public struct COBieInfo
    {
        /// COBie資料 - 目前僅包含機櫃/設備資產共用的部分，未來如有需要再擴充
    }


    /// <summary>
    /// 機櫃專屬資料
    /// <para>+ 最大功率 / 最大重量 / 最大U高</para>
    /// </summary>
    [Serializable]
    public struct RackPowerInfo
    {
        public int power_watt_Max;
        public float weight_kg_Max;
        public int u_height_Max;
    }

    /// <summary>
    /// 資產設備專屬資料
    /// <para>+ 功率 / 重量 / U高</para>
    /// </summary>
    [Serializable]
    public struct EquipmentPowerInfo
    {
        public int power_watt;
        public float weight_kg;
        public int u_height;
    }
}