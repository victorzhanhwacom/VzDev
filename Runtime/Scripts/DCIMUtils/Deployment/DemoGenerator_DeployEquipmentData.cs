using System.Collections.Generic;
using System.Linq;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;
using VzDev.DCIM.RevitAssetDataStructure;
using Random = UnityEngine.Random;

namespace VzDev.DCIMUtils.Deployment.Demo
{
    public class DemoGenerator_DeployEquipmentData : MonoBehaviour
    {
        #region Fields
        [SerializeField, ReadOnly] private List<EquipmentAsset> equipmentAssets = new List<EquipmentAsset>();
        [SerializeField] private List<GameObject> equipmentModels = new List<GameObject>();
        [Foldout("[Events]")] public UnityEvent<List<EquipmentAsset>> OnGetDeployEquipmentAssets;
        private bool isHaveData => equipmentAssets != null && equipmentAssets.Count > 0;
        #endregion

        /// <summary>
        /// 產生模擬的設備資產資料 (DEMO用)
        /// </summary>
        [Button]
        public void GenerateDeployEquipmentAssetsForDemo()
        {
            ClearData();
            for (int i = 0; i < equipmentModels.Count; i++)
            {
                EquipmentAsset equipmentAsset = new EquipmentAsset();
                equipmentAsset.deviceCode = equipmentModels[i].name;
                equipmentAsset.category = equipmentModels[i].name.Contains("Server") ? DCIMCategory.DCS : DCIMCategory.DCN;
                equipmentAsset.companyPropertyInfo = new CompanyPropertyInfo
                {
                    propertyName = equipmentModels[i].name.Substring(5),
                    note = $"This is equipment {i + 1}"
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
                    heightU = equipmentModels[i].name.Split('-').LastOrDefault(part => part.EndsWith("U"))?.Replace("U", "") is string heightUString && int.TryParse(heightUString, out int heightU) ? heightU : 1
                };
                equipmentAsset.deploymentStatus = DeploymentStatus.InStock;
                equipmentAssets.Add(equipmentAsset);

                if (equipmentModels == null || equipmentModels.Count == 0)
                {
                    Debug.LogWarning("No equipment models assigned for demo generation.");
                    continue;
                }
                equipmentAsset.modelInfo = new ModelInfo
                {
                    modelTarget = equipmentModels[i].transform
                };
            }
            OnGetDeployEquipmentAssets?.Invoke(equipmentAssets);
        }

        [Button, ShowIf("isHaveData")]
        private void ClearData() => equipmentAssets.Clear();
    }
}