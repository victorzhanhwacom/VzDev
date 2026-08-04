using System;
using NaughtyAttributes;
using UnityEngine;
using VzDev.DCIM.RevitAssetDataStructure;
using VzDev.DCIMUtils.ModelInteractUtils;
using VzDev.DebugUtils;

namespace VzDev
{
    public class RackComponentSetter : ModelComponentSetterBase<DCR_Asset, RackComponent>
    {
        [Foldout("[Components]"), SerializeField] private BoxCollider equipmentDeployCollider;

        private void OnSetComponentsCompleted()
        {
            for(int i=0; i<models.Count; i++)
            {
                if (models[i] == null) continue;
                if (components[i] == null) continue;
                ObjectHelper.Instantiate(equipmentDeployCollider, models[i]);
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            WebAPI_GetRackList.OnGetRackAssetsEvent += SetDatas;
            onSetComponentsCompleted += OnSetComponentsCompleted;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            WebAPI_GetRackList.OnGetRackAssetsEvent -= SetDatas;
            onSetComponentsCompleted -= OnSetComponentsCompleted;
        }
    }
}
