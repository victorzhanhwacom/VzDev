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

        Success,
        Warning,
        Error,
        #endregion

        #region 文字顏色
        TextPrimary,
        TextSecondary,
        TextTitle,  
        TextInputBackground,
        TextDisabled,
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

        ItemSelected,
    }
}

