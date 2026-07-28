using System;
using NaughtyAttributes;
using UnityEngine;
using VzDev.DCIM.Deployment;

namespace VzDev.DCIMUtils.ModelInteractUtils
{
    /// <summary>
    /// 資料項UI顯示的基底類別，會自動註冊到 InfoPanelRegistry
    /// <para> 透過 IModelSelectedHandler 介面，當模型被選取時，會自動呼叫 OnModelSelected 方法，並傳入對應的 DCIMAsset 資料</para>
    /// <para> 當模型被取消選取時，會自動呼叫 OnModelDeselected 方法</para>
    /// </summary>
    public abstract class AssetDataDisplayBase<TData> : MonoBehaviour, IModelSelectedHandler
        where TData : DCIMAsset
    {
        #region Fields
        [SerializeField, ReadOnly] protected TData data;

        /// <summary>
        /// 資料項的型別，供 AssetDataDisplayRegistry 註冊字典使用
        /// </summary>
        public Type DataType => typeof(TData);
        #endregion

        /// <summary>
        /// 當模型被選取時，會自動呼叫此方法，並傳入對應的 DCIMAsset 資料
        /// </summary>
        public void OnModelSelected(DCIMAsset asset)
        {
            data = asset as TData;
            UpdateUIOnSelected();
        }
        /// <summary>
        /// 當模型被取消選取時，會自動呼叫此方法
        /// </summary>
        public void OnModelDeselected() => UpdateUIOnDeselected();

        /// <summary>
        /// 當模型被選取時，更新UI顯示
        /// </summary>
        protected abstract void UpdateUIOnSelected();
        /// <summary>
        /// 當模型被取消選取時，更新UI顯示
        /// </summary>
        protected abstract void UpdateUIOnDeselected();

        #region 在顯示/隱藏時，註冊與否此組件給 AssetDataDisplayRegistry
        private void OnEnable() => AssetDataDisplayRegistry.Register(this);
        private void OnDisable() => AssetDataDisplayRegistry.Unregister(this);
        #endregion
    }

    public interface IModelSelectedHandler
    {
        Type DataType { get; }
        /// <summary>
        /// 模型被選取時
        /// </summary>
        void OnModelSelected(DCIMAsset asset);
        /// <summary>
        /// 模型被取消選取時
        /// </summary>
        void OnModelDeselected();
    }
}
