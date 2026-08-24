using System;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.UI;
using VzDev.DCIMUtils.DataUtils;
using VzDev.DebugUtils;

namespace VzDev.DCIMUtils.DeploymentUtils
{
    public class EquipmentStockList : MonoBehaviour
    {
        #region Fields
        [SerializeField, ReadOnly] private List<EquipmentAsset> equipmentAssets;
        [Foldout("[Comoponents]"), SerializeField] private ScrollRect scrollRect;
        [Foldout("[Comoponents]"), SerializeField, Required] private ToggleGroup toggleGroup;
        [Foldout("[Comoponents]"), SerializeField] private EquipmentStockListItem listItemPrefab;
        
        #endregion

        public void SetEquipmentAssets(List<EquipmentAsset> assets)
        {
            DeselectEquipmentItem();
            ClearListItems();
            equipmentAssets = assets;
            for (int i = 0; i < equipmentAssets.Count; i++)
            {
                EquipmentStockListItem item = ObjectHelper.Instantiate(listItemPrefab, scrollRect.content);
                item.SetEquipmentAsset(equipmentAssets[i], toggleGroup);
            }
        }

        private void ClearListItems()
        {
            foreach (Transform child in scrollRect.content)
            {
                Destroy(child.gameObject);
            }
        }

        private void OnEquipmentDeselectedHandler() => toggleGroup.SetAllTogglesOff(false);

        private void OnEnable()
        {
            EquipmentAssetInStockHandler.OnCreateEquipmentAssetsInStockEvent += SetEquipmentAssets;
            OnEquipmentDeselected += OnEquipmentDeselectedHandler;
        }
        private void OnDisable()
        {
            EquipmentAssetInStockHandler.OnCreateEquipmentAssetsInStockEvent -= SetEquipmentAssets;
            OnEquipmentDeselected -= OnEquipmentDeselectedHandler;
        }

        private void OnGetDeployEquipmentAssetsFaield(string msg) => Debug.Log($"OnGetDeployEquipmentAssetsFaield: {msg}");

        #region Static Methods
        public static void SelectedEquipmentItem(EquipmentAsset equipmentAsset) => OnEquipmentSelected?.Invoke(equipmentAsset);
        public static void DeselectEquipmentItem() => OnEquipmentDeselected?.Invoke();
        public static Action<EquipmentAsset> OnEquipmentSelected;
        public static Action OnEquipmentDeselected;
        #endregion
    }
}
