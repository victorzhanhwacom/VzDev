using System;
using UnityEngine;
using VzDev.DCIMUtils.DataUtils;

namespace VzDev.DCIMUtils.DeploymentUtils
{
     /// <summary>
    /// 庫存設備資料格式DTO
    /// </summary>
    [Serializable]
    public class EquipmentAsset_StockDTO
    {
        public string deviceCode;
        /// <summary>
        /// 財產編號
        /// </summary>
        public string serialNumber;
        /// <summary>
        /// 型號
        /// </summary>
        public string modelName;
        /// <summary>
        /// 品牌
        /// </summary>
        public string brand;
        /// <summary>
        /// 系統類別: DCS、DCN
        /// </summary>
        public string system;
        public int power;
        public float weight;
        public int heightU;
        /// <summary>
        /// 模型Prefab
        /// </summary>
        public Transform modelPrefab;

        public EquipmentAsset ToEquipmentAsset()
        {
            COBieInfo cobieInfo = new COBieInfo
            {
                type_modelNumber = modelName,
                type_manufacturer = brand,
                system_category = system,
            };
            EquipmentUsageInfo usageInfo = new EquipmentUsageInfo
            {
                power_watt = power,
                weight_kg = weight,
                heightU = heightU
            };
            CompanyPropertyInfo companyPropertyInfo = new CompanyPropertyInfo
            {
                propertyName = modelName,
            };

            /// For Demo
            companyPropertyInfo.GenerateRandomPropertyNo("NTCGO");

            return new EquipmentAsset
            {
                deviceCode = deviceCode,
                deviceName = modelName,
                system = (DCIMCategory)Enum.Parse(typeof(DCIMCategory), system),
                deploymentStatus = DeploymentStatus.InStock,
                companyPropertyInfo = companyPropertyInfo,
                cobieInfo = cobieInfo,
                equipmentUsageInfo = usageInfo,
            };
        }
    }
}
