using System;
using NaughtyAttributes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VzDev.DCIMUtils.DataUtils;
using VzDev.DebugUtils;
using VzDev.DOTweenUtils;

namespace VzDev.DCIMUtils.DeploymentUtils
{
    public class RemoveEquipmentConfirm : MonoBehaviour
    {
        #region Fields
        [Foldout("[Components]"), SerializeField] private GameObject rootView;
        [Foldout("[Components]"), SerializeField]
        private TextMeshProUGUI txtDeviceName, txtCategory, txtPropertyNumber,
        txtUsageHeightu, txtUsagePower, txtUsageWeight, txtRackName, txtURange;
        [Foldout("[Components]"), SerializeField] private Button btnConfirm, btnClose;
        [Foldout("[Components]"), SerializeField] private DOTweenFader tweenFader;

        private EquipmentAsset lastEquipmentAsset;
        private DCR_Asset rackAsset;
        #endregion

        private void Awake()
        {
            rootView.SetActive(false);
            transform.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
        }

        private void OnEnable()
        {
            EquipmentOperateToolbar.OnRemoveEquipmentAssetAction += OnRemoveEquipmentAssetHandler;
            btnConfirm.onClick.AddListener(OnClickConfirmBtn);
            btnClose.onClick.AddListener(OnClickCloseBtn);
        }

        private void OnDisable()
        {
            EquipmentOperateToolbar.OnRemoveEquipmentAssetAction -= OnRemoveEquipmentAssetHandler;
            btnConfirm.onClick.RemoveListener(OnClickConfirmBtn);
            btnClose.onClick.RemoveListener(OnClickCloseBtn);
        }

        private void OnRemoveEquipmentAssetHandler(EquipmentAsset equipmentAsset)
        {
            txtDeviceName.SetText(equipmentAsset.deviceName);
            txtCategory.SetText(equipmentAsset.category.ToString());
            txtPropertyNumber.SetText(equipmentAsset.companyPropertyInfo.propertyNumber);
            txtUsageHeightu.SetText(equipmentAsset.equipmentUsageInfo.heightU.ToString());
            txtUsagePower.SetText(equipmentAsset.equipmentUsageInfo.power_watt.ToString());
            txtUsageWeight.SetText(equipmentAsset.equipmentUsageInfo.weight_kg.ToString());

            if (equipmentAsset.modelInfo.modelTarget.parent.TryGetComponent(out DataModelBinder_Rack dataModelBinder_Rack))
            {
                rackAsset = dataModelBinder_Rack.RackAsset;
                txtRackName.SetText(rackAsset.deviceName);
            }

            txtURange.SetText(equipmentAsset.uRange);
            rootView.SetActive(true);

            lastEquipmentAsset = equipmentAsset;
        }

        private void OnClickCloseBtn()
        {
            rootView.SetActive(false);
        }

        private void OnClickConfirmBtn()
        {
            onConfirmRemoveAction?.Invoke(lastEquipmentAsset, rackAsset);
            OnClickCloseBtn();

            rackAsset.RemoveEquipmentAsset(lastEquipmentAsset);
            ObjectHelper.Destroy(lastEquipmentAsset.modelInfo.modelTarget.gameObject);
            lastEquipmentAsset = null;
        }

        public static Action<EquipmentAsset, DCR_Asset> onConfirmRemoveAction;
    }
}
