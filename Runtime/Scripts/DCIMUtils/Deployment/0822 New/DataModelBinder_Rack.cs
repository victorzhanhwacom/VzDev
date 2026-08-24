using NaughtyAttributes;
using UnityEngine;
using VzDev.DCIMUtils.DataUtils;
using VzDev.DebugUtils;
using VzDev.UnityAPI.Extensions;

namespace VzDev.DCIMUtils.DeploymentUtils
{
    public class DataModelBinder_Rack : MonoBehaviour
    {
        [SerializeField, ReadOnly] private DCR_Asset rackAsset;
        [Foldout("[Components]"), SerializeField] private MeshCollider meshCollider;

        public void SetRackAsset(DCR_Asset data)
        {
            rackAsset = data;
            rackAsset.modelInfo ??= new ModelInfo();
            rackAsset.modelInfo.modelTarget = transform;
            rackAsset.modelInfo.modelName = transform.name;
            rackAsset.RefreshUsageInfo();
            transform.TryAddComponent(out meshCollider);
        }

        public void SetColliderEnabled(bool isEnabled)
        {
            if (meshCollider == null) return;
            meshCollider.enabled = isEnabled;
        }

        public void ToDestroy() => OnDestroy();

        private void OnDestroy()
        {
            if (meshCollider != null) ObjectHelper.Destroy(meshCollider);
            ObjectHelper.Destroy(this);
        }
    }
}
