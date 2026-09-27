using System;
using UnityEngine;

namespace VzDev.DCIMUtils.DataUtils
{
    /// <summary>
    /// Revit模型資料
    /// </summary>
    [Serializable]
    public class RevitAsset
    {
        /// <summary>
        /// 模型名稱
        /// </summary>
        public string deviceName;

        /// <summary>
        /// 模型索引碼
        /// </summary>
        public string deviceCode;

        public Sprite assetPhotoSprite;

        public COBieInfo cobieInfo = new ();
        public ModelInfo modelInfo = new ();
    }
}