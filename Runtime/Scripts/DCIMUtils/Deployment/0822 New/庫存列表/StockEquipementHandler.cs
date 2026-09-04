using System;
using System.Collections.Generic;
using NaughtyAttributes;
using Newtonsoft.Json;
using UnityEngine;
using VzDev.DCIMUtils;
using VzDev.DCIMUtils.DataUtils;
using VzDev.UnityAPI.Extensions;

namespace VzDev.DCIMUtils.DeploymentUtils
{
    /// <summary>
    /// 處理庫存設備資料與庫存設備模型的管理器
    /// <para>+ 處理資料型式轉換</para>
    /// <para>+ 將資料與模型結合</para>
    /// </summary>
    public class StockEquipementHandler : MonoBehaviour
    {
        #region Fields
        [SerializeField] private List<EquipmentAsset> stockEquipmentList;
        /// <summary>
        /// 庫存設備資料
        /// </summary>
        private List<EquipmentAsset_StockDTO> stockEquipmentData;
        /// <summary>
        /// 庫存設備模型
        /// </summary>
        private List<Transform> stockEquipmentModels;
        private int dataReadyCount = 0, dataReadyCountTarget = 2;

        private bool IsDataReady => dataReadyCount >= dataReadyCountTarget;
        #endregion

        /// <summary>
        /// 取得庫存設備資料列表
        /// </summary>
        private void HandleGetStockEquipmentList(string json)
        {
            stockEquipmentData = JsonConvert.DeserializeObject<List<EquipmentAsset_StockDTO>>(json);
            CombineDataAndModel();
        }

        /// <summary>
        /// 取得庫存設備模型列表
        /// </summary>
        private void HandleGetStockEquipmentModels(List<Transform> list)
        {
            stockEquipmentModels = list;
            CombineDataAndModel();
        }

        /// <summary>
        /// 將庫存設備資料與模型結合，並觸發OnGetStockEquipmentListAction事件
        /// </summary>
        private void CombineDataAndModel()
        {
            dataReadyCount++;
            if (!IsDataReady) return;

            stockEquipmentList = new List<EquipmentAsset>();
            stockEquipmentData.ForEach(stockEquipment =>
            {
                Transform model = stockEquipmentModels.Find(model => DCIM_Helper.CompareEquipmentModelName(model.name, stockEquipment.modelName));
                if (model == null)
                {
                    Debug.LogWarning($"[StockEquipementHandler] 找不到對應的設備模型: {stockEquipment.modelName}");

                }
                else
                {
                    EquipmentAsset equipmentAsset = stockEquipment.ToEquipmentAsset();
                    equipmentAsset.modelInfo.modelTarget = model;
                    equipmentAsset.modelInfo.modelName = stockEquipment.modelName;
                    stockEquipmentList.Add(equipmentAsset);
                }
            });
            OnCombineStockeEquipmentAndModelAction?.Invoke(stockEquipmentList);
        }

        [Button, ShowIf("IsDataReady")]
        private void ClearData()
        {
            stockEquipmentData?.Clear();
            dataReadyCount = 1;
        }

        #region Event Listener
        private void OnEnable()
        {
            WebAPIManager_EquipmentDeploy.OnGetStockEquipmentListAction += HandleGetStockEquipmentList;
            WebAPIManager_EquipmentDeploy.OnGetStockEquipmentModelsAction += HandleGetStockEquipmentModels;
        }

        private void OnDisable()
        {
            WebAPIManager_EquipmentDeploy.OnGetStockEquipmentListAction -= HandleGetStockEquipmentList;
            WebAPIManager_EquipmentDeploy.OnGetStockEquipmentModelsAction -= HandleGetStockEquipmentModels;
        }
        #endregion

        #region Static Events
        /// <summary>
        /// 在庫存設備資料與模型結合完成後觸發，傳遞結合後的庫存設備列表
        /// </summary>
        public static event Action<List<EquipmentAsset>> OnCombineStockeEquipmentAndModelAction;
        #endregion
    }
}
