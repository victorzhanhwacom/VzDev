using System;
using UnityEngine;

namespace VzDev.DCIM.RevitAssetDataStructure
{
    /// <summary>
    /// Revit模型資料
    /// </summary>
    [Serializable]
    public class ModelInfo
    {
        /// <summary>
        /// 目標模型的 Transform，用來Camera定位用
        /// </summary>
        public Transform modelTarget;
        public string ModelName => modelTarget ? modelTarget.name : string.Empty;

        public void SetModelTarget(Transform model) => modelTarget = model;

    }
}