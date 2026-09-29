using NaughtyAttributes;
using UnityEngine;
using UnityEngine.UI;

namespace VzDev.Frameworks.ScrollRectUtils
{
    /// <summary>
    /// 框架：ScrollRect列表 - 列表項目基底
    /// </summary>
    public abstract class ScrollRectListItemBase<TData> : MonoBehaviour
    {
        #region Fields
        [SerializeField, ReadOnly] protected TData data;
        [Foldout("[Components]"), SerializeField] protected Toggle toggle;
        [Foldout("[Components]"), SerializeField] protected ScrollRectListBase<TData> scrollRectList;
        public TData Data => data;
        #endregion

        #region 設置項目
        public void SetScrollRectList(ScrollRectListBase<TData> scrollRectList) => this.scrollRectList = scrollRectList;
        public void SetToggleGroup(ToggleGroup toggleGroup) => toggle.group = toggleGroup;
        public void SetData(TData data)
        {
            this.data = data;
            UpdateUI(data);
        }
        #endregion

        /// <summary>
        /// 更新UI細節處理，需在子類別實作
        /// </summary>
        protected abstract void UpdateUI(TData data);

        #region Event Listener
        protected virtual void OnEnable() => toggle?.onValueChanged.AddListener(OnToggleValueChanged);
        protected virtual void OnDisable() => toggle?.onValueChanged.RemoveListener(OnToggleValueChanged);
        protected virtual void OnToggleValueChanged(bool isOn)
        {
            if (scrollRectList != null)
            {
                if (isOn) scrollRectList.SetSelectedListItem(this);
                else scrollRectList.CheckSelectEmpty();
            }
        }
        #endregion
    }
}
