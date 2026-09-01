using System;

namespace VzDev.DCIMUtils.DataUtils
{
    /// <summary>
    /// 設備資產資料 (DCN專用) - 網路設備
    /// </summary>
    [Serializable]
    public class DCN_Asset : EquipmentAsset
    {
        public DCN_Asset() => system = DCIMCategory.DCN;
        
        ///未來會有路由表類別
    }
}

