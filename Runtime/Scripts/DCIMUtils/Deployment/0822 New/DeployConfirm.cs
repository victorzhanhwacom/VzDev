using System;
using NaughtyAttributes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VzDev.DCIMUtils.DataUtils;

namespace VzDev.DCIMUtils.DeploymentUtils
{
    public class DeployConfirm : MonoBehaviour
    {
        #region Fields
        [Foldout("[Components]"), SerializeField] private GameObject rootView, confirmView;
        [Foldout("[Components]"), SerializeField] private TextMeshProUGUI txtDeployU;
        [Foldout("[Components]"), SerializeField] private Button btnConfirm, btnCancel;

        private string deployU;
        #endregion

        private void Awake()
        {
            rootView.SetActive(false);
            confirmView.SetActive(false);

        }

        private void OnEnable()
        {
            DeployEquipmentIndicator.onConfirmToDeployAction += UpdateDeployInfo;
        }

        private void OnDisable()
        {
            DeployEquipmentIndicator.onConfirmToDeployAction -= UpdateDeployInfo;
        }

        private void UpdateDeployInfo(EquipmentAsset data)
        {
            btnConfirm.onClick.AddListener(OnClickConfirmBtn);
            btnCancel.onClick.AddListener(OnClickCancelBtn);

            deployU = $"U{data.startUIndex} ~ U{data.equipmentUsageInfo.heightU + data.startUIndex - 1}";
            txtDeployU.SetText(deployU);
            rootView.SetActive(true);
        }

        private void OnClickConfirmBtn()
        {
            onConfirmDeployAction?.Invoke();
            rootView.SetActive(false);
            confirmView.SetActive(true);
        }

        private void OnClickCancelBtn()
        {
            rootView.SetActive(false);
            onCancelDeployAction?.Invoke();
        }

        public static Action onConfirmDeployAction, onCancelDeployAction;
    }
}
