using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace VzDev.DCIMUtils.DeploymentUtils
{
    public class DeployConfirm : MonoBehaviour
    {
        public TextMeshProUGUI txtRack, txtUIndex, txtEquipmentPropertyName, txtEquipmentPropertyNumber;

        public Button buttonConfirm, buttonCancel;

        public GameObject container;

        private void Awake()
        {
            container.SetActive(false);
            buttonConfirm.onClick.AddListener(() =>
            {
                container.SetActive(false);
                onConfirmDeploy?.Invoke();
                Debug.Log("Deploy equipment confirmed.");
            });

            buttonCancel.onClick.AddListener(() =>
            {
                container.SetActive(false);
                onCancelDeploy?.Invoke();
                Debug.Log("Deploy equipment canceled.");
            });
        }

        private void OnEnable() => DeployEquipmentIndicator.onSelectedeEquipmentToDeploy += UpdateDeployInfo;
        private void OnDisable() => DeployEquipmentIndicator.onSelectedeEquipmentToDeploy -= UpdateDeployInfo;

        private void UpdateDeployInfo(EquipmentDeployInfo info)
        {
            container.SetActive(true);
            txtRack.text = info.rackAsset != null ? info.rackAsset.deviceName : info.rackAsset.companyPropertyInfo.propertyName;
            txtUIndex.text = info.uIndex.ToString();
            txtEquipmentPropertyName.text = info.equipmentAsset != null ? info.equipmentAsset.deviceName : info.equipmentAsset.companyPropertyInfo.propertyName;
            txtEquipmentPropertyNumber.text = info.equipmentAsset != null ? info.equipmentAsset.companyPropertyInfo.propertyNumber : "{null}";
        }

        public static Action onConfirmDeploy, onCancelDeploy;
    }
}
