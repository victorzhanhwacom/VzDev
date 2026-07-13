using System;
using NaughtyAttributes;
using UnityEngine;
using VzDev.DCIM.Deployment;
using VzDev.InteractiveUtils.ModelMouseEvent;

namespace VzDev.DCIMUtils
{
    public class RackComponent : MonoBehaviour, IModelClick, IModelHover
    {
        [SerializeField, ReadOnly] private DCR_Asset data;

        public event Action<DCR_Asset> OnModelClickedEvent;
        public event Action<DCR_Asset> OnHoverEnterEvent;
        public event Action<DCR_Asset> OnHoverExitEvent;

        public void SetRackData(DCR_Asset rackData)
        {
            data = rackData;
            data.modelInfo.SetModelTarget(transform);
        }

        public void OnHoverEnter(GameObject targetObject)
        {
            Debug.Log($"Hover Enter: {targetObject.name}");
            OnHoverEnterEvent?.Invoke(data);
        }

        public void OnHoverExit(GameObject targetObject)
        {
            OnHoverExitEvent?.Invoke(data);
        }

        public void OnModelClicked(GameObject clickedObject)
        {
            Debug.Log($"Model Clicked: {clickedObject.name}");
            OnModelClickedEvent?.Invoke(data);
        }
    }
}
