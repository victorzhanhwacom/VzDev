using System;
using VzDev.MathUtils;

namespace VzDev.DCIMUtils.DataUtils
{
    /// <summary>
    /// DCIM內所有資產資料
    /// </summary>
    [Serializable]
    public class DCIMAsset : RevitAsset
    {
        public DCIMCategory system = DCIMCategory.Unknow;
        public CompanyPropertyInfo companyPropertyInfo = new ();
        public SizeInfo sizeInfo = new ();
        
        public string DeviceCategory => system.ToString();
    }

    /// <summary>
    /// DCIM資產類別 DCR:機房設備 DCS:機房系統 DCN:網路設備 DCE:電力設備 DCP:週邊設備
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

        /// <summary>
        /// 備註
        /// </summary>
        public string note;

         /// <summary>
        /// 自動產生財產編號 (DEMO用)
        /// </summary>
        public string GenerateRandomPropertyNo(string prefix, int length = 8)
        {
            int number = UnityEngine.Random.Range(0, MathHelper.GetAllNines(length));
            propertyNumber = $"{prefix}-{number.ToString($"D{length}")}";
            return propertyNumber;
        }
    }
}