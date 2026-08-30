using NaughtyAttributes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VzDev.DCIMUtils.DataUtils;

namespace VzDev.DCIMUtils.DeploymentUtils
{
    /// <summary>
    /// 顯示目前所選欲上架至目標機櫃的庫存設備資訊
    /// </summary>
    public class SelectedDeployToRackView : MonoBehaviour
    {
        #region Fields
        [SerializeField, ReadOnly] private DCR_Asset selectedRackAsset;
        [Foldout("[Comoponents]"), SerializeField] private GameObject root;
        [Foldout("[Comoponents]"), SerializeField] private Button btnCancel;
        [Foldout("[Comoponents]"), SerializeField]
        private TextMeshProUGUI
        txtPropertyName, txtPropertyNumber, remainHeightU, remainPowerWatt, remainWeightKG;
        #endregion

        private void Awake() => Clear();

        public void SetRackAsset(DCR_Asset rackAsset)
        {
            selectedRackAsset = rackAsset;
            txtPropertyName.text = selectedRackAsset.companyPropertyInfo.propertyName;
            txtPropertyNumber.text = selectedRackAsset.companyPropertyInfo.propertyNumber;
            remainHeightU.text = selectedRackAsset.usageInfo.remainHeightU.ToString();
            remainPowerWatt.text = selectedRackAsset.usageInfo.remainPowerWatt.ToString();
            remainWeightKG.text = selectedRackAsset.usageInfo.remainWeightKG.ToString();
            btnCancel.onClick.AddListener(OnCancelSelected);
            root.SetActive(true);
        }


        #region Event Listener
        private void OnEnable()
        {
            DeployToRackSelector.OnSelectRackTargetAction += SetRackAsset;
            DeployToRackSelector.OnDeselectRackTargetAction += Clear;
        }

        private void OnDisable()
        {
            DeployToRackSelector.OnSelectRackTargetAction -= SetRackAsset;
            DeployToRackSelector.OnDeselectRackTargetAction -= Clear;
        }

        private void OnCancelSelected() => DeployToRackSelector.DeselectRackTarget();

        private void Clear()
        {
            btnCancel.onClick.RemoveListener(OnCancelSelected);
            root.SetActive(false);
            selectedRackAsset = null;
            txtPropertyName.text = string.Empty;
            txtPropertyNumber.text = string.Empty;
            remainHeightU.text = string.Empty;
            remainPowerWatt.text = string.Empty;
            remainWeightKG.text = string.Empty;
        }
        #endregion
    }
}
