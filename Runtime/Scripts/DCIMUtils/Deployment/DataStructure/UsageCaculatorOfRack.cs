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
        /// 檢查機櫃內 [uIndex, uIndex + heightU - 1] 這個 U 區段，
        /// 是否與 container 裡任何既有設備的佔用區段重疊，或超出機櫃總 U 數。
        /// </summary>
        public static bool CanFit(DCR_Asset rack, int uIndex, int heightU)
        {
            int topUIndex = uIndex + heightU - 1;
            if (uIndex < 1 || topUIndex > rack.u_height_Max) return false;

            for (int i = 0; i < rack.container.Count; i++)
            {
                EquipmentAsset equipment = rack.container[i];
                if(equipment.deploymentStatus != DeploymentStatus.Deployed) continue;
                int equipmentTop = equipment.startUIndex + equipment.equipmentUsageInfo.heightU - 1;

                // 兩個區段不重疊的條件：一個完全在另一個上方，或完全在另一個下方
                bool noOverlap = topUIndex < equipment.startUIndex || uIndex > equipmentTop;
                if (!noOverlap) return false;
            }
            return true;
        }

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
                totalHeightU += _dcrAsset.container[i].equipmentUsageInfo.heightU;
            }
            remainPowerWatt = _dcrAsset.power_watt_Max - totalPowerWatt;
            remainWeightKG = _dcrAsset.weight_kg_Max - totalWeightKG;
            remainHeightU = _dcrAsset.u_height_Max - totalHeightU;
        }

        /// <summary>
        /// 計算使用百分比
        /// </summary>
        private void Calculate_Percent()
        {
            totalPowerPercent = _dcrAsset.power_watt_Max <= 0 ? 0f : (float)totalPowerWatt / _dcrAsset.power_watt_Max * 100f;
            totalWeightPercent = _dcrAsset.weight_kg_Max <= 0 ? 0f : (float)totalWeightKG / _dcrAsset.weight_kg_Max * 100f;
            totalHeightUPercent = _dcrAsset.u_height_Max <= 0 ? 0f : (float)totalHeightU / _dcrAsset.u_height_Max * 100f;
            remainPowerPercent = _dcrAsset.power_watt_Max <= 0 ? 0f : (float)remainPowerWatt / _dcrAsset.power_watt_Max * 100f;
            remainWeightPercent = _dcrAsset.weight_kg_Max <= 0 ? 0f : (float)remainWeightKG / _dcrAsset.weight_kg_Max * 100f;
            remainHeightUPercent = _dcrAsset.u_height_Max <= 0 ? 0f : (float)remainHeightU / _dcrAsset.u_height_Max * 100f;
        }
    }
}
