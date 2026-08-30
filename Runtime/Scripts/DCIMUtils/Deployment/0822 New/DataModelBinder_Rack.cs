using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;
using VzDev.DcimUtils;
using VzDev.DCIMUtils.DataUtils;
using VzDev.DebugUtils;
using VzDev.UnityAPI.Extensions;

namespace VzDev.DCIMUtils.DeploymentUtils
{
    public class DataModelBinder_Rack : MonoBehaviour
    {
        #region Fields
        [SerializeField, ReadOnly] private DCR_Asset rackAsset;
        [Foldout("[Components]"), SerializeField] private MeshCollider meshCollider;
        [Foldout("[Components]"), SerializeField] private BoxCollider rackSlotCollider;

        public DCR_Asset RackAsset => rackAsset;
        #endregion

        /// <summary>
        /// 生成機櫃內的槽位碰撞器，並與 DataModelBinder_Rack 綁定
        /// </summary>
        public void GenerateRackSlotCollider(BoxCollider colliderPrefab)
        {
            if (rackSlotCollider != null) ObjectHelper.Destroy(rackSlotCollider);
            rackSlotCollider = Instantiate(colliderPrefab, transform);
        }
        /// <summary>
        /// 生成機櫃內的設備與對應的設備模型綁定
        /// </summary>
        public void GenerateEquipmentInContainer(List<Transform> equipmentModels)
        {
            if (rackAsset == null || rackAsset.container == null) return;

            foreach (var equipmentData in rackAsset.container)
            {
                Transform model = equipmentModels.Find(m => m.name == equipmentData.modelInfo.modelName);
                if (model == null)
                {
                    Debug.LogWarning($"Equipment model not found for {equipmentData.modelInfo.modelName}");
                    continue;
                }

                Transform equipmentModel = ObjectHelper.Instantiate(model, transform);
                equipmentModel.TryAddComponent(out DataModelBinder_Equipment dataCombiner_Equipment);
                dataCombiner_Equipment.SetEquipmentAsset(equipmentData);

                DcimHelper.SetEquipmentSnapToRackSlot(equipmentModel, rackAsset, rackSlotCollider, equipmentData.startUIndex, equipmentData.equipmentUsageInfo.heightU);
            }
        }

        /// <summary>
        /// 設定機櫃資料與機櫃模型
        /// </summary>
        public void SetRackAsset(DCR_Asset data)
        {
            rackAsset = data;
            rackAsset.modelInfo ??= new ModelInfo();
            rackAsset.modelInfo.modelTarget = transform;
            rackAsset.modelInfo.modelName = transform.name;
            rackAsset.GenerateDeviceNameIfEmpty();
            rackAsset.RefreshUsageInfo();
            transform.TryAddComponent(out meshCollider);
        }

        public void SetRackColliderEnabled(bool isEnabled)
        {
            if (meshCollider == null) return;
            meshCollider.enabled = isEnabled;
        }
        public void SetRackSlotColliderEnabled(bool isEnabled)
        {
            if (rackSlotCollider == null) return;
            rackSlotCollider.enabled = isEnabled;
        }

        public void ToDestroy() => OnDestroy();

        private void OnDestroy()
        {
            if (meshCollider != null) ObjectHelper.Destroy(meshCollider);
            ObjectHelper.Destroy(this);
        }



    }
}
