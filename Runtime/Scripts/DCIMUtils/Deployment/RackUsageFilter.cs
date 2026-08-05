using System;
using System.Collections.Generic;
using System.Linq;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.UI;
using VzDev.DCIM.RevitAssetDataStructure;

namespace VzDev.DCIMUtils.Deployment
{
    /// <summary>
    /// 機櫃條件篩選器，根據設備的尺寸和位置來篩選可放置的設備。
    /// </summary>
    public class RackUsageFilter : MonoBehaviour
    {
        #region Fields
        [SerializeField, ReadOnly] private EquipmentAsset currentSelectedEquipment;
        [SerializeField, ReadOnly] private List<RackComponent> rackComponents = new();
        [SerializeField]
        private List<ColorItem> colorItems = new()
        {
            new ColorItem { thresold = 20, color = Color.red },
            new ColorItem { thresold = 50, color = Color.yellow },
            new ColorItem { thresold = 80, color = Color.green }
        };
        [Foldout("[Components]"), SerializeField] private Toggle togglePower, toggleWeight, toggleUSpacer;
        #endregion

        /// <summary>
        /// 根據目前所選設備的尺寸和位置，篩選出可放置的機櫃模型。
        /// </summary>
        public void FilterRackModels()
        {
            Debug.Log("FilterRackModels...");
            /*  List<Transform> filteredModels = rackModels.Where(model =>
             {
                 var rackUsage = model.GetComponent<RackUsage>();
                 if (rackUsage == null) return false;

                 bool isPowerValid = !togglePower.isOn || rackUsage.IsPowerSufficient(currentSelectedEquipment);
                 bool isWeightValid = !toggleWeight.isOn || rackUsage.IsWeightSufficient(currentSelectedEquipment);
                 bool isUSpacerValid = !toggleUSpacer.isOn || rackUsage.IsUSpacerSufficient(currentSelectedEquipment);

                 return isPowerValid && isWeightValid && isUSpacerValid;
             }).ToList(); */
        }

        #region LifeCycle
        /// <summary>
        /// 目前所選上架的庫存設備
        /// </summary>
        private void OnSelectedeEquipmentToDeploy(EquipmentAsset asset)
        {
            currentSelectedEquipment = asset;
            FilterRackModels();
        }
        private void OnSetComponentsCompleted(List<RackComponent> list) => rackComponents = list;
        private void OnEnable()
        {
            DeployEquipmentPlacementController.onSelectedeEquipmentToDeploy += OnSelectedeEquipmentToDeploy;
            RackComponentSetter.OnSetComponentsCompletedAction += OnSetComponentsCompleted;
            togglePower.onValueChanged.AddListener((value) => FilterRackModels());
            toggleWeight.onValueChanged.AddListener((value) => FilterRackModels());
            toggleUSpacer.onValueChanged.AddListener((value) => FilterRackModels());
        }
        private void OnDisable()
        {
            DeployEquipmentPlacementController.onSelectedeEquipmentToDeploy -= OnSelectedeEquipmentToDeploy;
            RackComponentSetter.OnSetComponentsCompletedAction += OnSetComponentsCompleted;
            togglePower.onValueChanged.RemoveAllListeners();
            toggleWeight.onValueChanged.RemoveAllListeners();
            toggleUSpacer.onValueChanged.RemoveAllListeners();
        }
        #endregion

        [System.Serializable]
        private class ColorItem
        {
            public int thresold;
            [ColorUsage(true, true)]
            public Color color;
        }
    }
}
