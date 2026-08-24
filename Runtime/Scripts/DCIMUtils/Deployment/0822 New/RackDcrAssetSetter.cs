using System;
using System.Collections.Generic;
using NaughtyAttributes;
using Newtonsoft.Json;
using UnityEngine;
using VzDev.DCIMUtils.DataUtils;
using VzDev.UnityAPI.Extensions;

namespace VzDev.DCIMUtils.DeploymentUtils
{
    public class RackDcrAssetSetter : MonoBehaviour
    {
        [SerializeField, ReadOnly] private List<DCR_Asset> rackAssets;
        [SerializeField, ReadOnly] private List<Transform> rackModels;
        [SerializeField, ReadOnly] private List<Transform> equipmentModels;

        private bool isHaveData => (rackModels != null && rackAssets != null) || (rackModels.Count > 0 && rackAssets.Count > 0);
        private int dataReadyCount = 0, dataReadyCountMax = 3;

        private void Awake() => dataReadyCount = 0;

                /// <summary>
        /// 解析機櫃Json資料
        /// </summary>
        public void SetRackAssets(string jsonString) 
        {
            var rackAssetDtoList = JsonConvert.DeserializeObject<List<DCR_Asset_DTO>>(jsonString);
            rackAssets?.Clear();
            rackAssets = new List<DCR_Asset>();
            for (int i = 0; i < rackAssetDtoList.Count; i++)
            {
                rackAssets.Add(rackAssetDtoList[i].ToDCRAsset());
            }
            GenerateDataCombiner();
        }

        /// <summary>
        /// 設置機櫃模型列表
        /// </summary>
        public void SetRackModels(List<Transform> models)
        {
            rackModels = models;
            for (int i = 0; i < rackModels.Count; i++)
            {
                rackModels[i].gameObject.TryAddComponent<DataModelBinder_Rack>();
            }
            GenerateDataCombiner();
        }

        /// <summary>
        /// 設定資產設備模型列表
        /// </summary>
        private void SetEquipmentModels(List<GameObject> list)
        {
            equipmentModels = new List<Transform>();
            for (int i = 0; i < list.Count; i++)
            {
                equipmentModels.Add(list[i].transform);
            }
            GenerateDataCombiner();
        }


         private void GenerateDataCombiner()
        {
            if (++dataReadyCount < dataReadyCountMax) return;
            for (int i = 0; i < rackAssets.Count; i++)
            {
                DCR_Asset rackAsset = rackAssets[i];
                Transform rackModel = rackModels.Find(r => r.name.GetStringBetweenMarks("[", "]") == rackAsset.deviceCode);
                if (rackModel != null)
                {
                    // 將 DCR_Asset 資料與對應的機櫃模型綁定
                    rackModel.gameObject.TryAddComponent(out DataModelBinder_Rack dataCombiner);
                    dataCombiner.SetRackAsset(rackAsset);

                    rackAsset.container.ForEach(equipment =>
                    {
                        Transform equipmentModel = equipmentModels.Find(m => m.name.GetStringBetweenMarks("[", "]") == equipment.deviceCode);
                        if (equipmentModel != null)
                        {
                            equipmentModel.gameObject.TryAddComponent(out DataModelBinder_Equipment dataCombiner_Equipment);
                            dataCombiner_Equipment.SetEquipmentAsset(equipment);
                        }
                        else
                        {
                            Debug.LogWarning($"找不到對應的設備模型: {equipment.deviceCode}");
                        }
                    });
                }
                else
                {
                    Debug.LogWarning($"找不到對應的機櫃模型: {rackAsset.deviceCode}");
                }
            }
        }

        [Button, ShowIf("isHaveData")]
        private void ClearData()
        {
            rackModels?.Clear();
            rackAssets?.Clear();
        }


        private void OnEnable()
        {
            WebAPIManager.OnGetRackListInformationAction += SetRackAssets;
            WebAPIManager.OnGetEquipmentModelsAction += SetEquipmentModels;
        }

        private void OnDisable()
        {
            WebAPIManager.OnGetRackListInformationAction -= SetRackAssets;
            WebAPIManager.OnGetEquipmentModelsAction -= SetEquipmentModels;
        }
    }
}
