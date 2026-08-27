using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using VzDev.UnityAPI.Extensions;

namespace VzDev.DCIMUtils.DeploymentUtils
{
    public class StockEquipementHandler : MonoBehaviour
    {
        #region Fields
        public List<StockEquipment> stockEquipmentList;
        private List<Transform> stockEquipmentModels;
        private int dataReadyCount = 0, dataReadyCountTarget = 2;
        #endregion

        /// <summary>
        /// 取得庫存設備資料列表
        /// </summary>
        private void HandleGetStockEquipmentList(string json)
        {
            stockEquipmentList = JsonConvert.DeserializeObject<List<StockEquipment>>(json);
            CombineDataAndModel();
        }

        private void CombineDataAndModel()
        {
            if (++dataReadyCount < dataReadyCountTarget) return;
            stockEquipmentList.ForEach(equipment =>
            {
                Transform model = stockEquipmentModels.Find(model => model.name.ContainKeyword(equipment.modelName));
                if (model == null)
                {
                    Debug.LogWarning($"[StockEquipementHandler] 找不到對應的設備模型: {equipment.modelName}");

                }
                else
                {
                    equipment.modelPrefab = model;
                }
            });
        }


        /// <summary>
        /// 取得庫存設備模型列表
        /// </summary>
        private void HandleGetStockEquipmentModels(List<Transform> list)
        {
            stockEquipmentModels = list;
            CombineDataAndModel();
        }

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


    }

    [Serializable]
    public class StockEquipment
    {
        public string modelName;
        public string brand;
        public string system;
        public int power;
        public float weight;
        public int heightU;
        public Transform modelPrefab;

        public string deviceCode;
    }
}
