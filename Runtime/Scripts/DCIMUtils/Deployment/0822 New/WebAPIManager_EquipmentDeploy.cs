using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Networking;
using VzDev.ApiExtensions;
using VzDev.CoroutineUtils;
using VzDev.FileUtils;

namespace VzDev.DCIMUtils.DeploymentUtils
{
    /// <summary>
    /// WebAPI管理器：設備管理
    /// </summary>
    public class WebAPIManager_EquipmentDeploy : MonoBehaviour
    {

        #region For DEMO
        [Foldout("[For Demo]"), SerializeField, TextArea(1, 5)] private string jsonFilePath_DCRList = "", jsonFilePath_StockEquipment = "";
        [Foldout("[For Demo]"), SerializeField] private List<Transform> equipmentModels;

        [Button]
        private void BrowseDCRListFile()
        {
            jsonFilePath_DCRList = FileHelper.BrowseFilePanel();
            GetDCRList();
        }

        [Button]
        private void BrowseStockEquipmentFile()
        {
            jsonFilePath_StockEquipment = FileHelper.BrowseFilePanel();
            GetStockEquipmentList();
        }
        #endregion


        /// <summary>
        /// 取得機房內機櫃群資訊
        /// </summary>
        [Button]
        public void GetDCRList()
        {
            TextFileLoader.LoadTextFileCoroutine(jsonFilePath_DCRList, (json) =>
            {
                Debug.Log($"{GetType().Name}-GetDCRList:\n{json}");
                OnGetRackListInformationAction?.Invoke(json);
            }, (error) =>
            {
                Debug.LogError($"Error loading file: {error}");
                OnGetRackListInformationAction?.Invoke(null);
            });
        }

        /// <summary>
        /// 取得庫存設備資料列表
        /// </summary>
        [Button]
        public void GetStockEquipmentList()
        {
            TextFileLoader.LoadTextFileCoroutine(jsonFilePath_StockEquipment, (json) =>
            {
                Debug.Log($"{GetType().Name}-GetStockEquipmentList:\n{json}");
                OnGetStockEquipmentListAction?.Invoke(json);
            }, (error) =>
            {
                Debug.LogError($"Error loading file: {error}");
                OnGetStockEquipmentListAction?.Invoke(null);
            });
        }

        /// <summary>
        /// 取得設備模型列表
        /// </summary>
        [Button]
        public void GetEquipmentModels() => OnGetEquipmentModelsAction?.Invoke(equipmentModels);

        /// <summary>
        /// 取得庫存設備模型列表
        /// </summary>
        [Button]
        public void GetStockEquipmentModels() => OnGetStockEquipmentModelsAction?.Invoke(equipmentModels);

        #region Static Events
        /// <summary>
        /// 取得機櫃資料(json字串)
        /// </summary>
        public static Action<string> OnGetRackListInformationAction;
        /// <summary>
        /// 取得設備模型列表
        /// </summary>
        public static Action<List<Transform>> OnGetEquipmentModelsAction;
        /// <summary>
        /// 取得庫存設備列表(json字串)
        /// </summary>
        public static Action<string> OnGetStockEquipmentListAction;
        /// <summary>
        /// 取得庫存設備模型列表
        /// </summary>
        public static Action<List<Transform>> OnGetStockEquipmentModelsAction;
        #endregion
    }
}
