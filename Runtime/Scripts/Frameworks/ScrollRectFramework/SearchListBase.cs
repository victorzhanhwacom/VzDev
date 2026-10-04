using System.Collections.Generic;
using System.Linq;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using VzDev.ApiExtensions;

namespace VzDev.Frameworks.ScrollRectUtils
{
    /// <summary>
    /// 框架：搜索列表
    /// </summary>
    public abstract class SearchListBase<TData> : MonoBehaviour where TData : IDataKeyID
    {
        #region Event
        public static UnityEvent<ScrollRectListItemBase<TData>> onSelectedItemEvent_Static;
        public static UnityEvent onSelectEmptyEvent_Static;
        [Foldout("[Event]")] public UnityEvent<string> listItemTotalCountEvent;
        [Foldout("[Event]")] public UnityEvent<ScrollRectListItemBase<TData>> onSelectedItemEvent;
        [Foldout("[Event]")] public UnityEvent onSelectEmptyEvent;
        #endregion

        #region Field
        [SerializeField, ReadOnly] protected List<TData> dataList;
        [Foldout("[Prefabs]"), SerializeField] protected ScrollRectListItemBase<TData> listItemPrefab;
        [Foldout("[Components]"), SerializeField] protected ScrollRect scrollRect;
        [Foldout("[Components]"), SerializeField] protected ToggleGroup toggleGroup;

        /// <summary>
        /// 資料對應的列表項目字典，用於快速查找和管理列表項目
        /// </summary>
        protected Dictionary<string, ScrollRectListItemBase<TData>> dataToItemMap = new Dictionary<string, ScrollRectListItemBase<TData>>();
        #endregion

        /// <summary>
        /// 設置資料集，並建立Prefab列表
        /// </summary>
        public virtual void SetDataList(List<TData> dataList)
        {
            this.dataList = dataList;
            // 判斷該TData是否已經存在對應的列表項目，若不存在則建立新的列表項目, 若存在則設置值
            foreach (var data in dataList)
            {
                if (!dataToItemMap.ContainsKey(data.dataKeyID))
                {
                    var item = Instantiate(listItemPrefab, scrollRect.content);
                    item.SetData(data);
                    item.SetToggleGroup(toggleGroup);
                    dataToItemMap[data.dataKeyID] = item;
                }
                else dataToItemMap[data.dataKeyID].SetData(data);
            }
            // 移除不在新的資料集中的列表項目
            var keysToRemove = new List<string>();
            List<string> dataIDs = dataList.Select(data => data.dataKeyID).ToList();
            foreach (string key in dataToItemMap.Keys)
            {
                if (!dataIDs.Contains(key))
                {
                    Destroy(dataToItemMap[key].gameObject);
                    keysToRemove.Add(key);
                }
            }
            foreach (var key in keysToRemove) dataToItemMap.Remove(key);

            listItemTotalCountEvent?.Invoke($"共{dataList.Count}筆資料");
        }

        /// <summary>
        /// 清空列表 
        /// </summary>
        public void ClearList()
        {
            dataToItemMap.Clear();
            scrollRect.content.RemoveAllChildren();
            scrollRect.verticalNormalizedPosition = 1;
        }

        /// <summary>
        /// 發送事件：選中的列表項目
        /// </summary>
        public void SetSelectedListItem(ScrollRectListItemBase<TData> selectedItem)
        {
            if (selectedItem == null) return;
            onSelectedItemEvent_Static?.Invoke(selectedItem);
            onSelectedItemEvent?.Invoke(selectedItem);
            OnSelectedItem(selectedItem);
        }

        /// <summary>
        /// 檢查無任何列表項目被選中時，觸發此事件
        /// </summary>
        public void CheckSelectEmpty()
        {
            if (toggleGroup != null && toggleGroup.AnyTogglesOn() == false)
            {
                onSelectEmptyEvent_Static?.Invoke();
                onSelectEmptyEvent?.Invoke();
                OnSelectEmpty();
            }
        }
        protected virtual void OnSelectedItem(ScrollRectListItemBase<TData> selectedItem) { }
        protected virtual void OnSelectEmpty() { }


        protected virtual void OnEnable()
        {
            toggleGroup?.SetAllTogglesOff();
            scrollRect.verticalNormalizedPosition = 1;
        }
    }
}
