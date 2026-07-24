using System;
using System.Collections.Generic;
using System.Linq;
using NaughtyAttributes;
using UnityEngine;
using VzDev.ObjectUtils;
using static VzDev.UnityAPI.Extensions.TransformExtension;

namespace VzDev
{
    public class MaterialReplacerMediator : MonoBehaviour
    {
        #region Fields

        [SerializeField, ReadOnly] private List<Transform> targetModels;
        [SerializeField] private List<ModelFinderSetting> modelFinderSettings;
        [Foldout("[Components]"), SerializeField] private MaterialReplacer materialReplacer;
        [Foldout("[Components]"), SerializeField] private MaterialReplacer materialReplacerLight, materialReplacerCableTray;
        #endregion

#if UNITY_EDITOR
        #region For NaughtyAttributes
        [Foldout("[手動測試]"), SerializeField, OnValueChanged("OnShowLightModelsChanged")] private bool isShowLightModels = false;
        [Foldout("[手動測試]"), SerializeField, OnValueChanged("OnShowCableTrayModelsChanged")] private bool isShowCableTrayModels = false;
        [Foldout("[手動測試]"), SerializeField, OnValueChanged("OnShowPowerModelsChanged")] private bool isShowPowerModels = false;
        [Foldout("[手動測試]"), SerializeField, OnValueChanged("OnShowEnvModelsChanged")] private bool isShowEnvModels = false;
        [Foldout("[手動測試]"), SerializeField, OnValueChanged("OnShowACModelsChanged")] private bool isShowACModels = false;
        [Foldout("[手動測試]"), SerializeField, OnValueChanged("OnShowCCTVModelsChanged")] private bool isShowCCTVModels = false;
        [Foldout("[手動測試]"), SerializeField, OnValueChanged("OnShowDoorModelsChanged")] private bool isShowDoorModels = false;
        [Foldout("[手動測試]"), SerializeField, OnValueChanged("OnShowCabinetModelsChanged")] private bool isShowCabinetModels = false;

        private void OnShowLightModelsChanged() => SetLightModelVisible(isShowLightModels);
        private void OnShowCableTrayModelsChanged() => SetCableTrayModelVisible(isShowCableTrayModels);
        private void OnShowPowerModelsChanged() => SetPowerModelVisible(isShowPowerModels);
        private void OnShowEnvModelsChanged() => SetEnvModelVisible(isShowEnvModels);
        private void OnShowCCTVModelsChanged() => SetCCTVModelVisible(isShowCCTVModels);
        private void OnShowDoorModelsChanged() => SetDoorModelVisible(isShowDoorModels);
        private void OnShowACModelsChanged() => SetACModelVisible(isShowACModels);
        private void OnShowCabinetModelsChanged() => SetCabinetModelVisible(isShowCabinetModels);
        #endregion
#endif

        [Button]
        public void RestoreModelsMaterial() => materialReplacer.RestoreModelsMaterial();

        public void SetLightModelVisible(bool isVisible)
        {
            if (isVisible) materialReplacerLight.RestoreModelsMaterial();
            else materialReplacerLight.ReplaceModelsMaterial();
        }
        public void SetCableTrayModelVisible(bool isVisible)
        {
            if (isVisible) materialReplacerCableTray.RestoreModelsMaterial();
            else materialReplacerCableTray.ReplaceModelsMaterial();
        }

        public void SetPowerModelVisible(bool isVisible)
        {
            if (isVisible) ShowModels(EnumModelType.能源管理);
            else RestoreModelsMaterial();
        }
        public void SetEnvModelVisible(bool isVisible)
        {
            if (isVisible) ShowModels(EnumModelType.環境管理);
            else RestoreModelsMaterial();
        }
        public void SetCCTVModelVisible(bool isVisible)
        {
            if (isVisible) ShowModels(EnumModelType.CCTV);
            else RestoreModelsMaterial();
        }
        public void SetDoorModelVisible(bool isVisible)
        {
            if (isVisible) ShowModels(EnumModelType.門禁管理);
            else RestoreModelsMaterial();
        }
        public void SetACModelVisible(bool isVisible)
        {
            if (isVisible) ShowModels(EnumModelType.空調系統);
            else RestoreModelsMaterial();
        }
        public void SetCabinetModelVisible(bool isVisible)
        {
            if (isVisible) ShowModels(EnumModelType.機櫃管理);
            else RestoreModelsMaterial();
        }
        /// <summary>
        /// 顯示相對應的模型，並將材質替換為指定材質
        /// </summary>
        private void ShowModels(EnumModelType enumModelType)
        {
            var targetModelFinderSetting = modelFinderSettings.FirstOrDefault(x => x.modelType == enumModelType);
            if (targetModelFinderSetting.modelFinder == null)
            {
                Debug.LogWarning($"No ModelFinder found for {enumModelType}. Please ensure that a ModelFinder is assigned in the inspector.");
                return;
            }
            targetModels = targetModelFinderSetting.modelFinder.FoundModels;
            materialReplacer.RestoreModelsMaterial();
            materialReplacer.SetTargetModels(targetModels);
            materialReplacer.ReplaceModelsMaterial();
        }

        [Button]
        public void GetModelFinderWithExpectInChildren()
        {
            modelFinderSettings = new List<ModelFinderSetting>();
            var modelFinders = GetComponentsInChildren<ModelFinder>(true);
            var enumSet = Enum.GetValues(typeof(EnumModelType));
            foreach (var modelFinder in modelFinders)
            {
                if (modelFinder.SearchType == EnumSearchType.Exclude)
                {
                    modelFinderSettings.Add(new ModelFinderSetting()
                    {
                        modelType = modelFinder.name.GetMatchedEnum<EnumModelType>() ?? EnumModelType.Unknown,
                        modelFinder = modelFinder
                    });
                }
            }
        }


        [System.Serializable]
        public struct ModelFinderSetting
        {
            public EnumModelType modelType;
            public ModelFinder modelFinder;
        }

        public enum EnumModelType
        {
            Unknown,
            能源管理,
            環境管理,
            CCTV,
            門禁管理,
            空調系統,
            機櫃管理,
        }
    }
}
