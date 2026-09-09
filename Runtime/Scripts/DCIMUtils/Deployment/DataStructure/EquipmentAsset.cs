using System;
using VzDev.MathUtils;
using Random = UnityEngine.Random;

namespace VzDev.DCIMUtils.DataUtils
{
    /// <summary>
    /// DCR機櫃內所有設備資產基底類別
    /// <para>包含：DCS / DCN / DCE / DCP</para>
    /// </summary>
    [Serializable]
    public class EquipmentAsset : DCIMAsset
    {
        public string rackDevicePath;

        public EquipmentUsageInfo equipmentUsageInfo;
        public DeploymentStatus deploymentStatus = DeploymentStatus.Unknow;
        public int startUIndex; // 部署在機櫃裡的起始 U 位置，未部署時為 0 或 -1

        /// <summary>
        /// 佔用U位範圍
        /// </summary>
        public string uRange => $"U{startUIndex} ~ U{startUIndex + equipmentUsageInfo.heightU - 1}";

        /// <summary>
        /// 複制一個新的設備資產，並保留原本的屬性值
        /// </summary>
        public EquipmentAsset ToClone()
        {
            var cloneAsset = new EquipmentAsset
            {
                deviceCode = deviceCode,
                rackDevicePath = rackDevicePath,
                deviceName = deviceName,
                cobieInfo = COBieInfo.ToClone(cobieInfo),
                modelInfo = ModelInfo.ToClone(modelInfo),
                timeStampData = timeStampData,
                category = category,
                system = system,
                companyPropertyInfo = companyPropertyInfo,
                equipmentUsageInfo = equipmentUsageInfo,
                deploymentStatus = deploymentStatus,
                startUIndex = startUIndex
            };

            if (string.IsNullOrEmpty(cloneAsset.deviceCode))
            {
                cloneAsset.deviceCode = $"{deviceName}[{deviceName}-{Random.Range(0,MathHelper.GetAllNines(3))}]";
            }
            return cloneAsset;
        }

        public DCN_Asset ToDCNAsset()
        {
            var dcnAsset = new DCN_Asset
            {
                deviceCode = deviceCode,
                deviceName = deviceName,
                cobieInfo = cobieInfo,
                modelInfo = modelInfo,
                timeStampData = timeStampData,
                category = category,
                system = system,
                companyPropertyInfo = companyPropertyInfo,
                equipmentUsageInfo = equipmentUsageInfo,
                deploymentStatus = deploymentStatus,
                startUIndex = startUIndex
            };
            return dcnAsset;
        }

        public DCS_Asset ToDCSAsset()
        {
            var dcsAsset = new DCS_Asset
            {
                deviceCode = deviceCode,
                deviceName = deviceName,
                cobieInfo = cobieInfo,
                modelInfo = modelInfo,
                timeStampData = timeStampData,
                category = category,
                system = system,
                companyPropertyInfo = companyPropertyInfo,
                equipmentUsageInfo = equipmentUsageInfo,
                deploymentStatus = deploymentStatus,
                startUIndex = startUIndex
            };
            return dcsAsset;
        }

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