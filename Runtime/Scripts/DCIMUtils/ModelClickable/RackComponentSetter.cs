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
        [SerializeField, ReadOnly] private List<Transform> models;
        [SerializeField, ReadOnly] private List<DCR_Asset> data;
        private bool isHaveModels => models != null && models.Count > 0;
        private List<RackComponent> components;

        #endregion  

        #region Events
        public static event Action<DCR_Asset> OnModelClickedEvent;
        public static event Action<DCR_Asset> OnHoverEnterEvent;
        public static event Action<DCR_Asset> OnHoverExitEvent;
        #endregion

        [Button, ShowIf("isHaveModels")]
        public void ClearData()
        {
            models = new List<Transform>();
            data = new List<DCR_Asset>();
            components?.Clear();
        }

        public void SetModels(List<Transform> modelList) => models = modelList;
        public void SetRackDatas(List<DCR_Asset> rackDatas) => data = rackDatas;

        [Button, ShowIf("isHaveModels")]
        public void SetRackComponents()
        {
            if (!isHaveModels)
            {
                Debug.LogWarning("No models found to set RackComponents on.", this);
                return;
            }
            components ??= new List<RackComponent>();
            components?.Clear();

            foreach (var model in models)
            {
                if (model == null) continue;
                model.gameObject.TryAddComponent(out RackComponent comp);
                CompareAndSetData(model, comp);
                components?.Add(comp);
            }
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
        }

        private void OnEnable()
        {
            Debug.Log($"OnEnable called. Models count: {models?.Count}, Data count: {data?.Count}, Components count: {components?.Count}", this);
            for (int i = 0; i < components.Count; i++)
            {
                if (components[i] == null) continue;
                components[i].OnModelClickedEvent += HandleModelClicked;
                components[i].OnHoverEnterEvent += HandleHoverEnter;
                components[i].OnHoverExitEvent += HandleHoverExit;
            }
        }
        private void OnDisable()
        {
            Debug.Log($"OnDisable called. Models count: {models?.Count}, Data count: {data?.Count}, Components count: {components?.Count}", this);
            for (int i = 0; i < components.Count; i++)
            {
                if (components[i] == null) continue;
                components[i].OnModelClickedEvent -= HandleModelClicked;
                components[i].OnHoverEnterEvent -= HandleHoverEnter;
                components[i].OnHoverExitEvent -= HandleHoverExit;
            }
        }
        private void HandleModelClicked(DCR_Asset asset) => OnModelClickedEvent?.Invoke(asset);
        private void HandleHoverEnter(DCR_Asset asset) => OnHoverEnterEvent?.Invoke(asset);
        private void HandleHoverExit(DCR_Asset asset) => OnHoverExitEvent?.Invoke(asset);
    }
}
