using System;
using UnityEngine;

namespace VzDev.DeviceDeploymentUtils
{
    /// <summary>
    /// Revit模型資料
    /// </summary>
    [Serializable]
    public class RevitAsset
    {
        /// <summary>
        /// 模型組成碼
        /// </summary>
        public string deviceCode;

        /// <summary>
        /// 模型名稱
        /// </summary>
        public string deviceName;

        public Sprite assetIcon;
        public COBieInfo cobieInfo = new ();
        public ModelInfo modelInfo = new ();
    }
}