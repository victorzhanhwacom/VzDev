using System;
using System.Collections.Generic;
using System.Linq;
using NaughtyAttributes;
using UnityEngine;
using VzDev.DCIMUtils.Import;
using VzDev.DCIMUtils.DataUtils;
using Random = UnityEngine.Random;

namespace VzDev.DCIMUtils.Deployment
{
    /// <summary>
    /// 產生機櫃內的設備
    /// </summary>
    public class CreateEquipmentInRackHandler : MonoBehaviour
    {
        #region Fields
        [SerializeField, ReadOnly] private List<DCR_Asset> dcrAssets = new List<DCR_Asset>();
        [SerializeField, ReadOnly] private List<GameObject> equipmentModels = new List<GameObject>();
        private bool isHaveData => dcrAssets != null && dcrAssets.Count > 0 || equipmentModels != null && equipmentModels.Count > 0;

        private int dataReadyCount = 0, dataReadyTotal = 2;
        #endregion

        private void SetEquipmentModels(bool isSuccess, List<GameObject> list)
        {
            if (!isSuccess)
            {
                Debug.LogError("Failed to get equipment models.");
                return;
            }
            equipmentModels = list;
            TryCreateEquipmentAssetsInRack();
        }

        public void ParseRackListInformation(bool isSuccess, string json)
        {
            if (!isSuccess)
            {
                Debug.LogError("Failed to get equipment asset in stock.");
                return;
            }
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
            WebAPIManager.OnGetEquipmentModelsAction += SetEquipmentModels;
            WebAPIManager.OnGetRackListInformationAction += ParseRackListInformation;
        }

        private void OnDisable()
        {
            WebAPIManager.OnGetEquipmentModelsAction -= SetEquipmentModels;
            WebAPIManager.OnGetRackListInformationAction -= ParseRackListInformation;
        }
        #endregion

        public static Action OnCreateEquipmentInRackEvent;


    }
}