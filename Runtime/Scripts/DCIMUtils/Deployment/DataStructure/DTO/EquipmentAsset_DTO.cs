using System;

namespace VzDev.DCIMUtils.DataUtils
{
    /// <summary>
    /// DCR機櫃內所有設備資產基底類別
    /// <para>包含：DCS / DCN / DCE / DCP</para>
    /// </summary>
    [Serializable]
    public class EquipmentAsset_DTO
    {
        public string rackDevicePath;
        public string devicePath;
        public int rackLocation;
        public InformationDto information;

        public EquipmentAsset ToEquipmentAsset()
        {
            var asset = new EquipmentAsset
            {
                rackDevicePath = rackDevicePath,
                deviceCode = devicePath,
                cobieInfo = information?.ToCOBieInfo(),
                startUIndex = rackLocation,
                equipmentUsageInfo = new EquipmentUsageInfo
                {
                    heightU = information?.heightU ?? 0,
                    weight_kg = information?.weight ?? 0,
                    power_watt = information?.watt ?? 0
                }
            };
            return asset;
        }
    }
}