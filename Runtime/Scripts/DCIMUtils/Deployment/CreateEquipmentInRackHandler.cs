using System;
using System.Collections.Generic;
using System.Linq;
using NaughtyAttributes;
using UnityEngine;
using VzDev.DCIMUtils.Import;
using VzDev.DCIMUtils.DataUtils;
using Random = UnityEngine.Random;

namespace VzDev.DCIMUtils.DeploymentUtils
{
    /// <summary>
    /// 產生機櫃內的設備
    /// </summary>
    public class CreateEquipmentInRackHandler : MonoBehaviour
    {
        #region Fields
        [SerializeField, ReadOnly] private List<DCR_Asset> dcrAssets = new List<DCR_Asset>();
        [SerializeField, ReadOnly] private List<Transform> equipmentModels = new List<Transform>();
        private bool isHaveData => dcrAssets != null && dcrAssets.Count > 0 || equipmentModels != null && equipmentModels.Count > 0;

        private int dataReadyCount = 0, dataReadyTotal = 2;
        #endregion

        private void SetEquipmentModels(List<Transform> list)
        {
            equipmentModels = list;
            TryCreateEquipmentAssetsInRack();
        }

        public void ParseRackListInformation(string json)
        {
           dcrAssets = RackAssetJsonConverter.ParseFromJson(json);
            TryCreateEquipmentAssetsInRack();
        }

        private void TryCreateEquipmentAssetsInRack()
        {
            if (++dataReadyCount < dataReadyTotal) return;
            ClearData();
            
            


            OnCreateEquipmentInRackEvent?.Invoke();
        }

        [Button, ShowIf("isHaveData")]
        private void ClearData()
        {
            dcrAssets.Clear();
            equipmentModels.Clear();
            dataReadyCount = 0;
        }

        #region EventListener
        private void OnEnable()
        {
            WebAPIManager_EquipmentDeploy.OnGetEquipmentModelsAction += SetEquipmentModels;
            WebAPIManager_EquipmentDeploy.OnGetRackListInformationAction += ParseRackListInformation;
        }

        private void OnDisable()
        {
            WebAPIManager_EquipmentDeploy.OnGetEquipmentModelsAction -= SetEquipmentModels;
            WebAPIManager_EquipmentDeploy.OnGetRackListInformationAction -= ParseRackListInformation;
        }
        #endregion

        public static Action OnCreateEquipmentInRackEvent;


    }
}