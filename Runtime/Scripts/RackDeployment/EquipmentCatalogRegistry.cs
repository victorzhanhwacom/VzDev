using System.Collections.Generic;

namespace VzDev.DCIMUtils.RackDeployment
{
    /// <summary>
    /// 執行期assetNo → EquipmentCatalogEntry 的查表，由 DeviceListItemView 在Toggle選中、
    /// 產生臨時資產實體時登記，供 DeployedModelSpawner 在Step5上架完成後，
    /// 反查該用哪個 modelPrefab 生成實體模型。
    ///
    /// 之後接後端資料時，這張表可能不再需要（後端資料本身就會帶Prefab對應關係），
    /// 屆時直接整個檔案刪掉，呼叫端改查後端資料即可。
    /// </summary>
    public static class EquipmentCatalogRegistry
    {
        private static readonly Dictionary<string, EquipmentCatalogEntry> registry = new();

        public static void Register(string assetNo, EquipmentCatalogEntry entry)
        {
            if (string.IsNullOrEmpty(assetNo) || entry == null) return;
            registry[assetNo] = entry;
        }

        public static bool TryGetEntry(string assetNo, out EquipmentCatalogEntry entry)
            => registry.TryGetValue(assetNo, out entry);
    }
}