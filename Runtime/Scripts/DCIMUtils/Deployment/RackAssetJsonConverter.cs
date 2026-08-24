using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;
using VzDev.DCIMUtils.DataUtils;
using VzDev.UnityAPI.Extensions;
using Debug = VzDev.ToolUtils.Debug;

namespace VzDev.DCIMUtils.Import
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
            Debug.Log("22:" +json.ToJsonFormat());
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
                cobieInfo = entry.information.ToCOBieInfo(),
                container = entry.containers
            };
            // container 依 startUIndex 遞減排序：U槽編號越大（越靠機櫃上方）排越前面。
            // 直接在 List 上 Sort，不額外配置新清單；container 跟 entry.containers 是同一個
            // 參照，排序後兩邊都會反映新順序，這裡不需要另外處理。
            // 若 JSON 裡明確給 "containers": null（不是缺欄位），entry.containers 會覆蓋成
            // null，這裡補一個防呆避免 Sort() 對 null 清單噴例外。
            result.container ??= new List<EquipmentAsset>();
            result.container.Sort((a, b) => b.startUIndex.CompareTo(a.startUIndex));
            result.companyPropertyInfo.propertyName = entry.devicePath.Split(":").LastOrDefault().Trim();
            result.companyPropertyInfo.GenerateRandomPropertyNo();
            result.RefreshUsageInfo();
            return result;
        }
        /// <summary>
        /// 將單筆 DCR_Asset 轉換成 DCR_Asset_DTO，是 Convert(DCR_Asset_DTO) 的反向操作。
        /// <para>
        /// 【尺寸欄位是假設值】原本 Convert() 從未對應過 sizeInfo（DTO的length/width/height
        /// 從來沒有被拿來設定 asset.sizeInfo），所以這裡反過來也沒有「正確答案」可以還原，
        /// 只能依欄位名稱猜一個對應關係（length↔depth_mm、width↔width_mm、height↔height_mm，
        /// 並依照 DTO 註解「單位公分，映射到SizeInfo需×10換算毫米」反過來 ÷10）。
        /// 如果實際對應關係不同，或者你不需要匯出尺寸，可以自行調整或刪除這幾行。
        /// </para>
        /// <para>
        /// 【companyPropertyInfo 不會匯出】DTO 沒有對應欄位承載財產名稱/編號，
        /// 這兩個欄位目前是 Convert() 時另外從 devicePath 產生、GenerateRandomPropertyNo() 隨機產生，
        /// 匯出時就遺失了，這是預期的行為（DTO 本來就只是WebAPI交換格式，不是完整備份格式）。
        /// </para>
        /// </summary>
        public static DCR_Asset_DTO ConvertToDto(DCR_Asset asset)
        {
            if (asset == null)
            {
                Debug.LogWarning("傳入的 DCR_Asset 為 null，無法進行轉換。");
                return null;
            }

            DCR_Asset_DTO dto = new DCR_Asset_DTO
            {
                devicePath = asset.deviceCode,
                information = InformationDto.FromCOBieInfo(asset.cobieInfo),
                containers = asset.container ?? new List<EquipmentAsset>(),
            };

            dto.information.watt_limit = asset.power_watt_Max;
            dto.information.weight_limit = asset.weight_kg_Max;
            dto.information.heightU = asset.u_height_Max;
            dto.information.weight = asset.weight_kg;

            if (asset.sizeInfo != null)
            {
                dto.information.length = asset.sizeInfo.depth_mm / 10f;
                dto.information.width = asset.sizeInfo.width_mm / 10f;
                dto.information.height = asset.sizeInfo.height_mm / 10f;
            }

            return dto;
        }

        /// <summary>
        /// 將 DCR_Asset 清單全部轉換成 DCR_Asset_DTO 清單。
        /// </summary>
        public static List<DCR_Asset_DTO> ConvertAllToDto(List<DCR_Asset> assets)
        {
            List<DCR_Asset_DTO> result = new List<DCR_Asset_DTO>();
            if (assets == null)
            {
                Debug.LogWarning("傳入的 DCR_Asset 清單為 null，無法進行轉換。");
                return result;
            }
            for (int i = 0; i < assets.Count; i++)
            {
                DCR_Asset_DTO dto = ConvertToDto(assets[i]);
                if (dto != null) result.Add(dto);
            }
            return result;
        }

        /// <summary>
        /// 將 DCR_Asset 清單轉換成 DTO 後序列化成 json 字串，直接寫入指定路徑。
        /// filePath 若包含不存在的資料夾，會自動建立。
        /// </summary>
        public static void ExportToJsonFile(List<DCR_Asset> assets, string filePath)
        {
            List<DCR_Asset_DTO> dtoList = ConvertAllToDto(assets);
            string json = JsonConvert.SerializeObject(dtoList, Formatting.Indented);

            string directory = System.IO.Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !System.IO.Directory.Exists(directory))
                System.IO.Directory.CreateDirectory(directory);

            System.IO.File.WriteAllText(filePath, json);
            Debug.Log($"[{nameof(RackAssetJsonConverter)}] 已匯出 {dtoList.Count} 筆機櫃資料到：{filePath}");
        }
    }
}