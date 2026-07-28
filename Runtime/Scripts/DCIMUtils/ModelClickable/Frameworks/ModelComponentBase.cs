using System;
using NaughtyAttributes;
using UnityEngine;
using VzDev.DCIM.Deployment;
using VzDev.InteractiveUtils.ModelMouseEvent;
using VzDev.UnityAPI.Extensions;

namespace VzDev.DCIMUtils.ModelInteractUtils
{
    /// <summary>
    /// 掛載資料項到目標模型上，並處理各模型的互動事件，將資料透過 ModelComponentSetterHub 轉發給其他地方訂閱。
    /// </summary>
    public abstract class ModelComponentBase<TData> : MonoBehaviour
        , IModelClick, IModelHover, IModelComponent<TData> where TData : DCIMAsset
    {
        #region Fields
        [SerializeField, ReadOnly, Tooltip("目標模型攜帶的資料項")] protected TData data;
        /// <summary>
        /// 將Collider獨立出來，提供設定Collider啟用狀態的功能，允許外部控制模型的互動性。
        /// </summary>
        private ModelColliderSetter modelColliderSetter;
        private bool isHaveData => data != null;

        #endregion

        public void SetColliderEnabled(bool isEnabled)
        {
            modelColliderSetter ??= new ModelColliderSetter(transform);
            modelColliderSetter.SetColliderEnabled(isEnabled);
        }

        public void SetData(TData assetData)
        {
            data = assetData;
            if (isHaveData) data.modelInfo.SetModelTarget(transform);
        }

        #region Events
        public event Action<TData> OnModelClickedEvent;
        public event Action<TData> OnHoverEnterEvent;
        public event Action<TData> OnHoverExitEvent;

        public void OnHoverEnter(GameObject targetObject) => OnHoverEnterEvent?.Invoke(data);
        public void OnHoverExit(GameObject targetObject) => OnHoverExitEvent?.Invoke(data);
        public void OnModelClicked(GameObject clickedObject) => OnModelClickedEvent?.Invoke(data);
        #endregion
    }

    /// <summary>
    /// 將Collider獨立出來，提供設定Collider啟用狀態的功能，允許外部控制模型的互動性。
    /// </summary>
    public class ModelColliderSetter
    {
        private Transform model;
        private BoxCollider hitCollider;
        public ModelColliderSetter(Transform target)
        {
            if (target == null) return;
            model = target;
            model.TryAddComponent(out hitCollider);
        }

        public void SetColliderEnabled(bool isEnabled)
        {
            if (hitCollider != null)
                hitCollider.enabled = isEnabled;
        }
    }

    
    /// <summary>
    /// 定義目標模型的互動介面
    /// </summary>
    public interface IModelComponent<TData> where TData : DCIMAsset
    {
        /// <summary>
        /// 設定資料項
        /// </summary>
        void SetData(TData assetData);

        /// <summary>
        /// 設定Collider的啟用狀態，允許外部控制模型的互動性。
        /// </summary>
        void SetColliderEnabled(bool isEnabled);
        event Action<TData> OnModelClickedEvent;
        event Action<TData> OnHoverEnterEvent;
        event Action<TData> OnHoverExitEvent;
    }
}
