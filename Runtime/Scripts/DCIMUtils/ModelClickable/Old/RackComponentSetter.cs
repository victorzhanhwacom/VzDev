using System;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;
using VzDev.DCIM.Deployment;
using VzDev.UnityAPI.Extensions;

namespace VzDev.DCIMUtils
{
    /// <summary>
    /// 設定機櫃模型與資產資料的組件，並將事件傳遞給外部訂閱者。
    /// </summary>
    public class RackComponentSetter : MonoBehaviour
    {
        #region Fields
        [SerializeField] private bool buildWithoutData = true;
        [SerializeField, ReadOnly] private List<Transform> models;
        [SerializeField, ReadOnly] private List<DCR_Asset> data;
        private bool isHaveModels => models != null && models.Count > 0;
        private bool isHaveData => data != null && data.Count > 0;
        private List<RackComponent> components;
        private bool isSubscribedEvents = false;

        #endregion  

        #region Events, 供外部透過static方式訂閱事件
        public static event Action<DCR_Asset> OnModelClickedEvent;
        public static event Action<DCR_Asset> OnHoverEnterEvent;
        public static event Action<DCR_Asset> OnHoverExitEvent;
        #endregion

        [Button, ShowIf("isHaveModels")]
        private void InteractiveOn() => SetInteractable(true);
        [Button, ShowIf("isHaveModels")]
        private void InteractiveOff() => SetInteractable(false);

        /// <summary>
        /// 設定所有 RackComponent 的互動性，啟用或禁用 Collider。
        /// </summary>
        public void SetInteractable(bool isEnabled)
        {
            if (components == null || components.Count == 0) return;
            for (int i = 0; i < components.Count; i++)
            {
                if (components[i] == null || components[i].hitCollider == null) continue;
                components[i].hitCollider.enabled = isEnabled;
            }
        }

        [Button, ShowIf("isHaveModels")]
        public void Clear()
        {
            UnsubscribeAll(); // 先取消訂閱，避免重複訂閱事件
            models = new List<Transform>();
            data = new List<DCR_Asset>();
            components?.Clear();
        }

        public void SetRackDatas(List<DCR_Asset> rackDatas)
        {
            data = rackDatas;
            if (isHaveModels && (isHaveData || buildWithoutData)) SetRackComponents();
        }

        public void SetModels(List<Transform> modelList)
        {
            models = modelList;
            if (isHaveModels && (isHaveData || buildWithoutData)) SetRackComponents();
        }

        /// <summary>
        /// 將模型與資產資料進行比對，並將對應的資料設定到 RackComponent 上。
        /// </summary>
        [Button, ShowIf("isHaveModels")]
        private void SetRackComponents()
        {
            UnsubscribeAll(); // 先取消訂閱，避免重複訂閱事件

            components ??= new List<RackComponent>();
            components?.Clear();

            for (int i = 0; i < models.Count; i++)
            {
                Transform model = models[i];
                if (model == null) continue;
                model.gameObject.TryAddComponent(out RackComponent comp);
                CompareAndSetData(model, comp);
                components?.Add(comp);
            }
            if (isActiveAndEnabled) SubscribeAll(); // 只在組件啟用時訂閱事件，避免在禁用狀態下觸發事件
        }

        /// <summary>
        /// 比較模型與資產資料，並將對應的資料設定到 RackComponent 上。
        /// </summary>
        private void CompareAndSetData(Transform model, RackComponent comp)
        {
            // Assuming that the model's name corresponds to the asset's name
            var asset = data?.Find(d => d.assetInfo.assetName == model.name);
            if (asset != null)
            {
                asset.modelInfo.SetModelTarget(model);
                comp.SetRackData(asset);
            }
            else
            {
                comp.SetRackData(null); // 明確處理「沒找到」的情況，設定為 null，避免使用舊資料
            }
        }

        private void OnEnable() => SubscribeAll();
        private void OnDisable() => UnsubscribeAll();

        private void SubscribeAll()
        {
            if (isSubscribedEvents) return;
            if (components == null || components.Count == 0) return; // 尚未初始化，安靜跳過

            for (int i = 0; i < components.Count; i++)
            {
                if (components[i] == null) continue;
                components[i].OnModelClickedEvent += HandleModelClicked;
                components[i].OnHoverEnterEvent += HandleHoverEnter;
                components[i].OnHoverExitEvent += HandleHoverExit;
            }
            isSubscribedEvents = true;
        }

        private void UnsubscribeAll()
        {
            if (components != null)
            {
                for (int i = 0; i < components.Count; i++)
                {
                    if (components[i] == null) continue;
                    components[i].OnModelClickedEvent -= HandleModelClicked;
                    components[i].OnHoverEnterEvent -= HandleHoverEnter;
                    components[i].OnHoverExitEvent -= HandleHoverExit;
                }
            }
            isSubscribedEvents = false; // 不管 components 是否為 null，flag 永遠跟著重置
        }
        private void HandleModelClicked(DCR_Asset asset) => OnModelClickedEvent?.Invoke(asset);
        private void HandleHoverEnter(DCR_Asset asset) => OnHoverEnterEvent?.Invoke(asset);
        private void HandleHoverExit(DCR_Asset asset) => OnHoverExitEvent?.Invoke(asset);
    }
}
