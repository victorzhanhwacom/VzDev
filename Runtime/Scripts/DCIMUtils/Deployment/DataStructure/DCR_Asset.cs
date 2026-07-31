using System;
using System.Collections.Generic;

namespace VzDev.DCIM.RevitAssetDataStructure
{
    /// <summary>
    /// 設備資產資料 (DCR專用) - 機櫃
    /// </summary>
    [Serializable]
    public class DCR_Asset : DCIMAsset
    {
        public DCR_Asset() => category = DCIMCategory.DCR;
        public CapacityInfo rackCapacityInfo;

        /// <summary>
        /// 機櫃內的所有資產設備
        /// </summary>
        public List<EquipmentAsset> container;
        public UsageCaculatorOfRack usageInfo;
        public void RefreshUsageInfo()
        {
            usageInfo ??= new UsageCaculatorOfRack();
            usageInfo.RefreshUsageInfo(this);
        }
    }

    /// <summary>
    /// 機櫃 最大功率 / 最大重量 / 最大U高</para>
    /// </summary>
    [Serializable]
    public struct CapacityInfo
    {
        public int power_watt_Max;
        public float weight_kg_Max;
        public int u_height_Max;
    }
}

