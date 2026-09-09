using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Networking;
using VzDev.ApiExtensions;
using VzDev.FileUtils;

namespace VzDev.DCIMUtils.DeploymentUtils
{
    /// <summary>
    /// WebAPI管理器：設備管理
    /// </summary>
    public class WebAPIManager_EquipmentDeploy : MonoBehaviour
    {
        #region Fields
        public ForDemo_RackInfo forDemo;
        public ForDemo_StockEquipment forDemo_StockEquipment;
        #endregion

        /// <summary>
        /// 取得機房內機櫃群資訊
        /// </summary>
        [Button]
        public void GetRackListInformation()
        {
            string result = forDemo.GetRackListInformation();

            StartCoroutine(ReadJsonFile(result, (json) =>
            {
                Debug.Log($"{GetType().Name}-GetRackListInformation:\n{json}");
                OnGetRackListInformationAction?.Invoke(json);
            }));

            /* Debug.Log($"{GetType().Name}-GetRackListInformation:\n{result}");
            OnGetRackListInformationAction?.Invoke(result); */
        }

        /// <summary>
        /// 取得上架庫存設備列表
        /// </summary>
     /*    [Button]
        public void GetEquipmentAssetInStock()
        {
            string result = forDemo_StockEquipment.GetStockEquipmentList();

            StartCoroutine(ReadJsonFile(result, (json) =>
            {
                Debug.Log($"{GetType().Name}-GetEquipmentAssetInStock:\n{json}");
                OnGetEquipmentAssetInStockAction?.Invoke(json);
            }));
        } */


        /// <summary>
        /// 讀取機架清單 JSON(WebGL 平台下透過 UnityWebRequest 非同步讀取 StreamingAssets)。
        /// </summary>
        /// <param name="onComplete">讀取完成後呼叫，參數為 JSON 字串；失敗時為 null。</param>
        private IEnumerator ReadJsonFile(string path, Action<string> onComplete)
        {
            string url = Path.Combine(Application.streamingAssetsPath, path);
            Debug.Log($"ReadJsonFile: {url} \t {path}");

            using (UnityWebRequest req = UnityWebRequest.Get(url))
            {
                yield return req.SendWebRequest();

                if (req.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"找不到檔案: {url} ({req.error})");
                    onComplete?.Invoke(null);
                    yield break;
                }

                onComplete?.Invoke(req.downloadHandler.text);
            }
        }

        /// <summary>
        /// 取得設備模型列表
        /// </summary>
        [Button]
        public void GetEquipmentModels()
        {
            List<Transform> result = forDemo.GetEquipmentModels();
            Debug.Log($"{GetType().Name}-GetEquipmentModels:\n{result.CombineToString(true)}");
            OnGetEquipmentModelsAction?.Invoke(result);
        }

        /// <summary>
        /// 取得庫存設備資料列表
        /// </summary>
        [Button]
        public void GetStockEquipmentList()
        {

            string result = forDemo_StockEquipment.GetStockEquipmentList();

            StartCoroutine(ReadJsonFile(result, (json) =>
            {
                Debug.Log($"{GetType().Name}-GetStockEquipmentList:\n{json}");
                OnGetStockEquipmentListAction?.Invoke(json);
            }));
/* 
            Debug.Log($"{GetType().Name}-GetStockEquipmentList:\n{result}");
            OnGetStockEquipmentListAction?.Invoke(result); */
        }

        /// <summary>
        /// 取得庫存設備模型列表
        /// </summary>
        [Button]
        public void GetStockEquipmentModels()
        {
            List<Transform> result = forDemo_StockEquipment.GetStockEquipmentModels();
            Debug.Log($"{GetType().Name}-GetStockEquipmentModels:\n{result.CombineToString(true)}");
            OnGetStockEquipmentModelsAction?.Invoke(result);
        }


        #region Static Events
        /// <summary>
        /// 取得機櫃資料(json字串)
        /// </summary>
        public static Action<string> OnGetRackListInformationAction;
        /// <summary>
        /// 設得庫存設備資料列表(json字串)
        /// </summary>
//        public static Action<string> OnGetEquipmentAssetInStockAction;
        /// <summary>
        /// 取得設備模型列表
        /// </summary>
        public static Action<List<Transform>> OnGetEquipmentModelsAction;
        /// <summary>
        /// 取得庫存設備列表
        /// </summary>
        public static Action<string> OnGetStockEquipmentListAction;

        /// <summary>
        /// 取得庫存設備模型列表
        /// </summary>
        public static Action<List<Transform>> OnGetStockEquipmentModelsAction;
        #endregion

        [Serializable]
        public class ForDemo_RackInfo
        {
            public string jsonFileName_DCRList = "機房一data.json";
            public List<Transform> equipmentModels;

            /// <summary>
            /// 取得機房內機櫃群資訊
            /// </summary>
            public string GetRackListInformation()
            {
                return jsonFileName_DCRList;
                return FileHelper.LoadTextFileDirectly(jsonFileName_DCRList, EnumFilePath.streamingAssetsPath);
            }

            /// <summary>
            /// 取得上架庫存設備列表
            /// </summary>
            public string GetEquipmentAssetInStock() => "";

            /// <summary>
            /// 取得設備模型列表
            /// </summary>
            public List<Transform> GetEquipmentModels() => equipmentModels;
        }


        [Serializable]
        public class ForDemo_StockEquipment
        {
            public string jsonFileName_StockEquipment = "全球人壽/全球人壽_設備型號清單.json";
            public List<Transform> stockEquipmentModels;

            /// <summary>
            /// 取得庫存設備列表
            /// </summary>
            public string GetStockEquipmentList()
            {
                return jsonFileName_StockEquipment;
                return FileHelper.LoadTextFileDirectly(jsonFileName_StockEquipment, EnumFilePath.streamingAssetsPath);
            }

            public List<Transform> GetStockEquipmentModels() => stockEquipmentModels;
        }
    }
}
