using System;
using VzDev.EnumUtils;
using VzDev.MathUtils;

namespace VzDev.DCIMUtils.DataUtils
{
    /// <summary>
    /// DCIM內所有資產資料
    /// </summary>
    [Serializable]
    public class DCIMAsset : RevitAsset
    {
        public DCIM_Catetory category = DCIM_Catetory.Unknow;
        public DCIM_System system = DCIM_System.Unknow;
        public CompanyPropertyInfo companyPropertyInfo = new();
        public SizeInfo sizeInfo = new();
        public string DeviceCategory => system.ToString();

        /// <summary>
        /// 檢查設備的系統類別與設備類別(For Demo)
        /// </summary>
        public void CheckSystemAndCategory()
        {
            category = EnumHelper<DCIM_Catetory>.GetEnumFromString(modelInfo.modelName ?? deviceCode);
            system = EnumHelper<DCIM_System>.GetEnumFromString(deviceCode);
            companyPropertyInfo.GenerateRandomPropertyNo("NTCGO");
        }
    }

    /// <summary>
    /// DCIM資產類別 DCR:機房設備 DCS:機房系統 DCN:網路設備 DCE:電力設備 DCP:週邊設備
    /// </summary>
    public enum DCIM_System
    {
        Unknow, DCR, DCS, DCN, DCE, DCP
    }
    public enum DCIM_Catetory
    {
        Unknow, Rack, Server, Switch, Router,
        Storage, Firewall, UPS, PDU, Patch_Panel, PatchPanel
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
        public void GenerateRandomPropertyNo(string prefix, int length = 8)
        {
            if (string.IsNullOrEmpty(propertyNumber) == false) return;
            int number = UnityEngine.Random.Range(0, MathHelper.GetAllNines(length));
            propertyNumber = $"{prefix}-{number.ToString($"D{length}")}";
        }
    }
}