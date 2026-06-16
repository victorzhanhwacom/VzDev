using System;
using System.Collections.Generic;

namespace VzDev.DCIM.Deployment
{
    /// <summary>
    /// 設備資產資料 (DCR專用) - 機櫃
    /// </summary>
    [Serializable]
    public class DCR_Asset: EquipmentAssetBase
    {
        public RackPowerInfo rackPowerInfo;

        /// <summary>
        /// 機櫃內的所有資產設備
        /// </summary>
        public List<EquipmentAssetBase> container;
    }
} 

