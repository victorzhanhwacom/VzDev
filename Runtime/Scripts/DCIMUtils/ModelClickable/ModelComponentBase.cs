using System;
using NaughtyAttributes;
using UnityEngine;
using VzDev.DCIM.Deployment;
using VzDev.InteractiveUtils.ModelMouseEvent;

namespace VzDev.DCIMUtils
{
    /// <summary>
    /// ModelComponentBase 是一個抽象類別，實作了 IModelClick、IModelHover 以及 IModelComponent<TData> 介面，
    /// 定義了模型互動的基本功能，並利用事件將資料傳遞給外部訂閱者。它提供了設定資產資料、啟用/禁用 Collider
    /// </summary>
    public abstract class ModelComponentBase<TData> : MonoBehaviour, IModelClick, IModelHover,
                                    IModelComponent<TData> where TData : DCIMAsset
    {
        #region Fields
        [SerializeField, ReadOnly] protected TData data;
        private ComponenetColliderSetter colliderSetter;
        private bool isHaveData => data != null;

        #endregion

        private void Awake() => colliderSetter = new ComponenetColliderSetter(transform);
        public void SetColliderEnabled(bool isEnabled) => colliderSetter?.SetColliderEnabled(isEnabled);
        public void SetData(TData assetData)
        {
            data = assetData;
            if (isHaveData) data.modelInfo.SetModelTarget(transform);
        }

        #region Events
        public event Action<TData> OnModelClickedEvent;
        public event Action<TData> OnHoverEnterEvent;
        public event Action<TData> OnHoverExitEvent;

        public void OnHoverEnter(GameObject targetObject)
        {
            if (isHaveData) OnHoverEnterEvent?.Invoke(data);
        }

        public void OnHoverExit(GameObject targetObject)
        {
            if (isHaveData) OnHoverExitEvent?.Invoke(data);
        }

        public void OnModelClicked(GameObject clickedObject)
        {
            if (isHaveData) OnModelClickedEvent?.Invoke(data);
        }
        #endregion
    }

    /// <summary>
    /// 將Collider獨立出來，提供設定Collider啟用狀態的功能，允許外部控制模型的互動性。
    /// </summary>
    public class ComponenetColliderSetter
    {
        private Collider hitCollider;
        public ComponenetColliderSetter(Transform target)
        {
            if (target == null) return;
            hitCollider = target.GetComponent<Collider>();
        }

        public void SetColliderEnabled(bool isEnabled)
        {
            if (hitCollider != null)
                hitCollider.enabled = isEnabled;
        }
    }
}
