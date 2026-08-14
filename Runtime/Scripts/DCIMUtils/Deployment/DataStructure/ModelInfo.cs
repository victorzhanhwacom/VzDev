using System;
using Newtonsoft.Json;
using UnityEngine;

namespace VzDev.DCIMUtils.DataUtils
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
        [field: SerializeField, JsonIgnore]
        public Transform modelTarget;
        public string modelName;
    }
}