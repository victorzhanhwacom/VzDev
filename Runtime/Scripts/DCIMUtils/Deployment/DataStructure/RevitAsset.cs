using System;
using VzDev.DCIMUtils.DataUtils;

namespace VzDev.DCIM.RevitAssetDataStructure
{
    /// <summary>
    /// Revit模型資料
    /// </summary>
    [Serializable]
    public class RevitAsset
    {
        /// <summary>
        /// 模型索引碼
        /// </summary>
        public string deviceCode;
        public COBieInfo cobieInfo = new ();
        public ModelInfo modelInfo = new ();
        public TimeStampData timeStampData = new TimeStampData();
    }
}