using System;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;
using VzDev.DCIMUtils.DataUtils;
using VzDev.DCIMUtils.ModelInteractUtils;

namespace VzDev.DCIMUtils.DeploymentUtils
{
      public class EquipmentComponentSetter : ModelComponentSetterBase<EquipmentAsset, EquipmentComponent>
      {
            #region Fields
            [SerializeField, ReadOnly] private List<Transform> equipmentModels;
            [SerializeField, ReadOnly] private List<RackComponent> rackComponents;
            private int dataReadyCount = 0, dataReadyTotal = 2;
            #endregion

            public void SetRackComponents(List<RackComponent> racks)
            {
                  rackComponents = racks;
                  TryCreateEquipmentAssetsInStock();
            }

            public void SetEquipmentModels(List<Transform> models)
            {
                  equipmentModels = models;
                  TryCreateEquipmentAssetsInStock();
            }

            private void TryCreateEquipmentAssetsInStock()
            {
                  if (++dataReadyCount < dataReadyTotal) return;
                  for (int i = 0; i < rackComponents.Count; i++)
                  {
                        if (rackComponents[i] == null) continue;
                        DCR_Asset dcrAsset = rackComponents[i].GetAsset() as DCR_Asset;
                        CreateEquipmentComponentsInRack(dcrAsset);
                  }
            }

            private void CreateEquipmentComponentsInRack(DCR_Asset dcrAsset)
            {
                  Debug.Log($"CreateEquipmentComponentsInRack{dcrAsset.companyPropertyInfo.propertyName}");
                  if (dcrAsset == null || dcrAsset.modelInfo == null || dcrAsset.modelInfo.modelTarget == null) return;
                  if (dcrAsset.container == null || dcrAsset.container.Count == 0) return;

                  Debug.Log($"在機櫃 {dcrAsset.companyPropertyInfo.propertyName} 中生成設備模型，數量: {dcrAsset.container.Count}");

                  for (int i = 0; i < dcrAsset.container.Count; i++)
                  {
                        EquipmentAsset equipmentAsset = dcrAsset.container[i] as EquipmentAsset;
                        if (equipmentAsset == null) continue;
                        GameObject modelPrefab = equipmentModels.Find(m => m.name.Contains(equipmentAsset.modelInfo.modelName))?.gameObject;
                        if (modelPrefab == null) continue;

                        GameObject modelInstance = Instantiate(modelPrefab, dcrAsset.modelInfo.modelTarget);
                        modelInstance.AddComponent<EquipmentComponent>().SetData(equipmentAsset);
                  }
            }

            #region Event Listener
            protected override void OnEnable()
            {
                  base.OnEnable();
                  WebAPIManager.OnGetEquipmentModelsAction += SetEquipmentModels;
                  RackComponentSetter.OnSetComponentsCompletedAction += SetRackComponents;
            }

            protected override void OnDisable()
            {
                  base.OnDisable();
                  WebAPIManager.OnGetEquipmentModelsAction -= SetEquipmentModels;
                  RackComponentSetter.OnSetComponentsCompletedAction -= SetRackComponents;
            }
            #endregion
      }
}
