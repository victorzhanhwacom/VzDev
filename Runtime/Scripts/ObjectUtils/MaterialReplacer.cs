using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;
using VzDev.MaterialUtils;

namespace VzDev.ObjectUtils
{
    /// 處理3D物件的材質替換
    public class MaterialReplacer : MonoBehaviour
    {
        #region Variables

        [Label("[Target Models]"), SerializeField] private List<Transform> targetModels;
        [Label("[Exclude Models]"), SerializeField] private List<Transform> excludeModels;
        [Foldout("[Settings]"), SerializeField] private Material replaceMaterial;

        private bool _isMaterialReplaced;

        private bool IsHaveTargetModels => targetModels != null && targetModels.Count > 0
                        && Application.isPlaying && !_isMaterialReplaced;

        private bool IsReplaceMaterial => Application.isPlaying && _isMaterialReplaced;


        #endregion

        /// <summary>
        /// 設定要替換材質的目標模型，這些模型將被替換為指定的材質
        /// </summary>
        public void SetTargetModels(List<Transform> models) => targetModels = models;
        /// <summary>
        /// 設定排除的模型，這些模型將不會被替換材質
        /// </summary>
        public void SetExcludeModels(List<Transform> models) => excludeModels = models;

        /// 將目標模型材質替換為指定材質
        [Button, ShowIf(nameof(IsHaveTargetModels))]
        public void ReplaceModelsMaterial()
        {
            if (Application.isPlaying == false)
            {
                Debug.LogWarning("Material replacement can only be performed in Play mode.");
                return;
            }
            if (excludeModels != null && excludeModels.Count > 0)
            {
                MaterialHelper.ReplaceMaterial(targetModels, replaceMaterial, excludeModels);
            }
            else
            {
                MaterialHelper.ReplaceMaterial(targetModels, replaceMaterial);
            }
            _isMaterialReplaced = true;
        }

        /// 將材質恢復為原始材質
        [Button, ShowIf(nameof(IsReplaceMaterial))]
        public void RestoreModelsMaterial()
        {
            if (Application.isPlaying == false)
            {
                Debug.LogWarning("Material restore can only be performed in Play mode.");
                return;
            }

            MaterialHelper.RestoreMaterial(targetModels);
            _isMaterialReplaced = false;
        }

        public void ToReplaceMaterial(bool isOn)
        {
            if (isOn)
                ReplaceModelsMaterial();
            else
                RestoreModelsMaterial();
        }
    }
}
