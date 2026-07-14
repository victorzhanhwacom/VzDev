using System;
using NaughtyAttributes;
using UnityEngine;
using VzDev.DCIM.Deployment;
using VzDev.InteractiveUtils.ModelMouseEvent;
using VzDev.UnityAPI.Extensions;

namespace VzDev.DCIMUtils
{
    public class RackComponent : MonoBehaviour, IModelClick, IModelHover
    {
        #region Fields
        [SerializeField, ReadOnly] private DCR_Asset data;

        public Collider hitCollider { get; private set; }
        private bool isHaveData => data != null;

        #endregion

        #region Events
        public event Action<DCR_Asset> OnModelClickedEvent;
        public event Action<DCR_Asset> OnHoverEnterEvent;
        public event Action<DCR_Asset> OnHoverExitEvent;
        #endregion

        private void Awake()
        {
            hitCollider = GetComponent<Collider>();
            if (hitCollider == null)
                Debug.LogWarning($"[{nameof(RackComponent)}] {gameObject.name} 沒有 Collider，無法進行互動。", this);
        }

        public void SetRackData(DCR_Asset rackData)
        {
            data = rackData;
            if (isHaveData) data.modelInfo.SetModelTarget(transform);
        }

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
    }
}
