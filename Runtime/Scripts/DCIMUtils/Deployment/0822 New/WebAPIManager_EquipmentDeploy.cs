using System;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;
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
            Debug.Log($"{GetType().Name}-GetRackListInformation:\n{result}");
            OnGetRackListInformationAction?.Invoke(result);
        }

        /// <summary>
        /// 取得上架庫存設備列表
        /// </summary>
        [Button]
        public void GetEquipmentAssetInStock()
        {
            string result = forDemo.GetEquipmentAssetInStock();
            Debug.Log($"{GetType().Name}-GetEquipmentAssetInStock:\n{result}");
            OnGetEquipmentAssetInStockAction?.Invoke(result);
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
            Debug.Log($"{GetType().Name}-GetStockEquipmentList:\n{result}");
            OnGetStockEquipmentListAction?.Invoke(result);
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
        public static Action<string> OnGetEquipmentAssetInStockAction;
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
            public string jsonFileName_DCRList = "機房一.json";
            public List<Transform> equipmentModels;

            /// <summary>
            /// 取得機房內機櫃群資訊
            /// </summary>
            public string GetRackListInformation() => FileHelper.LoadTextFileDirectly(jsonFileName_DCRList, EnumFilePath.streamingAssetsPath);

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
            public string GetStockEquipmentList() => FileHelper.LoadTextFileDirectly(jsonFileName_StockEquipment, EnumFilePath.streamingAssetsPath);
            public List<Transform> GetStockEquipmentModels() => stockEquipmentModels;
        }
    }
}
