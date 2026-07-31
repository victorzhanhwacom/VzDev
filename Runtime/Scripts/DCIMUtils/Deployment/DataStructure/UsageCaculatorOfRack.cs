using UnityEngine;
using VzDev.DCIM.RevitAssetDataStructure;
using Debug = VzDev.ToolUtils.Debug;

namespace VzDev
{
    /// <summary>
    /// 機櫃使用量資訊 (功率/重量/U高)
    /// </summary>
    public class UsageCaculatorOfRack
    {
        #region Fields
        [field: SerializeField]
        public int totalPowerWatt { get; private set; }
        [field: SerializeField]
        public float totalWeightKG { get; private set; }
        [field: SerializeField]
        public int totalHeightU { get; private set; }

        [field: SerializeField]
        public int remainPowerWatt { get; private set; }
        [field: SerializeField]
        public float remainWeightKG { get; private set; }
        [field: SerializeField]
        public int remainHeightU { get; private set; }

        [field: SerializeField]
        public float totalPowerPercent { get; private set; }
        [field: SerializeField]
        public float totalWeightPercent { get; private set; }
        [field: SerializeField]
        public float totalHeightUPercent { get; private set; }

        [field: SerializeField]
        public float remainPowerPercent { get; private set; }
        [field: SerializeField]
        public float remainWeightPercent { get; private set; }
        [field: SerializeField]
        public float remainHeightUPercent { get; private set; }

        private DCR_Asset _dcrAsset;

        #endregion

        /// <summary>
        /// 刷新機櫃使用量資訊 (功率/重量/U高)
        /// </summary>
        internal void RefreshUsageInfo(DCR_Asset dcrAsset)
        {
            if (Debug.Assert(dcrAsset != null, $"[{GetType().Name}] DCR_Asset is null")) return;
            if (Debug.Assert(dcrAsset.container != null, $"[{GetType().Name}] DCR_Asset.container is null")) return;

            _dcrAsset = dcrAsset;
            Calculate_EquipmentUsage();
            Calculate_Percent();
        }

        /// <summary>
        /// 計算資產設備使用量 (功率/重量/高度)
        /// </summary>
        private void Calculate_EquipmentUsage()
        {
            totalPowerWatt = 0;
            totalWeightKG = 0;
            totalHeightU = 0;

            for (int i = 0; i < _dcrAsset.container.Count; i++)
            {
                totalPowerWatt += _dcrAsset.container[i].equipmentUsageInfo.power_watt;
                totalWeightKG += _dcrAsset.container[i].equipmentUsageInfo.weight_kg;
                totalHeightU += _dcrAsset.container[i].equipmentUsageInfo.u_height;
            }
            remainPowerWatt = _dcrAsset.rackCapacityInfo.power_watt_Max - totalPowerWatt;
            remainWeightKG = _dcrAsset.rackCapacityInfo.weight_kg_Max - totalWeightKG;
            remainHeightU = _dcrAsset.rackCapacityInfo.u_height_Max - totalHeightU;
        }

        /// <summary>
        /// 計算使用百分比
        /// </summary>
        private void Calculate_Percent()
        {
            totalPowerPercent = _dcrAsset.rackCapacityInfo.power_watt_Max <= 0 ? 0f : (float)totalPowerWatt / _dcrAsset.rackCapacityInfo.power_watt_Max * 100f;
            totalWeightPercent = _dcrAsset.rackCapacityInfo.weight_kg_Max <= 0 ? 0f : (float)totalWeightKG / _dcrAsset.rackCapacityInfo.weight_kg_Max * 100f;
            totalHeightUPercent = _dcrAsset.rackCapacityInfo.u_height_Max <= 0 ? 0f : (float)totalHeightU / _dcrAsset.rackCapacityInfo.u_height_Max * 100f;
            remainPowerPercent = _dcrAsset.rackCapacityInfo.power_watt_Max <= 0 ? 0f : (float)remainPowerWatt / _dcrAsset.rackCapacityInfo.power_watt_Max * 100f;
            remainWeightPercent = _dcrAsset.rackCapacityInfo.weight_kg_Max <= 0 ? 0f : (float)remainWeightKG / _dcrAsset.rackCapacityInfo.weight_kg_Max * 100f;
            remainHeightUPercent = _dcrAsset.rackCapacityInfo.u_height_Max <= 0 ? 0f : (float)remainHeightU / _dcrAsset.rackCapacityInfo.u_height_Max * 100f;
        }
    }
}
