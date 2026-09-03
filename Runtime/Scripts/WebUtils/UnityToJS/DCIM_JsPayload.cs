using System;
using UnityEngine;

namespace VzDev.WebGLUtils
{
    [Serializable]
    public class DCIM_JsPayload
    {
        public EnumJsAction action = EnumJsAction.Unknown;
        [TextArea]public string payload;
    }

    #region 各 action 對應的 payload class
    [Serializable]
    public class UserTokenPayload
    {
        public string userToken;
    }

    [Serializable]
    public class SwitchSystemMenuPayload
    {
        public EnumSystemMenu systemMenu;
    }

    [Serializable]
    public class SwitchFloorPayload
    {
        public EnumFloor floor;
    }

    [Serializable]
    public class ClickModelPayload
    {
        public string deviceCode;
    }

    #endregion

    public enum EnumJsAction
    {
        Unknown,
        [InspectorName("使用者登入Token")] UserToken,
        [InspectorName("切換系統選單")] SwitchSystemMenu,
        [InspectorName("切換樓層")] SwitchToFloor,
        [InspectorName("點擊模型")] SimulateClickModel,
        [InspectorName("取消點擊模型")] SimulateClickEmpty,
    }

    /// <summary>
    /// 切換系統選單
    /// </summary>
    public enum EnumSystemMenu
    {
        Unknown,
        [InspectorName("電力DCP")] DCP,
        [InspectorName("環控-溫度")] RT,
        [InspectorName("環控-濕度")] RH,
        [InspectorName("環控-漏水帶")] WLK,
        [InspectorName("BMS-空調")] HVAC,
        [InspectorName("BMS-消防")] FS,
        [InspectorName("CCTV")] CCTV,
        [InspectorName("門禁")] ACS,
        [InspectorName("設備資產")] ICT,
        [InspectorName("配置管理")] EquipmentDeployment,
        [InspectorName("告警管理")] Alarm,
    }

    public enum EnumFloor
    {
        Unknown,
        Building,
        RF,
        [InspectorName("15F")] Floor15,
        B1F
    }
}
