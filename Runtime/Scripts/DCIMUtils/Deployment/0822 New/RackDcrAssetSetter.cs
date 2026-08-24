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
        #region Fields
        [SerializeField, OnValueChanged("OnRackClickableChanged")] private bool isRackClickable = true;
        private void OnRackClickableChanged() => SetRackClickable(isRackClickable);
        [SerializeField] private bool showList = true;
        [SerializeField, ReadOnly, ShowIf("showList")] private List<DCR_Asset> rackAssets;
        [SerializeField, ReadOnly, ShowIf("showList")] private List<Transform> rackModels;
        [SerializeField, ReadOnly, ShowIf("showList")] private List<Transform> equipmentModels;

        [SerializeField, ReadOnly] private List<DataModelBinder_Rack> rackDataModelBinders = new List<DataModelBinder_Rack>();

        private bool isHaveData => isRackDataReady && isRackModelReady && isEquipmentModelReady;
        private bool isRackDataReady => rackAssets != null && rackAssets.Count > 0;
        private bool isRackModelReady => rackModels != null && rackModels.Count > 0;
        private bool isEquipmentModelReady => equipmentModels != null && equipmentModels.Count > 0;
        #endregion

        public void SetRackClickable(bool isClickable)
        {
            isRackClickable = isClickable;
            rackDataModelBinders.ForEach(combiner => combiner.SetColliderEnabled(isRackClickable));
        }

        /// <summary>
        /// 解析機櫃Json資料
        /// </summary>
        public void SetRackAssets(string jsonString)
        {
            rackAssets?.Clear();
            rackAssets = new List<DCR_Asset>();
            var rackAssetDtoList = JsonConvert.DeserializeObject<List<DCR_Asset_DTO>>(jsonString);
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
                rackModels[i].gameObject.TryAddComponent(out DataModelBinder_Rack dataCombiner);
                rackDataModelBinders.Add(dataCombiner);
            }
            GenerateDataCombiner();
        }

        /// <summary>
        /// 設定資產設備模型列表
        /// </summary>
        public void SetEquipmentModels(List<Transform> list)
        {
            equipmentModels = list;
            GenerateDataCombiner();
        }

        private void GenerateDataCombiner()
        {
            if (!isRackDataReady || !isRackModelReady || !isEquipmentModelReady) return;

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
                        Transform equipmentModel = equipmentModels.Find(m => m.name.ContainKeyword(System.StringComparison.OrdinalIgnoreCase, equipment.deviceCode));
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
            rackDataModelBinders.ForEach(combiner => combiner.ToDestroy());
            rackDataModelBinders?.Clear();
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
