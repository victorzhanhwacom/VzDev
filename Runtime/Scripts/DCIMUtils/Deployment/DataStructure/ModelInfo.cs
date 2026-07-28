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
        /// 目標模型的 Transform，用來Camera定位用
        /// </summary>
        public Transform modelTarget;

        public void SetModelTarget(Transform model) => modelTarget = model;
    }
}