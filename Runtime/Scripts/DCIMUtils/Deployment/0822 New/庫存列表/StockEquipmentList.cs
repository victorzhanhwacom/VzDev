using System;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.UI;
using VzDev.DCIMUtils.DataUtils;
using VzDev.DebugUtils;
using VzDev.InteractiveUtils.ModelMouseEvent;

namespace VzDev.DCIMUtils.DeploymentUtils
{
    /// <summary>
    /// 庫存設備列表
    /// </summary>
    public class StockEquipmentList : MonoBehaviour
    {
        #region Fields
        [SerializeField] private List<EquipmentAsset> stockEquipmentData;
        [Foldout("[Comoponents]"), SerializeField] private StockEquipmentListItem listItemPrefab;
        [Foldout("[Comoponents]"), SerializeField] private ScrollRect scrollRect;
        [Foldout("[Comoponents]"), SerializeField, Required] private ToggleGroup toggleGroup;

        #endregion

        /// <summary>
        /// 建立庫存設備列表
        /// </summary>
        public void GenerateEquipmentAssetList(List<EquipmentAsset> assets)
        {
            DeselectStockEquipmentItem();
            ClearListItems();
            stockEquipmentData = assets;
            for (int i = 0; i < stockEquipmentData.Count; i++)
            {
                StockEquipmentListItem item = ObjectHelper.Instantiate(listItemPrefab, scrollRect.content);
                item.SetEquipmentAsset(stockEquipmentData[i]);
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
        /// 點選列表上的庫存設備
        /// </summary>
        public static void SelectStockEquipmentItem(EquipmentAsset stockEquipment) => OnStockEquipmentItemSelectedAction?.Invoke(stockEquipment);

        /// <summary>
        /// 取消選取列表上的庫存設備
        /// </summary>
        public static void DeselectStockEquipmentItem() => OnStockEquipmentItemDeselectedAction?.Invoke();


        #region Event Listener
        private void OnEnable()
        {
            StockEquipementHandler.OnCombineStockeEquipmentAndModelAction += GenerateEquipmentAssetList;
            OnStockEquipmentItemDeselectedAction += OnStockEquipmentItemDeselectedHandler;
        }
        private void OnDisable()
        {
            StockEquipementHandler.OnCombineStockeEquipmentAndModelAction -= GenerateEquipmentAssetList;
            OnStockEquipmentItemDeselectedAction -= OnStockEquipmentItemDeselectedHandler;
        }
        /// <summary>
        /// For非從Toggle控制的取消選取庫存設備事件
        /// </summary>
        private void OnStockEquipmentItemDeselectedHandler()
        {
            if (toggleGroup.AnyTogglesOn() == false) toggleGroup.SetAllTogglesOff(false);
        }
        #endregion

        #region Static Methods
        /// <summary>
        /// 選取庫存設備 (列表)
        /// </summary>
        public static Action<EquipmentAsset> OnStockEquipmentItemSelectedAction;
        /// <summary>
        /// 取消選取庫存設備 (列表)
        /// </summary>
        public static Action OnStockEquipmentItemDeselectedAction;

        #endregion
    }
}
