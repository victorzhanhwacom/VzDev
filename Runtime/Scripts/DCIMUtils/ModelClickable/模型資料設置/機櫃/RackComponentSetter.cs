using System;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;
using VzDev.DCIMUtils.Import;
using VzDev.DCIMUtils.DataUtils;
using VzDev.DCIMUtils.ModelInteractUtils;
using VzDev.DebugUtils;

namespace VzDev.DCIMUtils.DeploymentUtils
{
    public class RackComponentSetter : ModelComponentSetterBase<DCR_Asset, RackComponent>
    {
        #region Fields
        [Foldout("[Components]"), SerializeField] private BoxCollider equipmentDeployCollider;
      //  private int dataReadyCount = 0, dataReadyTotal = 2;
        #endregion

        private void OnSetComponentsCompletedHandler(List<RackComponent> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == null) continue;
                var rackData = list[i].GetAsset();
                if (rackData == null || rackData.modelInfo == null || rackData.modelInfo.modelTarget == null) continue;
                ObjectHelper.Instantiate(equipmentDeployCollider, rackData.modelInfo.modelTarget);
            }
            onSetDeployColliderCompleted?.Invoke();
        }

        public void ParseRackListInformation(string json)
        {
            SetDatas(RackAssetJsonConverter.ParseFromJson(json));
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            WebAPIManager_EquipmentDeploy.OnGetRackListInformationAction += ParseRackListInformation;
            OnSetComponentsCompletedAction += OnSetComponentsCompletedHandler;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            WebAPIManager_EquipmentDeploy.OnGetRackListInformationAction -= ParseRackListInformation;
            OnSetComponentsCompletedAction -= OnSetComponentsCompletedHandler;
        }

        public static Action onSetDeployColliderCompleted;
    }
}
