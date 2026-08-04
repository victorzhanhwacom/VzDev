using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;
using VzDev.DCIM.RevitAssetDataStructure;
using Debug = VzDev.ToolUtils.Debug;

namespace VzDev.DCIM.Import
{
    /// <summary>
    /// 解析 WebAPI 回傳的機櫃 JSON 資料，並進行DTO轉換成 DCR_Asset 清單。
    /// </summary>
    public static class RackAssetJsonConverter
    {
        /// <summary>
        /// 將完整的 json 字串（WebAPI 回傳的機櫃陣列）解析並轉換成 DCR_Asset 清單。
        /// </summary>
        public static List<DCR_Asset> ParseFromJson(string json)
        {
            List<DCR_Asset_DTO> entries = JsonConvert.DeserializeObject<List<DCR_Asset_DTO>>(json);
            return ConvertAll(entries);
        }

        /// <summary>
        /// 將已解析的 DTO 清單轉換成 DCR_Asset 清單。
        /// </summary>
        public static List<DCR_Asset> ConvertAll(List<DCR_Asset_DTO> entries)
        {
            List<DCR_Asset> result = new List<DCR_Asset>();
            if (entries == null)
            {
                Debug.LogWarning("傳入的 DTO 清單為 null，無法進行轉換。");
                return result;
            }
            for (int i = 0; i < entries.Count; i++)
            {
                DCR_Asset asset = Convert(entries[i]);
                if (asset != null) result.Add(asset);
            }
            return result;
        }

        /// <summary>
        /// 將單筆 DCR_Asset_DTO 轉換成 DCR_Asset。
        /// </summary>
        public static DCR_Asset Convert(DCR_Asset_DTO entry)
        {
            if (entry == null || entry.information == null)
            {
                Debug.LogWarning("傳入的 DTO 為 null，無法進行轉換。");
                return null;
            }
            DCR_Asset result = new DCR_Asset
            {
                deviceCode = entry.devicePath,
                weight_kg = entry.information.weight,
                weight_kg_Max = entry.information.weight_limit,
                power_watt_Max = entry.information.watt_limit,
                u_height_Max = entry.information.heightU,
                cobieInfo = entry.information.ToAsset(),
                container = entry.containers
            };
            result.companyPropertyInfo.propertyName = entry.devicePath.Split(":").LastOrDefault().Trim();
            result.companyPropertyInfo.GenerateRandomPropertyNo();

           /*  Transform model = rackModels?.Find(x => x.name.Contains(entry.devicePath));
            if (model == null)
            {
                Debug.LogWarning($"找不到對應的機櫃模型，請確認模型名稱是否包含 '{entry.devicePath}'。");
            }
            else
            {
                result.modelInfo.modelTarget = model;
            } */
            result.RefreshUsageInfo();
            return result;
        }
    }
}
