using UnityEngine;
using VzDev.DCIM.Deployment;

namespace VzDev.DCIMUtils.RackDeployment
{
    /// <summary>
    /// 上架結果暫存服務：目前用 PlayerPrefs 存 JSON 字串（符合需求「先做暫存」），
    /// 之後若要接後端 API，只需把 Load/Save 的實作換成 API 呼叫，
    /// DeploymentSessionController 呼叫的介面（AppendRecord/LoadAll/Clear）不需要變動。
    ///
    /// 注意：PlayerPrefs 在 WebGL 是用 IndexedDB 實作，SetString 後務必呼叫 Save()
    /// 才會確實寫入，否則瀏覽器重新整理有機率遺失（WebGL 沒有程式正常關閉時的 flush 時機）。
    /// </summary>
    public static class DeploymentPersistenceService
    {
        private const string PrefsKey = "VzDev_DeploymentRecords";

        public static void AppendRecord(DeploymentRecord record)
        {
            var data = LoadAll();
            data.records.Add(record);
            SaveAll(data);
        }

        public static DeploymentSaveData LoadAll()
        {
            string json = PlayerPrefs.GetString(PrefsKey, string.Empty);
            if (string.IsNullOrEmpty(json)) return new DeploymentSaveData();

            try
            {
                return JsonUtility.FromJson<DeploymentSaveData>(json) ?? new DeploymentSaveData();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[{nameof(DeploymentPersistenceService)}] JSON解析失敗，回傳空資料：{e.Message}");
                return new DeploymentSaveData();
            }
        }

        public static void SaveAll(DeploymentSaveData data)
        {
            string json = JsonUtility.ToJson(data);
            PlayerPrefs.SetString(PrefsKey, json);
            PlayerPrefs.Save();
        }

        public static void Clear()
        {
            PlayerPrefs.DeleteKey(PrefsKey);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// 移除指定機櫃+起始U槽的紀錄（供未來「卸載設備」功能使用，目前流程未涵蓋卸載，先預留介面）。
        /// </summary>
        public static void RemoveRecord(string rackAssetNo, int startUSlot)
        {
            var data = LoadAll();
            data.records.RemoveAll(r => r.rackAssetNo == rackAssetNo && r.assignment.startUSlot == startUSlot);
            SaveAll(data);
        }
    }
}