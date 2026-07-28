using System;
using System.Collections.Generic;
using System.Linq;
using NaughtyAttributes;
using UnityEngine;
using VzDev.DCIM.Deployment;
using VzDev.DebugUtils;
using VzDev.UnityAPI.Extensions;

namespace VzDev.DCIMUtils.ModelInteractUtils
{
    /// <summary>
    /// 設定目標模型掛載相對應的ModelComponent，以存取相對應的資料
    /// <para>並處理各模型的互動事件，將資料透過 ModelComponentSetterHub 轉發給其他地方訂閱。</para>
    /// </summary>
    public abstract class ModelComponentSetterBase<TData, TComponent> : MonoBehaviour
        where TComponent : ModelComponentBase<TData> where TData : DCIMAsset, new()
    {
        #region Fields
        [SerializeField, Tooltip("(DEMO)是否在沒有資料的情況下也建立組件")] protected bool buildWithoutData = true;
        [SerializeField, OnValueChanged("OnModelClickEnabledChanged"), ShowIf("isHaveComponents")] protected bool modelClickEnabled = true;
        private void OnModelClickEnabledChanged() => SetModelClickEnabled(modelClickEnabled);

        [SerializeField, ReadOnly, Tooltip("紀錄已跟EventHub訂閱事件，避免在編輯器中造成事件重複訂閱")] private bool isSubscribedEvents = false;

        [Label("[目標模型巨集]"), SerializeField, ReadOnly] protected List<Transform> models = new();
        [Label("[資料巨集]"), SerializeField, ReadOnly] protected List<TData> data = new();
        [Label("[目標Component巨集]"), SerializeField, ReadOnly] protected List<TComponent> components = new();

        protected bool isHaveModels => models != null && models.Count > 0;
        protected bool isHaveData => data != null && data.Count > 0;
        protected bool isHaveComponents => components != null && components.Count > 0;
        #endregion

        #region Events, 供外部透過static方式訂閱事件
        public static event Action<TData> OnModelClickedEvent;
        public static event Action<TData> OnHoverEnterEvent;
        public static event Action<TData> OnHoverExitEvent;
        #endregion

        /// <summary>
        /// 設定目標模型巨集
        /// </summary>
        public void SetModels(List<Transform> modelList)
        {
            models = modelList;
            if (isHaveModels && (isHaveData || buildWithoutData)) SetComponents();
        }

        /// <summary>
        /// 設定資料巨集
        /// </summary>
        public void SetDatas(List<TData> assetDatas)
        {
            if (isHaveModels && (isHaveData || buildWithoutData)) SetComponents();
        }

        /// <summary>
        /// 依照目標模型巨集與資料巨集建立對應的ModelComponent，並將資料設定到Component上。
        /// </summary>
        [Button, ShowIf("isHaveModels")]
        private void SetComponents()
        {
            UnsubscribeAll();
            ClearComponents();
            components ??= new List<TComponent>();

            for (int i = 0; i < models.Count; i++)
            {
                Transform model = models[i];
                if (model == null) continue;
                //建置ModelComponent
                model.gameObject.TryAddComponent(out TComponent comp);
                components.Add(comp);
                //設定資料
                AssignDataToComponent(comp, model);
            }
            if (isActiveAndEnabled) SubscribeAll();
        }

        /// <summary>
        /// 比對資料巨集，將相對應的資料設定到Component上
        /// </summary>
        protected virtual void AssignDataToComponent(TComponent comp, Transform model)
        {
            //這段下列皆為暫時測試用，實際上依WebAPI取得的資料才能正確比對
            TData data = isHaveData ? this.data.Find(d => d.assetInfo.assetName == model.name) : default;
            
            if(data == null)
            {
                data = new TData();  
                string raw = model.name.GetStringBetweenMarks("[","]");
                data.assetInfo = new AssetInfo { assetName = raw.Split(':').LastOrDefault() }; 
            }
            /*
            待補全
            */

            data.modelInfo = new ModelInfo{ modelTarget = model,};
            comp.SetData(data); // null 也明確設定，避免殘留舊資料
        }

        /// <summary>
        /// 啟用/禁用 Component的Collider。
        /// </summary>
        public void SetModelClickEnabled(bool isEnabled)
        {
            if (components == null || components.Count == 0) return;
            modelClickEnabled = isEnabled;
            for (int i = 0; i < components.Count; i++)
            {
                components[i]?.SetColliderEnabled(modelClickEnabled && isSubscribedEvents);
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
        [Button, ShowIf("isHaveComponents")]
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
                isSubscribedEvents = false;
            }
        }


        private void OnEnable() => SubscribeAll();
        private void OnDisable() => UnsubscribeAll();

        #region 開啟/關閉ModelComponent的互動事件訂閱，避免在編輯器中造成事件重複訂閱
        [Button, HideIf("isSubscribedEvents")]
        private void SubscribeAll()
        {
            if (isSubscribedEvents) return;
            SetSubscribe(true);
        }

        [Button, ShowIf("isSubscribedEvents")]
        private void UnsubscribeAll()
        {
            if (isSubscribedEvents == false) return;
            SetSubscribe(false);
        }

        private void SetSubscribe(bool isSubscribe)
        {
            if (components != null)
            {
                for (int i = 0; i < components.Count; i++)
                {
                    TComponent modelComp = components[i];
                    if (modelComp == null) continue;
                    modelComp.SetColliderEnabled(isSubscribe && modelClickEnabled);
                    if (isSubscribe)
                    {
                        modelComp.OnModelClickedEvent += HandleModelClicked;
                        modelComp.OnHoverEnterEvent += HandleHoverEnter;
                        modelComp.OnHoverExitEvent += HandleHoverExit;
                    }
                    else
                    {
                        modelComp.OnModelClickedEvent -= HandleModelClicked;
                        modelComp.OnHoverEnterEvent -= HandleHoverEnter;
                        modelComp.OnHoverExitEvent -= HandleHoverExit;
                    }
                }
            }
            isSubscribedEvents = isSubscribe;
        }

        private void HandleModelClicked(TData asset)
        {
            OnModelClickedEvent?.Invoke(asset);
            ModelComponentSetterEventHub.RaiseClicked(asset); //集中管理事件給 ModelInteractionHub，方便其他地方訂閱
        }

        private void HandleHoverEnter(TData asset)
        {
            OnHoverEnterEvent?.Invoke(asset);
            ModelComponentSetterEventHub.RaiseHoverEnter(asset);  //集中管理事件給 ModelInteractionHub，方便其他地方訂閱
        }

        private void HandleHoverExit(TData asset)
        {
            OnHoverExitEvent?.Invoke(asset);
            ModelComponentSetterEventHub.RaiseHoverExit(asset);  //集中管理事件給 ModelInteractionHub，方便其他地方訂閱
        }
        #endregion
    }
}