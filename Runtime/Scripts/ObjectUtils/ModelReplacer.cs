using System.Collections.Generic;
using _VictorDev.MaterialUtils;
using NaughtyAttributes;
using UnityEngine;

namespace VzDev.ObjectUtils
{
    /// 處理3D物件的材質替換
    public class MaterialReplacer: MonoBehaviour
    {
        #region Variables
        [Label("[模型列表]"), SerializeField] private List<Transform> targetModels;
        [Foldout("[設定]"), SerializeField] private Material replaceMaterial;
        
        private bool IsRuntime => Application.isPlaying;

        #endregion

        /// 設定目標模型
        public void SetTargetModels(List<Transform> models) => targetModels = models;
        
        /// 將目標模型材質替換為指定材質
        [Button, ShowIf(nameof(IsRuntime))]
        public void ReplaceModelsMaterial() => MaterialHelper.ReplaceMaterial(targetModels, replaceMaterial);
        
        /// 將材質恢復為原始材質
        [Button, ShowIf(nameof(IsRuntime))]
        public void RestoreModelsMaterial() => MaterialHelper.RestoreMaterial(targetModels);

        public void ReceiveData(List<Transform> models)
        {
            targetModels = models;
        }
    }
}
