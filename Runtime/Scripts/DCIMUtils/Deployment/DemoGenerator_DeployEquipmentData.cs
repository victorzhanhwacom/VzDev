using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;
using VzDev.DCIM.RevitAssetDataStructure;

namespace VzDev.DCIMUtils.Deployment.Demo
{
    public class DemoGenerator_DeployEquipmentData : MonoBehaviour
    {
        [SerializeField, Range(1, 100)] private int generateCount = 10;
        [SerializeField] private List<GameObject> equipmentModels = new List<GameObject>();
        [Foldout("[Events]")] public UnityEvent<List<EquipmentAsset>> OnGetDeployEquipmentAssets;

        /// <summary>
        /// 產生模擬的設備資產資料 (DEMO用)
        /// </summary>
        [Button]
        public void GenerateData() => GenerateDeployEquipmentAssetsForDemo(generateCount);
        private void GenerateDeployEquipmentAssetsForDemo(int count)
        {
            List<EquipmentAsset> equipmentAssets = new List<EquipmentAsset>();

            for (int i = 0; i < count; i++)
            {
                EquipmentAsset equipmentAsset = new EquipmentAsset();
                equipmentAsset.category = UnityEngine.Random.Range(0, 2) switch
                {
                    0 => DCIMCategory.DCS,
                    1 => DCIMCategory.DCN,
                    _ => DCIMCategory.Unknow
                };
                equipmentAsset.companyPropertyInfo = new CompanyPropertyInfo
                {
                    propertyName = $"Equipment {equipmentAsset.category}-{i + 1}",
                    sizeInfo = new SizeInfo
                    {
                        width_mm = UnityEngine.Random.Range(400, 600),
                        height_mm = UnityEngine.Random.Range(1800, 2200),
                        depth_mm = UnityEngine.Random.Range(700, 900)
                    },
                    note = $"This is equipment {i + 1}"
                };
                equipmentAsset.companyPropertyInfo.GenerateRandomPropertyNo();
                equipmentAsset.equipmentUsageInfo = new EquipmentUsageInfo
                {
                    power_watt = UnityEngine.Random.Range(100, 1000),
                    weight_kg = UnityEngine.Random.Range(10f, 100f),
                    heightU = UnityEngine.Random.Range(1, 10)
                };
                equipmentAsset.deploymentStatus = DeploymentStatus.InStock;
                equipmentAssets.Add(equipmentAsset);

                if(equipmentModels == null || equipmentModels.Count == 0)
                {
                    Debug.LogWarning("No equipment models assigned for demo generation.");
                    continue;
                }
                equipmentAsset.modelInfo = new ModelInfo
                {
                    modelTarget = equipmentModels[UnityEngine.Random.Range(0, equipmentModels.Count)].transform
                };
            }
            OnGetDeployEquipmentAssets?.Invoke(equipmentAssets);
        }
    }
}