using System;
using System.Collections.Generic;
using NaughtyAttributes;
using Newtonsoft.Json;
using UnityEngine;
using VzDev.DCIMUtils.DataUtils;
using VzDev.UnityAPI.Extensions;
using System.Linq;
using VzDev.ApiExtensions;
using VzDev.DebugUtils;

namespace VzDev.DCIMUtils.DeploymentUtils
{
    public class RackDcrAssetSetter : MonoBehaviour
    {
        #region Fields
        [SerializeField, OnValueChanged("OnRackSelectableChanged")] private bool isRackSelectable = true;
        [SerializeField, OnValueChanged("OnRackDeployable")] private bool isRackDeployable = true;
        private void OnRackSelectableChanged() => SetRackSelectable(isRackSelectable);
        private void OnRackDeployable() => SetRackDeployable(isRackDeployable);
        [SerializeField] private BoxCollider rackSlotColliderPrefab;

        private List<DCR_Asset> rackAssets;
        private List<Transform> rackModels;
        private List<Transform> equipmentModels;

        [SerializeField, ReadOnly] private List<DataModelBinder_Rack> rackDataModelBinders = new List<DataModelBinder_Rack>();

        private bool isHaveData => isRackDataReady && isRackModelReady && isEquipmentModelReady;
        private bool isRackDataReady => rackAssets != null && rackAssets.Count > 0;
        private bool isRackModelReady => rackModels != null && rackModels.Count > 0;
        private bool isEquipmentModelReady => equipmentModels != null && equipmentModels.Count > 0;
        #endregion

        /// <summary>
        /// 設置機櫃是否可被選取
        /// </summary>
        public void SetRackSelectable(bool isClickable)
        {
            isRackSelectable = isClickable;
            rackDataModelBinders.ForEach(combiner => combiner.SetRackColliderEnabled(isRackSelectable));
        }

        /// <summary>
        /// 設置機櫃是否可被部署設備
        /// </summary>
        public void SetRackDeployable(bool isDeployable)
        {
            isRackDeployable = isDeployable;
            rackDataModelBinders.ForEach(combiner => combiner.SetRackSlotColliderEnabled(isRackDeployable));
        }
        

        /// <summary>
        /// 解析機櫃Json資料
        /// </summary>
        public void SetRackAssets(string jsonString)
        {
            rackAssets?.Clear();
            rackAssets ??= new List<DCR_Asset>();
            var rackAssetDtoList = JsonConvert.DeserializeObject<List<DCR_Asset_DTO>>(jsonString);
            rackAssets.AddRange(rackAssetDtoList.Select(d => d.ToDCRAsset()));
            GenerateDataCombiner();
        }

        /// <summary>
        /// 設置機櫃模型列表
        /// </summary>
        public void SetRackModels(List<Transform> models)
        {
            rackModels = models;
            foreach (Transform t in rackModels)
            {
                t.gameObject.TryAddComponent(out DataModelBinder_Rack dataCombiner);
                rackDataModelBinders.TryAdd(dataCombiner);
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

            foreach (DCR_Asset rackAsset in rackAssets)
            {
                // 比對deviceCode，找到對應的機櫃模型
                Transform rackModel = rackModels.Find(r => r.name.GetStringBetweenMarks("[", "]") == rackAsset.deviceCode);
                if (rackModel == null)
                {
                    Debug.LogWarning($"找不到對應的機櫃模型: {rackAsset.deviceCode}");
                    continue;
                }
                // 將 DCR_Asset 資料與對應的機櫃模型綁定
                rackModel.gameObject.TryAddComponent(out DataModelBinder_Rack rackDataCombiner);
                rackDataCombiner.SetRackAsset(rackAsset);

                if (Application.isPlaying == false) continue; // 編輯模式下不生成模型，避免場景中出現多餘的物件

                rackDataCombiner.GenerateRackSlotCollider(rackSlotColliderPrefab);
                rackDataCombiner.GenerateEquipmentInContainer(equipmentModels);
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
            WebAPIManager_EquipmentDeploy.OnGetRackListInformationAction += SetRackAssets;
            WebAPIManager_EquipmentDeploy.OnGetEquipmentModelsAction += SetEquipmentModels;
        }

        private void OnDisable()
        {
            WebAPIManager_EquipmentDeploy.OnGetRackListInformationAction -= SetRackAssets;
            WebAPIManager_EquipmentDeploy.OnGetEquipmentModelsAction -= SetEquipmentModels;
        }
    }
}
