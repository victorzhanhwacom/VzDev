using System;
using NaughtyAttributes;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using VzDev.DCIMUtils.DataUtils;
using VzDev.DOTweenUtils;

namespace VzDev.DCIMUtils.DeploymentUtils
{
    public class DeployConfirm : MonoBehaviour
    {
        #region Fields
        [Foldout("[Components]"), SerializeField] private GameObject rootView;
        [Foldout("[Components]"), SerializeField] private TextMeshProUGUI txtRackName, txtURange;
        [Foldout("[Components]"), SerializeField] private Button btnConfirm, btnCancel, btnClose;
        [Foldout("[Components]"), SerializeField] private DOTweenFader tweenFader;
        #endregion

        public UnityEvent<Transform> invokeEquipmentModelEvent;

        private void Awake() => rootView.SetActive(false);

        private void OnEnable() => DeployEquipmentIndicator.onConfirmToDeployAction += OnConfirmToDeployHandler;

        private void OnDisable() => DeployEquipmentIndicator.onConfirmToDeployAction -= OnConfirmToDeployHandler;

        private void OnConfirmToDeployHandler(EquipmentAsset equipmentAsset, DCR_Asset rackAsset, Transform previewInstance)
        {
            btnConfirm.onClick.AddListener(OnClickConfirmBtn);
            btnCancel.onClick.AddListener(OnClickCancelBtn);
            txtRackName.SetText(rackAsset.deviceName);
            txtURange.SetText(equipmentAsset.uRange);
            invokeEquipmentModelEvent?.Invoke(previewInstance);
            tweenFader.Show();
            // rootView.SetActive(true);
        }

        private void OnClickConfirmBtn()
        {
            onConfirmDeployAction?.Invoke();
            rootView.SetActive(false);
        }

        private void OnClickCancelBtn()
        {
            tweenFader.Hide();
            onCancelDeployAction?.Invoke();
        }

        public static Action onConfirmDeployAction, onCancelDeployAction;
    }
}
