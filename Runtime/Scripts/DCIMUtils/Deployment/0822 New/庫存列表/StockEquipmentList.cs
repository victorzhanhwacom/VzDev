using System;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.UI;
using VzDev.DCIMUtils.DataUtils;
using VzDev.DebugUtils;

namespace VzDev.DCIMUtils.DeploymentUtils
{
    /// <summary>
    /// 庫存設備列表
    /// </summary>
    public class StockEquipmentList : MonoBehaviour
    {
        #region Fields
        [SerializeField] private List<EquipmentAsset> stockEquipment;
        [SerializeField, ReadOnly] private EquipmentAsset selectedStockEquipment;
        [Foldout("[Comoponents]"), SerializeField] private StockEquipmentListItem listItemPrefab;
        [Foldout("[Comoponents]"), SerializeField] private ScrollRect scrollRect;
        [Foldout("[Comoponents]"), SerializeField, Required] private ToggleGroup toggleGroup;

        #endregion

        public void SetEquipmentAssets(List<EquipmentAsset> assets)
        {
            DeselectStockEquipmentItem();
            ClearListItems();
            stockEquipment = assets;
            for (int i = 0; i < stockEquipment.Count; i++)
            {
                StockEquipmentListItem item = ObjectHelper.Instantiate(listItemPrefab, scrollRect.content);
                item.SetEquipmentAsset(stockEquipment[i]);
                item.SetToggleGroup(toggleGroup);
            }
        }

        private void ClearListItems()
        {
            foreach (Transform child in scrollRect.content)
            {
                Destroy(child.gameObject);
            }
        }

         /// <summary>
        /// 選取庫存設備 (列表)
        /// </summary>
        public static void SelectStockEquipmentItem(EquipmentAsset stockEquipment) => OnStockEquipmentSelectedAction?.Invoke(stockEquipment);
        /// <summary>
        /// 取消選取庫存設備 (列表)
        /// </summary>
        public static void DeselectStockEquipmentItem() => OnStockEquipmentDeselectedAction?.Invoke();

        #region Event Listener
        private void OnEnable()
        {
            StockEquipementHandler.OnGetStockEquipmentListAction += SetEquipmentAssets;
            OnStockEquipmentSelectedAction += OnStockEquipmentSelectedHandler;
            OnStockEquipmentDeselectedAction += OnStockEquipmentDeselectedHandler;
        }
        private void OnDisable()
        {
            StockEquipementHandler.OnGetStockEquipmentListAction -= SetEquipmentAssets;
            OnStockEquipmentSelectedAction -= OnStockEquipmentSelectedHandler;
            OnStockEquipmentDeselectedAction -= OnStockEquipmentDeselectedHandler;
        }

        private void OnStockEquipmentSelectedHandler(EquipmentAsset asset) => selectedStockEquipment = asset;
        private void OnStockEquipmentDeselectedHandler()
        {
            selectedStockEquipment = null;
            if(toggleGroup.AnyTogglesOn() == false) toggleGroup.SetAllTogglesOff(false);
        }
        #endregion

        #region Static Methods
        public static Action<EquipmentAsset> OnStockEquipmentSelectedAction;
        public static Action OnStockEquipmentDeselectedAction;
        #endregion
    }
}
