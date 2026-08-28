using System;
using System.Collections.Generic;
using System.Linq;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.UI;
using VzDev.ColorUtils;
using VzDev.DCIMUtils.DataUtils;

namespace VzDev.DCIMUtils.DeploymentUtils
{
    /// <summary>
    /// 機櫃條件篩選器，根據設備的尺寸和位置來篩選可放置的設備。
    /// </summary>
    public class RackUsageFilter : MonoBehaviour
    {
        #region Fields
        [SerializeField, ReadOnly] private EquipmentAsset selectedStockEquipment;
        [SerializeField]
        private List<ColorThresholdItem> colorThresholds = new()
        {
            new ColorThresholdItem { threshold = 0, color = Color.gray },
            new ColorThresholdItem { threshold = 30, color = Color.red },
            new ColorThresholdItem { threshold = 70, color = Color.yellow },
            new ColorThresholdItem { threshold = 100, color = Color.green }
        };
        [Foldout("[Components]"), SerializeField] private GameObject container;
        [Foldout("[Components]"), SerializeField] private Toggle togglePower, toggleWeight, toggleHeightU;
        private List<DataModelBinder_Rack> rackDataCombiners;
        #endregion

        /// <summary>
        /// 當選取庫存設備時，根據其電力、重量和高度，篩選出可放置的機櫃模型。
        /// </summary>
        public void FilterRackModels(bool isOn = true)
        {
            rackDataCombiners.ForEach(combiner =>
            {
                DCR_Asset rackAsset = combiner.RackAsset;
                UsageCaculatorOfRack rackUsage = rackAsset?.usageInfo;

                bool isSuitable = true;
                float totalRemainPercent = 0f;
                if (togglePower.isOn)
                {
                    isSuitable &= rackUsage.IsRackUCanFit_Power(selectedStockEquipment.equipmentUsageInfo.power_watt, out float remainPowerPercent);
                    totalRemainPercent += remainPowerPercent;
                }
                if (toggleWeight.isOn)
                {
                    isSuitable &= rackUsage.IsRackUCanFit_Weight(selectedStockEquipment.equipmentUsageInfo.weight_kg, out float remainWeightPercent);
                    totalRemainPercent = (totalRemainPercent + remainWeightPercent) * 0.5f; // 將重量剩餘百分比加權計算
                }
                if (toggleHeightU.isOn)
                {
                    isSuitable &= rackUsage.IsRackUCanFit_Height(selectedStockEquipment.equipmentUsageInfo.heightU, out float remainHeightPercent);
                    totalRemainPercent = (totalRemainPercent + remainHeightPercent) * 0.5f; // 將高度剩餘百分比加權計算
                }

                Debug.Log($"Rack: {rackAsset.modelInfo.modelName}, Suitable: {isSuitable}, Total Remain Percent: {totalRemainPercent}");
                SetRackModelColor(rackAsset.modelInfo.modelTarget, isSuitable, totalRemainPercent);
            });
        }

        private void SetRackModelColor(Transform modelTarget, bool isSuitable, float totalRemainPercent)
        {
            if (modelTarget == null) return;
            Renderer[] renderers = modelTarget.GetComponentsInChildren<Renderer>();
            Color colorToApply = isSuitable 
            ? ColorHelper.GetColorLerpFromThresholds(totalRemainPercent, colorThresholds) : colorThresholds[0].color;
            foreach (var renderer in renderers)
            {
                if (renderer.material.HasProperty("_Color"))
                {
                    renderer.material.color = colorToApply;
                }
            }
        }

        /// <summary>
        /// 當取消選取庫存設備時，恢復所有機櫃模型的顏色。
        /// </summary>
        private void RecoverRackModelColor()
        {
            rackDataCombiners?.ForEach(combiner =>
            {
                DCR_Asset rackAsset = combiner.RackAsset;
                UsageCaculatorOfRack rackUsage = rackAsset?.usageInfo;
            });
        }

        #region Event Listener
        /// <summary>
        /// 目前所選上架的庫存設備
        /// </summary>
        private void OnSelectedeEquipmentToDeploy(EquipmentAsset asset)
        {
            selectedStockEquipment = asset;
            togglePower.onValueChanged.AddListener(FilterRackModels);
            toggleWeight.onValueChanged.AddListener(FilterRackModels);
            toggleHeightU.onValueChanged.AddListener(FilterRackModels);
            container.SetActive(true);
            FilterRackModels();
        }
        private void OnStockEquipmentDeselected()
        {
            selectedStockEquipment = null;
            togglePower.onValueChanged.RemoveAllListeners();
            toggleWeight.onValueChanged.RemoveAllListeners();
            toggleHeightU.onValueChanged.RemoveAllListeners();
            container.SetActive(false);
            RecoverRackModelColor();
        }

        private void OnSetComponentsCompleted(List<DataModelBinder_Rack> list) => rackDataCombiners = list;
        private void OnEnable()
        {
            StockEquipmentList.OnStockEquipmentSelectedAction += OnSelectedeEquipmentToDeploy;
            StockEquipmentList.OnStockEquipmentDeselectedAction += OnStockEquipmentDeselected;
            RackDcrAssetSetter.OnRackDataCombinerGeneratedAction += OnSetComponentsCompleted;
        }
        private void OnDisable()
        {
            StockEquipmentList.OnStockEquipmentSelectedAction -= OnSelectedeEquipmentToDeploy;
            StockEquipmentList.OnStockEquipmentDeselectedAction -= OnStockEquipmentDeselected;
            RackDcrAssetSetter.OnRackDataCombinerGeneratedAction -= OnSetComponentsCompleted;
        }
        #endregion

        private void OnValidate()
        {
            if (container == null) container = transform.GetChild(0).gameObject;
        }
    }
}
