using System;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;
using VzDev.DCIM.Deployment;
using VzDev.DebugUtils;
using VzDev.UnityAPI.Extensions;

namespace VzDev.DCIMUtils
{
    /// <summary>
    /// 定義模型互動的介面，提供設定DCIM資產資料、啟用/禁用 Collider，以及提供外部訂閱互動事件的功能。
    /// </summary>
    public interface IModelComponent<TData> where TData : DCIMAssetBase
    {
        void SetData(TData assetData);
        void SetColliderEnabled(bool isEnabled);
        event Action<TData> OnModelClickedEvent;
        event Action<TData> OnHoverEnterEvent;
        event Action<TData> OnHoverExitEvent;
    }

    /// <summary>
    /// ModelComponentSetterBase 是一個抽象類別，提供了管理模型與資產資料的關聯、設定互動性以及訂閱互動事件的功能。
    /// </summary>
    public abstract class ModelComponentSetterBase<TData, TComponent> : MonoBehaviour
        where TComponent : MonoBehaviour, IModelComponent<TData>
        where TData : DCIMAssetBase
    {
        #region Fields
        [SerializeField, Tooltip("(DEMO)是否在沒有資料的情況下也建立組件")] protected bool buildWithoutData = true;
        [SerializeField, ReadOnly] protected List<Transform> models = new();
        [SerializeField, ReadOnly] protected List<TData> data = new();
        protected List<TComponent> components = new();
        private bool isSubscribedEvents = false;

        protected bool isHaveModels => models != null && models.Count > 0;
        protected bool isHaveData => data != null && data.Count > 0;
        #endregion

        #region Events, 供外部透過static方式訂閱事件
        public static event Action<TData> OnModelClickedEvent;
        public static event Action<TData> OnHoverEnterEvent;
        public static event Action<TData> OnHoverExitEvent;
        #endregion

        [Button, ShowIf("isHaveModels")] private void InteractiveOn() => SetInteractable(true);
        [Button, ShowIf("isHaveModels")] private void InteractiveOff() => SetInteractable(false);

        /// <summary>
        /// Component 的互動性，啟用或禁用 Collider。
        /// </summary>
        public void SetInteractable(bool isEnabled)
        {
            for (int i = 0; i < components.Count; i++)
            {
                components[i]?.SetColliderEnabled(isEnabled);
            }
        }

        [Button, ShowIf("isHaveModels")]
        public void Clear()
        {
            UnsubscribeAll();
            models = new List<Transform>();
            data = new List<TData>();
            ClearComponents();
        }
        private void ClearComponents()
        {
            if (components != null)
            {
                for (int i = 0; i < components.Count; i++)
                {
                    if (components[i] != null)
                    {
                        ObjectHelper.Destroy(components[i]);
                    }
                }
                components.Clear();
            }
        }

        public void SetDatas(List<TData> assetDatas)
        {
            data = assetDatas;
            if (isHaveModels && (isHaveData || buildWithoutData)) SetComponents();
        }

        public void SetModels(List<Transform> modelList)
        {
            models = modelList;
            if (isHaveModels && (isHaveData || buildWithoutData)) SetComponents();
        }

        [Button, ShowIf("isHaveModels")]
        private void SetComponents()
        {
            UnsubscribeAll();
            components ??= new List<TComponent>();
            ClearComponents();

            for (int i = 0; i < models.Count; i++)
            {
                Transform model = models[i];
                if (model == null) continue;
                model.gameObject.TryAddComponent(out TComponent comp);
                components.Add(comp);
                AssignData(comp, model);
            }
            if (isActiveAndEnabled) SubscribeAll();
        }

        /// <summary>
        /// 將對應的資產資料設定到元件上。比對方式目前用名稱，
        /// 之後若比對邏輯改變（例如改用 ID、Dictionary 查表等），只需修改此函式。
        /// </summary>
        protected virtual void AssignData(TComponent comp, Transform model)
        {
            var asset = isHaveData ? data.Find(d => d.assetInfo.assetName == model.name) : default;
            comp.SetData(asset); // null 也明確設定，避免殘留舊資料
        }

        private void OnEnable() => SubscribeAll();
        private void OnDisable() => UnsubscribeAll();

        private void SubscribeAll()
        {
            if (isSubscribedEvents) return;
            if (components == null || components.Count == 0) return;
            for (int i = 0; i < components.Count; i++)
            {
                TComponent c = components[i];
                if (c == null) continue;
                c.OnModelClickedEvent += HandleModelClicked;
                c.OnHoverEnterEvent += HandleHoverEnter;
                c.OnHoverExitEvent += HandleHoverExit;
            }
            isSubscribedEvents = true;
        }

        private void UnsubscribeAll()
        {
            if (components != null)
            {
                for (int i = 0; i < components.Count; i++)
                {
                    TComponent c = components[i];
                    if (c == null) continue;
                    c.OnModelClickedEvent -= HandleModelClicked;
                    c.OnHoverEnterEvent -= HandleHoverEnter;
                    c.OnHoverExitEvent -= HandleHoverExit;
                }
            }
            isSubscribedEvents = false;
        }

        private void HandleModelClicked(TData asset) => OnModelClickedEvent?.Invoke(asset);
        private void HandleHoverEnter(TData asset) => OnHoverEnterEvent?.Invoke(asset);
        private void HandleHoverExit(TData asset) => OnHoverExitEvent?.Invoke(asset);
    }
}