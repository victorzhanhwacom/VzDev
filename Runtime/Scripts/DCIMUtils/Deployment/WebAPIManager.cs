using System;
using System.Collections.Generic;
using UnityEngine;
using VzDev.DCIM.RevitAssetDataStructure;

namespace VzDev.DCIMUtils.Deployment
{
    public class WebAPIManager : MonoBehaviour
    {
        public void SetRackAssets(List<DCR_Asset> dcrAsset) => OnGetRackAssets?.Invoke(dcrAsset);
        public static Action<List<DCR_Asset>> OnGetRackAssets;


        public void SetDeployEquipmentData(List<EquipmentAsset> equipmentAssets)
        {
            OnGetDeployEquipmentAssets?.Invoke(equipmentAssets);
        }



        public static Action<List<EquipmentAsset>> OnGetDeployEquipmentAssets;
        public static Action<string> OnGetDeployEquipmentAssetsFaield;
    }
}
