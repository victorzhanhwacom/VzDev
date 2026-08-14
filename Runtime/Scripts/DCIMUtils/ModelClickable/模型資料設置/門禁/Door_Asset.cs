using System;
using VzDev.DCIMUtils.DataUtils;

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
