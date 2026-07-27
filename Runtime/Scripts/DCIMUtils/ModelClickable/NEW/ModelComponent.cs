using System;
using NaughtyAttributes;
using UnityEngine;
using VzDev.DCIM.Deployment;
using VzDev.InteractiveUtils.ModelMouseEvent;
using VzDev.UnityAPI.Extensions;

namespace VzDev.DCIMUtils
{
    /// <summary>
    /// 通用模型掛載元件，取代逐一自訂的 XxxComponent（RackComponent、FanComponent...）。
    /// 只負責「持有 data」+「轉發滑鼠事件」，不含任何設備專屬邏輯。
    /// 若某設備未來需要專屬行為（例如風扇要自轉動畫），再繼承本類別另外處理，
    /// 其餘設備維持直接使用本類別即可，不需要另外寫檔案。
    /// </summary>
    public class ModelComponent<TData> : MonoBehaviour, IModelClick, IModelHover,
                                            IModelComponent<TData>
        where TData : DCIMAsset
    {
        #region Fields
        [SerializeField, ReadOnly, Tooltip("目標模型攜帶的資料項")] private TData data;
        /// <summary>
        /// 
        /// </summary>
        private ComponenetColliderSetter colliderSetter;
        private bool isHaveData => data != null;
        #endregion

        private void Awake() => colliderSetter = new ComponenetColliderSetter(transform);
        public void SetColliderEnabled(bool isEnabled) => colliderSetter.SetColliderEnabled(isEnabled);
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
        private BoxCollider hitCollider;
        public ComponenetColliderSetter(Transform target)
        {
            if (target == null) return;
            target.TryAddComponent(out hitCollider);
        }

        public void SetColliderEnabled(bool isEnabled)
        {
            if (hitCollider != null)
                hitCollider.enabled = isEnabled;
        }
    }
}