using System;
using System.Collections.Generic;
using VzDev.UnityAPI.Extensions;

namespace VzDev.DCIMUtils.DataUtils
{
    /// <summary>
    /// 設備資產資料 (DCR專用) - 機櫃
    /// </summary>
    [Serializable]
    public class DCR_Asset : DCIMAsset
    {
        public DCR_Asset() => system = DCIMCategory.DCR;

        /// <summary>
        /// 所在位置
        /// </summary>
        public string location;

        /// <summary>
        /// 機櫃本身重量
        /// </summary>
        public float weight_kg;

        /// <summary>
        /// 機櫃最大功率
        /// </summary>
        public int power_watt_Max;
        /// <summary>
        /// 機櫃最大承重
        /// </summary>
        public float weight_kg_Max;
        /// <summary>
        /// 機櫃最大U高
        /// </summary>
        public int u_height_Max = 42;

        /// <summary>
        /// 機櫃內的所有資產設備
        /// </summary>
        public List<EquipmentAsset> container = new();

        public UsageCaculatorOfRack usageInfo = new UsageCaculatorOfRack();
        /// <summary>
        /// 重新計算機櫃內的使用資訊 (功率/重量/U高)
        /// </summary>
        public void RefreshUsageInfo()
        {
            usageInfo ??= new UsageCaculatorOfRack();
            usageInfo.RefreshUsageInfo(this);
        }

        /// <summary>
        /// 若設備名稱為空則自動從模型名稱取得
        /// </summary>
        public void GenerateDeviceNameIfEmpty()
        {
            if (string.IsNullOrEmpty(deviceName) && modelInfo?.modelTarget != null)
            {
                deviceName = modelInfo.modelTarget.name.GetStringBetweenMarks("[", "]").Split(":")[1];
                companyPropertyInfo.propertyName = deviceName;
                companyPropertyInfo.GenerateRandomPropertyNo("NTCGO");
            }
        }
    }
}

