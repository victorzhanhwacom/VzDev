using System;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;
using VzDev.DCIM.Import;
using VzDev.DCIM.RevitAssetDataStructure;
using VzDev.DCIMUtils.ModelInteractUtils;
using VzDev.DebugUtils;

namespace VzDev.DCIMUtils.Deployment
{
    public class RackComponentSetter : ModelComponentSetterBase<DCR_Asset, RackComponent>
    {
        #region Fields
        [Foldout("[Components]"), SerializeField] private BoxCollider equipmentDeployCollider;
        private int dataReadyCount = 0, dataReadyTotal = 2;
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

        public void ParseRackListInformation(bool isSuccess, string json)
        {
            if (!isSuccess)
            {
                Debug.LogError("Failed to get rack list information.");
                return;
            }
            SetDatas(RackAssetJsonConverter.ParseFromJson(json));
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            WebAPIManager.OnGetRackListInformationAction += ParseRackListInformation;
            OnSetComponentsCompletedAction += OnSetComponentsCompletedHandler;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            WebAPIManager.OnGetRackListInformationAction -= ParseRackListInformation;
            OnSetComponentsCompletedAction -= OnSetComponentsCompletedHandler;
        }

        public static Action onSetDeployColliderCompleted;
    }
}
