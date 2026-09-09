using System;
using NaughtyAttributes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VzDev.DCIMUtils.DataUtils;

namespace VzDev.DCIMUtils.DeploymentUtils
{
    /// <summary>
    /// 設備下架結果顯示面板
    /// </summary>
    public class EquipmentRemoveResultView : MonoBehaviour
    {
        #region Field
        [SerializeField, ReadOnly] private EquipmentAsset equipmentAsset;
        [SerializeField, ReadOnly] private DCR_Asset rackAsset;
        [Foldout("[Components]"), SerializeField] private GameObject rootView;
        [Foldout("[Components]"), SerializeField] private TextMeshProUGUI txtDeviceName, txtRackName, txtULocation;
        [Foldout("[Components]"), SerializeField] private Image imgDevicePhoto;
        #endregion

        private void Awake()
        {
            rootView.SetActive(false);
            GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
        }

        private void OnEnable() => RemoveEquipmentConfirm.onConfirmRemoveAction += OnRemoveEquipmentSuccessHandler;

        private void OnRemoveEquipmentSuccessHandler(EquipmentAsset equipmentAsset, DCR_Asset rackAsset)
        {
            this.equipmentAsset = equipmentAsset;
            this.rackAsset = rackAsset;
            txtDeviceName.SetText(equipmentAsset.deviceName);
            txtRackName.SetText(rackAsset.deviceName);
            txtULocation.SetText(equipmentAsset.uRange);
            rootView.SetActive(true);
        }

        private void OnDisable() => RemoveEquipmentConfirm.onConfirmRemoveAction -= OnRemoveEquipmentSuccessHandler;
    }
}
