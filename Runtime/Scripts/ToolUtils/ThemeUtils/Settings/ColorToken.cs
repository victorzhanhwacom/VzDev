namespace VzDev.ToolUtils.ThemeUtils
{
    public enum ColorToken
    {
        #region 基礎色系
        Primary,
        Secondary,
        Background,
        Panel,
        Border,

        Divider,
        Success,
        Warning,
        Error,
        #endregion

        #region 文字顏色
        TextPrimary,
        TextSecondary,
        #endregion

        #region Button/Toggle 專用
        Normal,
        Highlight,
        Pressed,
        Selected,
        Disabled,
        #endregion

        #region DCIM專用
        DCIM_StatusNormal,
        DCIM_StatusWarning,
        DCIM_StatusAlarm,
        #endregion
    }
}

