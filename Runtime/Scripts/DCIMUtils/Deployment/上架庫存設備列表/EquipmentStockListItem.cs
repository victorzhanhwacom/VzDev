using NaughtyAttributes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VzDev.DCIMUtils.DataUtils;

namespace VzDev.DCIMUtils.DeploymentUtils
{
    public class EquipmentStockListItem : MonoBehaviour
    {
        #region Fields
        [SerializeField, ReadOnly] private EquipmentAsset equipmentAsset;
        [Foldout("[Comoponents]"), SerializeField] private Toggle toggle;
        [Foldout("[Comoponents]"), SerializeField] private TextMeshProUGUI txtPropertyName, txtPropertyNumber, txtHeightU;
        #endregion

        public void SetEquipmentAsset(EquipmentAsset asset, ToggleGroup toggleGroup)
        {
            equipmentAsset = asset;
            toggle.group = toggleGroup;
            txtPropertyName.text = equipmentAsset.companyPropertyInfo.propertyName;
            txtPropertyNumber.text = equipmentAsset.companyPropertyInfo.propertyNumber;
            txtHeightU.text = equipmentAsset.equipmentUsageInfo.heightU.ToString();
        }

        public void DeselectItem() => toggle.isOn = false;

        private void OnEnable() => toggle.onValueChanged.AddListener(HandleToggleValueChanged);
        private void OnDisable() => toggle.onValueChanged.RemoveListener(HandleToggleValueChanged);

        private void HandleToggleValueChanged(bool isOn)
        {
            if (isOn) {
                EquipmentStockList.SelectedEquipmentItem(equipmentAsset);
                return;
            }

            if(toggle.group != null && toggle.group.AnyTogglesOn()) return;
            EquipmentStockList.DeselectEquipmentItem();
        }
    }
}
