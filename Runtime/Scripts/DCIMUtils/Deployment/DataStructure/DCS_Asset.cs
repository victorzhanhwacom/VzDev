using System;

namespace VzDev.DCIMUtils.DataUtils
{
    /// <summary>
    /// 設備資產資料 (DCS專用) - 伺服主機
    /// </summary>
    [Serializable]
    public class DCS_Asset : EquipmentAsset
    {
        public DCS_Asset() => system = DCIM_System.DCS;
           
        ///未來會有IP&Port類別
    }
}

