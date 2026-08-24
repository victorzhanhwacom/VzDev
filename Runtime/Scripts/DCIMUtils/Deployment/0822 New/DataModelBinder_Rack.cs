using System;
using NaughtyAttributes;
using UnityEngine;
using VzDev.DCIMUtils.DataUtils;
using VzDev.DebugUtils;

namespace VzDev.DCIMUtils.DeploymentUtils
{
    [RequireComponent(typeof(MeshCollider))]
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
