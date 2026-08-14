using System;
using System.Collections.Generic;
using System.Linq;
using NaughtyAttributes;
using UnityEngine;
using VzDev.DCIMUtils.DataUtils;
using Random = UnityEngine.Random;

namespace VzDev.DCIMUtils.Deployment
{
    /// <summary>
    /// 產生上架庫存設備列表
    /// </summary>
    public class EquipmentAssetInStockHandler : MonoBehaviour
    {
        #region Fields
        [SerializeField] private bool isGenerateDemoData = false;
        [SerializeField, ShowIf("isGenerateDemoData")] private int demoDataCount = 30;
        [SerializeField, ReadOnly] private List<EquipmentAsset> equipmentAssetsInStock = new List<EquipmentAsset>();
        [SerializeField, ReadOnly] private List<GameObject> equipmentModels = new List<GameObject>();
        private bool isHaveData => equipmentAssetsInStock != null && equipmentAssetsInStock.Count > 0;

        private int dataReadyCount = 0, dataReadyTotal = 2;
        private string jsonData;
        #endregion

        public void SetEquipmentAssetInStock(bool isSuccess, string json)
        {
            if (!isSuccess)
            {
                Debug.LogError("Failed to get equipment asset in stock.");
                return;
            }
            jsonData = json;
            TryCreateEquipmentAssetsInStock();
        }

        public void SetEquipmentModels(bool isSuccess, List<GameObject> list)
        {
            if (!isSuccess)
            {
                Debug.LogError("Failed to get equipment models.");
                return;
            }
            equipmentModels = list;
            TryCreateEquipmentAssetsInStock();
        }

        private void TryCreateEquipmentAssetsInStock()
        {
            if (++dataReadyCount < dataReadyTotal) return;
            ClearData();
            if (isGenerateDemoData) equipmentAssetsInStock = DEMO_EquipmentAssetInStock.Generate(equipmentModels, demoDataCount);
            else
            {
                Debug.Log("解析 jsonData 並生成設備資產列表，待補齊…");
            }
            OnCreateEquipmentAssetsInStockEvent?.Invoke(equipmentAssetsInStock);
        }

        [Button, ShowIf("isHaveData")]
        private void ClearData()
        {
            equipmentAssetsInStock.Clear();
            dataReadyCount = 0;
        }

        #region EventListener
        private void OnEnable()
        {
            WebAPIManager.OnGetEquipmentAssetInStockAction += SetEquipmentAssetInStock;
            WebAPIManager.OnGetEquipmentModelsAction += SetEquipmentModels;
        }

        private void OnDisable()
        {
            WebAPIManager.OnGetEquipmentAssetInStockAction -= SetEquipmentAssetInStock;
            WebAPIManager.OnGetEquipmentModelsAction -= SetEquipmentModels;
        }
        #endregion

        public static Action<List<EquipmentAsset>> OnCreateEquipmentAssetsInStockEvent;

        #region [For Demo] 產生上架庫存設備列表
        /// <summary>
        /// [For Demo] 產生上架庫存設備列表
        /// </summary>
        private static class DEMO_EquipmentAssetInStock
        {
            public static List<EquipmentAsset> Generate(List<GameObject> equipmentModels, int count = 30)
            {
                if (equipmentModels == null || equipmentModels.Count == 0 || count <= 0)
                {
                    Debug.LogWarning("No equipment models assigned for demo generation.");
                    return new List<EquipmentAsset>();
                }

                List<EquipmentAsset> result = new();
                for (int i = 0; i < count; i++)
                {
                    GameObject model = equipmentModels[Random.Range(0, equipmentModels.Count)];
                    string deviceCode = model.name + "+" + Random.Range(1000, 9999).ToString("D4");

                    EquipmentAsset equipmentAsset = new EquipmentAsset
                    {
                        deviceCode = deviceCode,
                        deploymentStatus = DeploymentStatus.InStock,
                        category = model.name.Contains("Server") ? DCIMCategory.DCS : DCIMCategory.DCN,
                        companyPropertyInfo = new CompanyPropertyInfo
                        {
                            propertyName = deviceCode,
                            note = $"{deviceCode} in stock"
                        },
                    };

                    equipmentAsset.companyPropertyInfo.GenerateRandomPropertyNo();
                    equipmentAsset.sizeInfo = new SizeInfo
                    {
                        width_mm = Random.Range(400, 600),
                        height_mm = Random.Range(1800, 2200),
                        depth_mm = Random.Range(700, 900)
                    };
                    equipmentAsset.equipmentUsageInfo = new EquipmentUsageInfo
                    {
                        power_watt = Random.Range(100, 1000),
                        weight_kg = Random.Range(10f, 100f),
                        heightU = model.name.Split('-').LastOrDefault(part => part.EndsWith("U"))?.Replace("U", "") is string heightUString && int.TryParse(heightUString, out int heightU) ? heightU : 1
                    };
                    equipmentAsset.modelInfo = new ModelInfo
                    {
                        modelTarget = model.transform,
                        modelName = deviceCode
                    };

                    result.Add(equipmentAsset);
                }
                return result;
            }
        }
        #endregion
    }
}