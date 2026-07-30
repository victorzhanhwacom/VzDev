using VzDev.DCIM.Deployment;

namespace VzDev.DCIMUtils.RackDeployment
{
    /// <summary>
    /// 純計算層：不持有任何狀態，直接讀 DCR_Asset.container（機櫃內目前已上架的設備清單，
    /// 型別為 EquipmentAssetBase，同時涵蓋 DCS/DCN 等所有子類別）計算剩餘容量，
    /// 並評估指定設備需求是否能放入。Step2變色與Step3拖曳合法性檢查都呼叫此類別，
    /// 不重複計算邏輯。
    /// </summary>
    public static class RackCapacityEvaluator
    {
        public static int GetUsedPowerWatt(DCR_Asset rack)
        {
            int sum = 0;
            if (rack.container == null) return sum;
            foreach (var e in rack.container) sum += e.equipmentInfo.power_watt;
            return sum;
        }

        public static float GetUsedWeightKg(DCR_Asset rack)
        {
            float sum = 0f;
            if (rack.container == null) return sum;
            foreach (var e in rack.container) sum += e.equipmentInfo.weight_kg;
            return sum;
        }

        public static int GetUsedUSlots(DCR_Asset rack)
        {
            int sum = 0;
            if (rack.container == null) return sum;
            foreach (var e in rack.container) sum += e.equipmentInfo.u_height;
            return sum;
        }

        public static RackCapacityResult Evaluate(DCR_Asset rack, EquipmentPowerInfo required)
        {
            int remainingPower = rack.rackPowerInfo.power_watt_Max - GetUsedPowerWatt(rack);
            float remainingWeight = rack.rackPowerInfo.weight_kg_Max - GetUsedWeightKg(rack);
            int remainingU = rack.rackPowerInfo.u_height_Max - GetUsedUSlots(rack);

            RackCapacityShortfall shortfall = RackCapacityShortfall.None;
            if (required.power_watt > remainingPower) shortfall |= RackCapacityShortfall.Power;
            if (required.weight_kg > remainingWeight) shortfall |= RackCapacityShortfall.Weight;
            if (required.u_height > remainingU) shortfall |= RackCapacityShortfall.Space;

            return new RackCapacityResult(remainingPower, remainingWeight, remainingU, shortfall);
        }

        /// <summary>
        /// 檢查指定起始U槽是否與 container 裡現有設備的佔用區段重疊，不檢查電力/重量
        /// （那是整櫃層級的判斷，交給Evaluate；這裡只判斷「這個U槽區段本身有沒有被別的設備佔用」）。
        /// </summary>
        public static bool IsSlotRangeFree(DCR_Asset rack, int startUSlot, int uHeight)
        {
            if (rack == null) return false;
            if (startUSlot < 1 || startUSlot + uHeight - 1 > rack.rackPowerInfo.u_height_Max) return false;
            if (rack.container == null) return true;

            int newEnd = startUSlot + uHeight - 1;
            foreach (var e in rack.container)
            {
                int existingEnd = e.startUSlot + e.equipmentInfo.u_height - 1;
                bool overlaps = e.startUSlot <= newEnd && startUSlot <= existingEnd;
                if (overlaps) return false;
            }
            return true;
        }

        /// <summary>由下往上找第一個能放入指定高度的空白U槽區段，找不到回傳 -1。</summary>
        public static int FindFirstFreeSlot(DCR_Asset rack, int uHeight)
        {
            if (rack == null) return -1;
            for (int start = 1; start + uHeight - 1 <= rack.rackPowerInfo.u_height_Max; start++)
            {
                if (IsSlotRangeFree(rack, start, uHeight)) return start;
            }
            return -1;
        }
    }
}