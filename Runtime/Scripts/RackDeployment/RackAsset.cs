using System;
using System.Collections.Generic;

namespace VzDev.DCIM.Deployment
{
    /// <summary>
    /// 機櫃資產資料。powerInfo 為機櫃的容量上限（電力/重量/U高，見 AssetInfo.cs 的 RackPowerInfo），
    /// slotAssignments 為目前已上架設備的佔用清單（執行期狀態，隨上架/卸載變動，
    /// 由 DeploymentSessionController 統一寫入，其他系統只應讀取，不應直接修改）。
    /// </summary>
    [Serializable]
    public class RackAsset : DCIMAsset
    {
        public RackPowerInfo powerInfo;
        public List<RackSlotAssignment> slotAssignments = new();
    }
}
