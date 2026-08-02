using System;
using System.Collections.Generic;
using UnityEngine;
using VzDev.DCIM.RevitAssetDataStructure;

namespace VzDev.DCIMUtils.Deployment
{
    public class WebAPIManager : MonoBehaviour
    {
        public void SetDeployEquipmentData(List<EquipmentAsset> equipmentAssets)
        {
            OnGetDeployEquipmentAssets?.Invoke(equipmentAssets);
        }

        public static Action<List<EquipmentAsset>> OnGetDeployEquipmentAssets;
        public static Action<string> OnGetDeployEquipmentAssetsFaield;
    }
}
