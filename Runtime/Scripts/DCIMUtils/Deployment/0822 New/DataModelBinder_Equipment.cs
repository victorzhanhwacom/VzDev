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


        public void SetEquipmentAsset(EquipmentAsset data)
        {
            equipmentAsset = data;
            equipmentAsset.modelInfo ??= new ModelInfo();
            equipmentAsset.modelInfo.modelTarget = transform;
            equipmentAsset.modelInfo.modelName = transform.name;
        }

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
