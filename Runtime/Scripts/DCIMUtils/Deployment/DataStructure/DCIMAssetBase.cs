using System;

namespace VzDev.DCIM.Deployment
{
    /// <summary>
    /// 機房內所有資產設備資料基底類別
    /// <para>包含：門禁、CCTV、消防、溫濕度感應、機櫃/設備資產類</para>
    /// </summary>
    [Serializable]
    public class DCIMAssetBase
    {
        public AssetInfo assetInfo;
        public COBieInfo cobieInfo;
        public ModelInfo modelInfo;
    }
}