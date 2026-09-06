using NaughtyAttributes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VzDev.DCIMUtils.DataUtils;

namespace VzDev.DCIMUtils.DeploymentUtils
{
    /// <summary>
    /// 顯示目前所選的上架庫存設備資訊
    /// </summary>
    public class SelectedStockEquipmentToDeployView : MonoBehaviour
    {
        #region Fields
        [SerializeField, ReadOnly] private EquipmentAsset equipmentAsset;
        [Foldout("[Comoponents]"), SerializeField] private GameObject rootView;
        [Foldout("[Comoponents]"), SerializeField] private Button btnCancelSelected;
        [Foldout("[Comoponents]"), SerializeField] private Toggle toggleEditoInfo;
        [Foldout("[Comoponents]"), SerializeField]
        private TextMeshProUGUI
        txtDeviceName, txtHeightU, txtPowerWatt, txtWeightKG, txtSystem;
        #endregion

        private void Awake()
        {
            rootView.SetActive(false);
            Clear();
        }

        public void SetEquipmentAsset(EquipmentAsset equipmentAsset)
        {
            this.equipmentAsset = equipmentAsset;
            txtDeviceName.text = equipmentAsset.deviceName;
            txtHeightU.text = equipmentAsset.equipmentUsageInfo.heightU.ToString();
            txtPowerWatt.text = equipmentAsset.equipmentUsageInfo.power_watt.ToString();
            txtWeightKG.text = equipmentAsset.equipmentUsageInfo.weight_kg.ToString();
            txtSystem.text = equipmentAsset.system.ToString();
            toggleEditoInfo.isOn = false;
            rootView.SetActive(true);
        }

        private void OnEnable()
        {
            StockEquipmentList.OnStockEquipmentItemSelectedAction += SetEquipmentAsset;
            StockEquipmentList.OnStockEquipmentItemDeselectedAction += Clear;
            btnCancelSelected.onClick.AddListener(OnCancelSelected);
        }

        private void OnDisable()
        {
            StockEquipmentList.OnStockEquipmentItemSelectedAction -= SetEquipmentAsset;
            StockEquipmentList.OnStockEquipmentItemDeselectedAction -= Clear;
            btnCancelSelected.onClick.RemoveListener(OnCancelSelected);
        }

        private void OnCancelSelected() => StockEquipmentList.DeselectStockEquipmentItem();

        private void Clear()
        {
            rootView.SetActive(false);
            equipmentAsset = null;
            txtDeviceName.text = string.Empty;
            txtHeightU.text = string.Empty;
            txtPowerWatt.text = string.Empty;
            txtWeightKG.text = string.Empty;
            txtSystem.text = string.Empty;
        }
    }
}
