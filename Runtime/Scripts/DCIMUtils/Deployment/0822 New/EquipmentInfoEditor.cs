using NaughtyAttributes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VzDev.DCIMUtils.DataUtils;
using VzDev.DCIMUtils.DeploymentUtils;

namespace VzDev.DCIMUtils
{
    public class EquipmentInfoEditor : MonoBehaviour
    {
        #region Fields
        [SerializeField, ReadOnly] private EquipmentAsset equipmentAsset;
        [Foldout("[Comoponents]"), SerializeField] private GameObject rootView;
        [Foldout("[Comoponents]"), SerializeField]
        private TMP_InputField inputDeviceName;
        #endregion


        public void SetEquipmentAsset(EquipmentAsset equipmentAsset)
        {
            this.equipmentAsset = equipmentAsset;
            inputDeviceName.text = equipmentAsset.deviceName;
            rootView.SetActive(true);
        }

        private void Clear()
        {
            rootView.SetActive(false);
            equipmentAsset = null;
            inputDeviceName.text = string.Empty;
        }

        private void OnEnable()
        {
            StockEquipmentList.OnStockEquipmentItemSelectedAction += SetEquipmentAsset;
            StockEquipmentList.OnStockEquipmentItemDeselectedAction += Clear;
        }


        private void OnDisable()
        {
            StockEquipmentList.OnStockEquipmentItemSelectedAction -= SetEquipmentAsset;
            StockEquipmentList.OnStockEquipmentItemDeselectedAction -= Clear;
        }
    }
}
