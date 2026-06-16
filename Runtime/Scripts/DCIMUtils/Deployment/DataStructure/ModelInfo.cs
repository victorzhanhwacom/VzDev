using System;
using UnityEngine;

namespace VzDev.DCIM.Deployment
{
    /// <summary>
    /// DCIM模型資料
    /// </summary>
    [Serializable]
    public class ModelInfo
    {
        /// <summary>
        /// 模型對像Prefab, 在Instantiating後會將實例化的對象賦值給modelTarget
        /// </summary>
        public Transform modelTarget;

        public void SetModelTarget(Transform model) => modelTarget = model;
    }
}