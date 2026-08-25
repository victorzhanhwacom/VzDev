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
        public ForDemo forDemo;
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
        #endregion

        [Serializable]
        public class ForDemo
        {
            public string jsonFileName = "機房一.json";
            public List<Transform> equipmentModels;

            /// <summary>
            /// 取得機房內機櫃群資訊
            /// </summary>
            public string GetRackListInformation() => FileHelper.LoadTextFileDirectly(jsonFileName, EnumFilePath.streamingAssetsPath);

            /// <summary>
            /// 取得上架庫存設備列表
            /// </summary>
            public string GetEquipmentAssetInStock() => "";

            /// <summary>
            /// 取得設備模型列表
            /// </summary>
            public List<Transform> GetEquipmentModels() => equipmentModels;
        }
    }
}
