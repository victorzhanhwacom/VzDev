using System;
using NaughtyAttributes;
using UnityEngine;
using VzDev.DCIMUtils.DataUtils;
using VzDev.InteractiveUtils.ModelMouseEvent;

namespace VzDev.DCIMUtils.DeploymentUtils
{
    /// <summary>
    /// 控制選取欲上架至目標機櫃對像
    /// </summary>
    public class DeployToRackSelector : MonoBehaviour
    {
        #region Fields
        [SerializeField, ReadOnly, Label("選取的庫存設備")] private EquipmentAsset selectedStockEquipment;
        [SerializeField, ReadOnly, Label("選取的機櫃資產")] private DCR_Asset selectedRackAsset;
        #endregion

        #region Event Listener
        private void OnEnable()
        {
            StockEquipmentList.OnStockEquipmentItemSelectedAction += OnStockEquipmentItemSelectedHandler;
            StockEquipmentList.OnStockEquipmentItemDeselectedAction += OnStockEquipmentItemDeselectedHandler;
        }
        private void OnDisable()
        {
            StockEquipmentList.OnStockEquipmentItemSelectedAction -= OnStockEquipmentItemSelectedHandler;
            StockEquipmentList.OnStockEquipmentItemDeselectedAction -= OnStockEquipmentItemDeselectedHandler;
        }

        /// <summary>
        /// 選取庫存設備 (列表), 接下來的點選機櫃皆為選取要上架的目標機櫃
        /// </summary>
        private void OnStockEquipmentItemSelectedHandler(EquipmentAsset asset)
        {
            selectedStockEquipment = asset;
            ColliderInteractionSystem.SimulateClickEmpty();
            ColliderInteractionSystem.OnMouseClick += OnSelecRackTarget;
            ColliderInteractionSystem.OnMouseClickEmpty += DeselectRackTarget;
            OnDeselectRackTargetAction += SetRackTargetNull;
        }
        private void OnStockEquipmentItemDeselectedHandler()
        {
            selectedStockEquipment = null;
            ColliderInteractionSystem.SimulateClickEmpty();
            ColliderInteractionSystem.OnMouseClick -= OnSelecRackTarget;
            ColliderInteractionSystem.OnMouseClickEmpty -= DeselectRackTarget;
            OnDeselectRackTargetAction -= SetRackTargetNull;
        }

        /// <summary>
        /// 選取欲上架至目標機櫃
        /// </summary>
        private void OnSelecRackTarget(GameObject target)
        {
            if (target.TryGetComponent(out DataModelBinder_Rack rackDataBinder))
            {
                selectedRackAsset = rackDataBinder.RackAsset;
                // ColliderInteractionSystem.SetMouseClickEnabled(false);
                OnSelectRackTargetAction?.Invoke(selectedRackAsset);
            }
            else
                ColliderInteractionSystem.SimulateClickEmpty();
        }
        private void SetRackTargetNull() => selectedRackAsset = null;
        public static void DeselectRackTarget()
        {
            // ColliderInteractionSystem.SetMouseClickEnabled(true);
            OnDeselectRackTargetAction?.Invoke();
        }

        #endregion

        /// <summary>
        /// 選取欲上架至目標機櫃 (點選機櫃)
        /// </summary>
        public static Action<DCR_Asset> OnSelectRackTargetAction;
        /// <summary>
        /// 取消選取欲上架至目標機櫃 (點選空白處)
        /// </summary>
        public static Action OnDeselectRackTargetAction;
    }
}
