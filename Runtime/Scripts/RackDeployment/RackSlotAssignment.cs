using System;

namespace VzDev.DCIM.Deployment
{
    /// <summary>
    /// 記錄機櫃上單一U槽區段被哪個設備佔用。
    /// startUSlot 從 1 起算（對應機櫃實體U槽編號），佔用範圍為 [startUSlot, startUSlot + uHeight - 1]。
    ///
    /// powerWatt / weightKg 在上架當下從 EquipmentAsset.powerInfo 快照進來，
    /// 之後計算機櫃剩餘容量只需要讀 RackAsset.slotAssignments，不需要反查設備資料庫，
    /// 對 JSON 暫存（見 DeploymentPersistenceService）也比較自足，不怕之後設備資料變動造成舊紀錄失真。
    /// </summary>
    [Serializable]
    public class RackSlotAssignment
    {
        public string equipmentAssetNo;
        public string equipmentAssetName;
        public int startUSlot;
        public int uHeight;
        public int powerWatt;
        public float weightKg;

        public int EndUSlot => startUSlot + uHeight - 1;

        public bool Overlaps(int otherStart, int otherHeight)
        {
            int otherEnd = otherStart + otherHeight - 1;
            return startUSlot <= otherEnd && otherStart <= EndUSlot;
        }
    }
}