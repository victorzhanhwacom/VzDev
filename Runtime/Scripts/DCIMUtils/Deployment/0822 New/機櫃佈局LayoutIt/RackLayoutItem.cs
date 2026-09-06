using NaughtyAttributes;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using VzDev.DCIMUtils.DataUtils;

namespace VzDev.DCIMUtils.DeploymentUtils
{
    public class RackLayoutItem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private bool isDeployMode;
        [SerializeField, ReadOnly] private EquipmentAsset equipmentAsset;
        [Foldout("[Components]"), SerializeField] private GameObject rootView, usageView;
        [Foldout("[Components]"), SerializeField] private RectTransform rectTransform;
        [Foldout("[Components]"), SerializeField] private TextMeshProUGUI txtDeviceName, txtHeight, txtPower, txtWeight;

        private readonly int heightPerU = 25;

        public void IsDeployMode(bool isDeployMode)
        {
            this.isDeployMode = isDeployMode;
            usageView.SetActive(!isDeployMode);
        }

        private void Awake() => usageView.SetActive(false);

        public void SetEquipmentAsset(EquipmentAsset equipmentAsset)
        {
            this.equipmentAsset = equipmentAsset;

            SetStartUIndex(equipmentAsset.startUIndex);

            rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x,
            heightPerU * equipmentAsset.equipmentUsageInfo.heightU);

            txtDeviceName.SetText(equipmentAsset.deviceName);
            txtHeight.SetText(equipmentAsset.equipmentUsageInfo.heightU.ToString() + "U");
            txtPower.SetText(equipmentAsset.equipmentUsageInfo.power_watt.ToString() + "kW");
            txtWeight.SetText(equipmentAsset.equipmentUsageInfo.weight_kg.ToString() + "KG");
        }

        public void SetStartUIndex(int startUIndex)
        {
            if (equipmentAsset == null) return;
            equipmentAsset.startUIndex = startUIndex;
            rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x,
               (equipmentAsset.startUIndex - 1) * heightPerU);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (isDeployMode) return;
            usageView.SetActive(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (isDeployMode) return;
            usageView.SetActive(false);
        }

        private void OnStockEquipmentItemDeselectedAction()
        {
            equipmentAsset = null;
            txtDeviceName.SetText(string.Empty);
            txtHeight.SetText(string.Empty);
            txtPower.SetText(string.Empty);
            txtWeight.SetText(string.Empty);
        }
    }
}
