using System;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;
using VzDev.DCIM.RevitAssetDataStructure;
using VzDev.DebugUtils;
using VzDev.InteractiveUtils.ModelMouseEvent;
using VzDev.UnityAPI.Extensions;

namespace VzDev.DCIMUtils.ModelInteractUtils
{
    /// <summary>
    /// 設定目標模型掛載相對應的ModelComponent，以存取相對應的資料
    /// <para>並處理各模型的互動事件。</para>
    /// </summary>
    public abstract class ModelComponentSetterBase<TData, TComponent> : MonoBehaviour
        where TComponent : ModelComponentBase<TData> where TData : DCIMAsset, new()
    {
        #region Fields
        [SerializeField, OnValueChanged("OnModelClickEnabledChanged"), ShowIf("isHaveComponents")] protected bool modelClickEnabled = true;
        private void OnModelClickEnabledChanged() => SetModelClickEnabled(modelClickEnabled);

        [SerializeField, ReadOnly, Tooltip("紀錄已跟EventHub訂閱事件，避免在編輯器中造成事件重複訂閱")] private bool isSubscribedEvents = false;

        [SerializeField,] protected List<Transform> models = new();
        [SerializeField, ReadOnly] protected List<TData> dcimAssetDatas = new();
        [SerializeField, ReadOnly] protected List<TComponent> modelComponents = new();
        [Foldout("[Events]"), SerializeField, HideIf("isPlaying")] protected UnityEvent<List<TComponent>> OnSetComponentsCompletedEvent;

        protected bool isHaveModels => models != null && models.Count > 0;
        protected bool isHaveData => dcimAssetDatas != null && dcimAssetDatas.Count > 0;
        protected bool isHaveComponents => modelComponents != null && modelComponents.Count > 0;
        protected bool isPlaying => Application.isPlaying;
        #endregion

        #region Events, 供外部透過static方式訂閱事件
        public static event Action<TData> OnModelClickedAction;
        public static event Action<TData> OnHoverEnterAction;
        public static event Action<TData> OnHoverExitAction;

        private int dataGetCount = 0, dataGetCountMax = 2; //計數器，判斷資料是否都已經準備好
        #endregion

        protected virtual void Awake()
        {
            if (isPlaying)
            {
                OnSetComponentsCompletedEvent.RemoveAllListeners();
            }
        }


        /// <summary>
        /// 設定目標模型巨集
        /// </summary>
        public void SetModels(List<Transform> modelList)
        {
            models = modelList;
            SetComponents();
        }

        /// <summary>
        /// 設定資料巨集
        /// </summary>
        public void SetDatas(List<TData> assetDatas)
        {
            dcimAssetDatas = assetDatas;
            SetComponents();
        }

        /// <summary>
        /// 依照目標模型巨集與資料巨集建立對應的ModelComponent，並將資料設定到Component上。
        /// </summary>
        [Button, ShowIf("isHaveModels")]
        private void SetComponents()
        {
            if (++dataGetCount < dataGetCountMax) return; 

            UnsubscribeAll();
            ClearComponents();
            modelComponents ??= new List<TComponent>();

            for (int i = 0; i < models.Count; i++)
            {
                Transform model = models[i];
                if (model == null) continue;
                //建置ModelComponent
                model.gameObject.TryAddComponent(out TComponent comp);
                modelComponents.Add(comp);

                //比對資料巨集，將相對應的資料設定到Component上
                TData data = isHaveData ? dcimAssetDatas.Find(d => model.name.ContainKeyword(StringComparison.OrdinalIgnoreCase, d.deviceCode)) : default;
                if (data == null)
                {
                    Debug.LogWarning($"[{nameof(ModelComponentSetterBase<TData, TComponent>)}] 找不到對應的資料，請確認模型名稱是否包含資料的deviceCode，模型名稱：{model.name}");
                    continue;
                }
                data.modelInfo.modelTarget = model; //將對應的模型設定到資料的modelInfo中
                comp.SetData(data); 
            }
            SetModelClickEnabled(modelClickEnabled);
            OnSetComponentsCompletedAction?.Invoke(modelComponents);
            OnSetComponentsCompletedEvent?.Invoke(modelComponents);
            if (isActiveAndEnabled) SubscribeAll();
            
        }

        /// <summary>
        /// 啟用/禁用 Component的Collider。
        /// </summary>
        public void SetModelClickEnabled(bool isEnabled)
        {
            if (modelComponents == null || modelComponents.Count == 0) return;
            modelClickEnabled = isEnabled;
            for (int i = 0; i < modelComponents.Count; i++)
            {
                modelComponents[i]?.SetColliderEnabled(modelClickEnabled && isSubscribedEvents);

            }
            if (!isEnabled)
            {
                var renderers = new List<Renderer>(modelComponents.Count);
                foreach (var comp in modelComponents) //根據建立的Component，找出對應的Renderer，並從SelectionController中移除
                {
                    if (comp != null && comp.TryGetComponent<Renderer>(out var r))
                        renderers.Add(r);
                }
                SelectionController.RemoveFromSelection(renderers);
            }
        }

        [Button, ShowIf("isHaveComponents")]
        private void ClearComponents()
        {
            if (modelComponents != null)
            {
                for (int i = 0; i < modelComponents.Count; i++)
                {
                    if (modelComponents[i] != null)
                    {
                        if(!Application.isPlaying) modelComponents[i].OnDestroy();
                        ObjectHelper.Destroy(modelComponents[i]);
                    }
                }
                modelComponents.Clear();
                models.Clear();
                dcimAssetDatas.Clear();
                isSubscribedEvents = false;
            }
            dataGetCount = 0; //清除Component後，重置計數器，等待資料重新準備好
        }


        protected virtual void OnEnable() => SubscribeAll();
        protected virtual void OnDisable() => UnsubscribeAll();

        #region 開啟/關閉ModelComponent的互動事件訂閱，避免在編輯器中造成事件重複訂閱
        private void SubscribeAll()
        {
            if (isSubscribedEvents) return;
            SetSubscribe(true);
        }

        private void UnsubscribeAll()
        {
            if (isSubscribedEvents == false) return;
            SetSubscribe(false);
        }

        private void SetSubscribe(bool isSubscribe)
        {
            if (modelComponents != null)
            {
                for (int i = 0; i < modelComponents.Count; i++)
                {
                    TComponent modelComp = modelComponents[i];
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

        private void HandleModelClicked(TData asset) => OnModelClickedAction?.Invoke(asset);

        private void HandleHoverEnter(TData asset) => OnHoverEnterAction?.Invoke(asset);

        private void HandleHoverExit(TData asset) => OnHoverExitAction?.Invoke(asset);
        #endregion

        public static Action<List<TComponent>> OnSetComponentsCompletedAction;
    }
}