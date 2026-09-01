using NaughtyAttributes;
using UnityEngine;
using VzDev.DCIMUtils.DataUtils;
using VzDev.DebugUtils;

namespace VzDev.DCIMUtils.DeploymentUtils
{
    [RequireComponent(typeof(MeshCollider))]
    public class DataModelBinder_Equipment : MonoBehaviour
    {
        [SerializeField, ReadOnly] private EquipmentAsset equipmentAsset;
        [Foldout("[Components]"), SerializeField] private MeshCollider meshCollider;

        public EquipmentAsset EquipmentAsset => equipmentAsset;

        public void SetEquipmentAsset(EquipmentAsset data)
        {
            equipmentAsset = (data.deviceCode.Contains("DCS") || data.deviceCode.Contains("Server")) ?
            data.ToDCSAsset() : data.ToDCNAsset();

            equipmentAsset.modelInfo ??= new ModelInfo();
            equipmentAsset.modelInfo.modelTarget = transform;
            equipmentAsset.modelInfo.modelName = transform.name;
        }

        private void SetMeshColliderEnabled(EquipmentAsset asset) => SetInteractable(false);
        private void SetMeshColliderDisabled() => SetInteractable(true);
        private void SetInteractable(bool isInteractable)
        {
            if (meshCollider != null) meshCollider.enabled = isInteractable;
        }
        #region Event Listener
        private void OnEnable()
        {
            StockEquipmentList.OnStockEquipmentItemSelectedAction += SetMeshColliderEnabled;
            StockEquipmentList.OnStockEquipmentItemDeselectedAction += SetMeshColliderDisabled;
        }
        private void OnDisable()
        {
            StockEquipmentList.OnStockEquipmentItemSelectedAction -= SetMeshColliderEnabled;
            StockEquipmentList.OnStockEquipmentItemDeselectedAction -= SetMeshColliderDisabled;
        }
        #endregion

        private void OnValidate()
        {
            if (meshCollider == null) meshCollider = GetComponent<MeshCollider>();
        }
        private void OnDestroy()
        {
            if (meshCollider != null) ObjectHelper.Destroy(meshCollider);
        }
    }
}
