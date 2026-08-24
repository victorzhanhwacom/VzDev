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
    public class SelectedDeployEquipmentView : MonoBehaviour
    {
        #region Fields
        [SerializeField, ReadOnly] private EquipmentAsset equipmentAsset;
        [Foldout("[Comoponents]"), SerializeField] private GameObject root;
        [Foldout("[Comoponents]"), SerializeField] private Button btnCancel;
        [Foldout("[Comoponents]"), SerializeField]
        private TextMeshProUGUI
        txtPropertyName, txtPropertyNumber, txtHeightU, txtPowerWatt, txtWeightKG, txtCategory;
        #endregion

        private void Awake() => Clear();

        public void SetEquipmentAsset(EquipmentAsset equipmentAsset)
        {
            this.equipmentAsset = equipmentAsset;
            txtPropertyName.text = equipmentAsset.companyPropertyInfo.propertyName;
            txtPropertyNumber.text = equipmentAsset.companyPropertyInfo.propertyNumber;
            txtHeightU.text = equipmentAsset.equipmentUsageInfo.heightU.ToString();
            txtPowerWatt.text = equipmentAsset.equipmentUsageInfo.power_watt.ToString();
            txtWeightKG.text = equipmentAsset.equipmentUsageInfo.weight_kg.ToString();
            txtCategory.text = equipmentAsset.category.ToString();
            root.SetActive(true);
        }

        private void OnEnable()
        {
            EquipmentStockList.OnEquipmentSelected += SetEquipmentAsset;
            EquipmentStockList.OnEquipmentDeselected += Clear;
            btnCancel.onClick.AddListener(OnCancelSelected);
        }

        private void OnDisable()
        {
            EquipmentStockList.OnEquipmentSelected -= SetEquipmentAsset;
            EquipmentStockList.OnEquipmentDeselected -= Clear;
            btnCancel.onClick.RemoveListener(OnCancelSelected);
        }

        private void OnCancelSelected() => EquipmentStockList.DeselectEquipmentItem();

        private void Clear()
        {
            root.SetActive(false);
            equipmentAsset = null;
            txtPropertyName.text = string.Empty;
            txtPropertyNumber.text = string.Empty;
            txtHeightU.text = string.Empty;
            txtPowerWatt.text = string.Empty;
            txtWeightKG.text = string.Empty;
            txtCategory.text = string.Empty;
        }
    }
}
