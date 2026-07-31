using System;
using VzDev.DCIM.RevitAssetDataStructure;

namespace VzDev
{
    [Serializable]
    public class Door_Asset : DCIMAsset
    {
        /// <summary>
        /// 關聯的CCTV資產列表
        /// </summary>
        public Cctv_Asset[] cctvAssets;
    }
}
