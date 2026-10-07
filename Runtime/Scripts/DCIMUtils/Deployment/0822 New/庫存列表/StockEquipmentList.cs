using System;
using System.Collections.Generic;
using System.Linq;
using NaughtyAttributes;
using TMPro;
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
        [SerializeField] private List<EquipmentAsset> stockEquipmentData;
        [Foldout("[Comoponents]"), SerializeField] private StockEquipmentListItem listItemPrefab;
        [Foldout("[Comoponents]"), SerializeField] private ScrollRect scrollRect;
        [Foldout("[Comoponents]"), SerializeField] private ToggleGroup toggleGroup;
        [Foldout("[Comoponents]"), SerializeField] private TMP_Dropdown dpBrand, dpSystem;

        /// <summary>
        /// 庫存設備列表項目
        /// </summary>
        private List<StockEquipmentListItem> stockEquipmentListItems = new List<StockEquipmentListItem>();

        /// <summary>
        /// 搜尋關鍵字
        /// </summary>
        private string searchKeyword = string.Empty;
        #endregion

        /// <summary>
        /// 建立庫存設備列表
        /// </summary>
        public void GenerateEquipmentAssetList(List<EquipmentAsset> assets)
        {
            dpBrand.interactable = false;
            dpSystem.interactable = false;
            DeselectStockEquipmentItem();
            ClearListItems();
            stockEquipmentData = assets;
            for (int i = 0; i < stockEquipmentData.Count; i++)
            {
                StockEquipmentListItem item = ObjectHelper.Instantiate(listItemPrefab, scrollRect.content);
                item.SetEquipmentAsset(stockEquipmentData[i]);
                item.SetToggleGroup(toggleGroup);
                stockEquipmentListItems.Add(item);
            }
            GroupingStockEquipmentData();
        }

        [Foldout("[Grouping]"), SerializeField] private List<string> manufacturerList = new List<string>();
        [Foldout("[Grouping]"), SerializeField] private List<string> systemCategoryList = new List<string>();

        /// <summary>
        /// 將庫存設備資料依品牌cobieInfo.type_manufacturer、系統cobieInfo.system_category進行群組化分類
        /// </summary>
        [Button]
        private void GroupingStockEquipmentData()
        {
            //用LINQ方式，將庫存設備資料依品牌cobieInfo.type_manufacturer、系統cobieInfo.system_category進行群組化分類，並且字串後面加上(數量)
            manufacturerList = stockEquipmentData.GroupBy(x => x.cobieInfo.type_manufacturer)
                .Select(g => $"{g.Key} ({g.Count()})").ToList();
            systemCategoryList = stockEquipmentData.GroupBy(x => x.cobieInfo.system_category)
                .Select(g => $"{g.Key} ({g.Count()})").ToList();

            //將 "所有資料" 加入到品牌與系統分類的第一個選項
            manufacturerList.Insert(0, $"所有廠牌({manufacturerList.Count})");
            systemCategoryList.Insert(0, $"所有類型({systemCategoryList.Count})");

            //將品牌與系統分類的選項，設定到下拉選單中
            dpBrand.ClearOptions();
            dpBrand.AddOptions(manufacturerList);
            dpSystem.ClearOptions();
            dpSystem.AddOptions(systemCategoryList);

            dpBrand.interactable = true;
            dpSystem.interactable = true;
        }

        #region 依據選擇的品牌、系統分類，進行篩選庫存設備列表，若關鍵字不為空的話，亦列入篩選的條件，若選擇的品牌、系統分類為"所有資料"，則不列入篩選條件，根據前述條件，將不符合條件的庫存設備列表項目隱藏起來 
        private void SearchBySelectedBrand(int index)
        {

            // 將庫存設備資料依選擇的品牌、進行系統cobieInfo.system_category的群組化分類，並且字串後面加上(數量)
            string selectedBrand = dpBrand.options[dpBrand.value].text;
            if (selectedBrand != $"所有廠牌({manufacturerList.Count})")
            {
                systemCategoryList = stockEquipmentData.Where(x => x.cobieInfo.type_manufacturer == selectedBrand.Split(' ')[0])
                    .GroupBy(x => x.cobieInfo.system_category)
                    .Select(g => $"{g.Key} ({g.Count()})").ToList();
                systemCategoryList.Insert(0, $"所有類型({systemCategoryList.Count})");
                dpSystem.ClearOptions();
                dpSystem.AddOptions(systemCategoryList);
            }
            else
            {
                systemCategoryList = stockEquipmentData.GroupBy(x => x.cobieInfo.system_category)
                    .Select(g => $"{g.Key} ({g.Count()})").ToList();
                systemCategoryList.Insert(0, $"所有類型({systemCategoryList.Count})");
                dpSystem.ClearOptions();
                dpSystem.AddOptions(systemCategoryList);
            }

            SearchByKeyword(searchKeyword);
        }

        private void SearchBySelectedBySystem(int index) => SearchByKeyword(searchKeyword);
        public void SearchByKeyword(string keyword)
        {
            // 依據選擇的品牌、系統分類，進行篩選庫存設備列表，若關鍵字不為空的話，亦列入篩選的條件，若選擇的品牌、系統分類為"所有資料"，則不列入篩選條件，根據前述條件，將不符合條件的庫存設備列表項目隱藏起來 
            searchKeyword = keyword;
            string selectedBrand = dpBrand.options[dpBrand.value].text;
            string selectedSystem = dpSystem.options[dpSystem.value].text;

            foreach (var item in stockEquipmentListItems)
            {
                bool isMatchBrand = selectedBrand == $"所有廠牌({manufacturerList.Count})" || item.EquipmentAsset.cobieInfo.type_manufacturer == selectedBrand.Split(' ')[0];
                bool isMatchSystem = selectedSystem == $"所有類型({systemCategoryList.Count})" || item.EquipmentAsset.cobieInfo.system_category == selectedSystem.Split(' ')[0];
                bool isMatchKeyword = string.IsNullOrEmpty(searchKeyword) || item.EquipmentAsset.deviceName.Contains(searchKeyword, StringComparison.OrdinalIgnoreCase) || item.EquipmentAsset.companyAssetInfo.assetNumber.Contains(searchKeyword, StringComparison.OrdinalIgnoreCase);

                if (dpBrand.value == 0) isMatchBrand = true;
                if (dpSystem.value == 0) isMatchSystem = true;

                item.gameObject.SetActive(isMatchBrand && isMatchSystem && isMatchKeyword);
            }
            scrollRect.verticalNormalizedPosition = 1f;
        }
        #endregion

        private void ClearListItems()
        {
            foreach (Transform child in scrollRect.content)
            {
                Destroy(child.gameObject);
            }
            stockEquipmentListItems.Clear();
        }

        private static StockEquipmentListItem currentStockEquipmentListItem;

        /// <summary>
        /// 點選列表上的庫存設備
        /// </summary>
        public static void SelectStockEquipmentItem(StockEquipmentListItem stockEquipmentItem)
        {
            currentStockEquipmentListItem = stockEquipmentItem;
            Debug.Log($"Selected Stock Equipment: {currentStockEquipmentListItem.EquipmentAsset.deviceName}");
            OnStockEquipmentItemSelectedAction?.Invoke(currentStockEquipmentListItem.EquipmentAsset);
        }

        /// <summary>
        /// 取消選取列表上的庫存設備
        /// </summary>
        public static void DeselectStockEquipmentItem()
        {
            currentStockEquipmentListItem?.SetToggle(false);
            currentStockEquipmentListItem = null;
            OnStockEquipmentItemDeselectedAction?.Invoke();
        }


        #region Event Listener
        private void OnEnable()
        {
            StockEquipementHandler.OnCombineStockeEquipmentAndModelAction += GenerateEquipmentAssetList;
            OnStockEquipmentItemDeselectedAction += OnStockEquipmentItemDeselectedHandler;
            dpBrand.onValueChanged.AddListener(SearchBySelectedBrand);
            dpSystem.onValueChanged.AddListener(SearchBySelectedBySystem);
        }



        private void OnDisable()
        {
            StockEquipementHandler.OnCombineStockeEquipmentAndModelAction -= GenerateEquipmentAssetList;
            OnStockEquipmentItemDeselectedAction -= OnStockEquipmentItemDeselectedHandler;
            dpBrand.onValueChanged.RemoveListener(SearchBySelectedBrand);
            dpSystem.onValueChanged.RemoveListener(SearchBySelectedBySystem);
        }
        /// <summary>
        /// For非從Toggle控制的取消選取庫存設備事件
        /// </summary>
        private void OnStockEquipmentItemDeselectedHandler()
        {
            if (toggleGroup.AnyTogglesOn() == false)
            {
                toggleGroup.SetAllTogglesOff(true);
            }
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
