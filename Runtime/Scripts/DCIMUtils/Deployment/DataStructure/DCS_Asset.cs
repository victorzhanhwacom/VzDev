using System;

namespace VzDev.DCIM.RevitAssetDataStructure
{
    /// <summary>
    /// 設備資產資料 (DCS專用) - 伺服主機
    /// </summary>
    [Serializable]
    public class DCS_Asset : EquipmentAsset
    {
        public DCS_Asset() => category = DCIMCategory.DCS;
           
        ///未來會有IP&Port類別
    }
}

