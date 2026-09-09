using System;
using System.Collections.Generic;
using UnityEngine;
using VzDev.DCIMUtils.DeploymentUtils;
using VzDev.UnityAPI.Extensions;

namespace VzDev.DCIMUtils.DataUtils
{
    /// <summary>
    /// 設備資產資料 (DCR專用) - 機櫃
    /// </summary>
    [Serializable]
    public class DCR_Asset : DCIMAsset
    {
        public DCR_Asset()
        {
            system = DCIM_System.DCR;
            category = DCIM_Catetory.Rack;
        }

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

        /// <summary>
        /// 新增設備資產到機櫃內
        /// </summary>
        public void AddEquipmentAsset(EquipmentAsset equipmentAsset)
        {
            if (equipmentAsset == null) return;
            if (container == null) container = new List<EquipmentAsset>();
            container.Add(equipmentAsset);
           

            Transform equipmentModel = equipmentAsset.modelInfo.modelTarget;
            Transform rackModel = modelInfo.modelTarget;
            equipmentModel.SetParent(rackModel);

            if (equipmentModel.TryAddComponent(out DataModelBinder_Equipment equipmentBinder))
            {
                equipmentBinder.SetEquipmentAsset(equipmentAsset);
                equipmentAsset.deploymentStatus = DeploymentStatus.Deployed;
            }

            RefreshUsageInfo();
        }
        /// <summary>
        /// 移除機櫃內的設備資產
        /// </summary>
        public void RemoveEquipmentAsset(EquipmentAsset equipmentAsset)
        {
            if (equipmentAsset == null) return;
            if (container == null) container = new List<EquipmentAsset>();
            else container.Remove(equipmentAsset);
            RefreshUsageInfo();
        }
    }
}

