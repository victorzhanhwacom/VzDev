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

        /// <summary>
        /// 機櫃最大功率
        /// </summary>
        public int power_watt_Max;
        /// <summary>
        /// 機櫃最大重量
        /// </summary>
        public float weight_kg_Max;
        /// <summary>
        /// 機櫃最大U高
        /// </summary>
        public int u_height_Max = 42;

        /// <summary>
        /// 機櫃內的所有資產設備
        /// </summary>
        public List<EquipmentAsset> container;
        
        public UsageCaculatorOfRack usageInfo;
        /// <summary>
        /// 重新計算機櫃內的使用資訊 (功率/重量/U高)
        /// </summary>
        public void RefreshUsageInfo()
        {
            usageInfo ??= new UsageCaculatorOfRack();
            usageInfo.RefreshUsageInfo(this);
        }
    }
}

