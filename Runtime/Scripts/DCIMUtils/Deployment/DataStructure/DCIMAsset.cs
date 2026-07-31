using System;
using VzDev.MathUtils;

namespace VzDev.DCIM.RevitAssetDataStructure
{
    /// <summary>
    /// DCIM內所有資產資料
    /// </summary>
    [Serializable]
    public class DCIMAsset : RevitAsset
    {
        public DCIMCategory category = DCIMCategory.Unknow;
        public CompanyPropertyInfo companyPropertyInfo;

        /// <summary>
        /// 自動產生財產編號
        /// </summary>
        public string GenerateRandomPropertyNo()
        {
            string prefix = "NTCGO-";
            int n = 8;
            int index = UnityEngine.Random.Range(0, MathHelper.GetAllNines(n));
            companyPropertyInfo.propertyNumber = $"{prefix}{index.ToString($"D{n}")}";
            return companyPropertyInfo.propertyNumber;
        }
    }

    /// <summary>
    /// DCIM資產類別
    /// </summary>
    public enum DCIMCategory
    {
        Unknow, DCR, DCS, DCN, DCE, DCP
    }

    /// <summary>
    /// 公司資產資訊
    /// </summary>
    [Serializable]
    public class CompanyPropertyInfo
    {
        /// <summary>
        /// 財產名稱
        /// </summary>
        public string propertyName;
        /// <summary>
        /// 財產編號
        /// </summary>
        public string propertyNumber;

        public SizeInfo sizeInfo;

        /// <summary>
        /// 備註
        /// </summary>
        public string note;

    }
}