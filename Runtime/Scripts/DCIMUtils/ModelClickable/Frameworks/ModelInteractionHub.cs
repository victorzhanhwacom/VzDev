using System;
using VzDev.DCIM.Deployment;

namespace VzDev.DCIMUtils.ModelInteractUtils
{
    /// <summary>
    /// 集中管理所有ModelComponentSetterBase的互動事件，提供給其他地方訂閱使用。
    /// <para> {外部：點擊處} → ModelComponentSetterHub.OnAnyModelClicked → {外部：訂閱處} </para>
    /// </summary>
    public static class ModelComponentSetterEventHub
    {
        #region 供其它地方訂閱的事件
        public static event Action<DCIMAsset> OnAnyModelClicked;
        public static event Action<DCIMAsset> OnAnyModelHoverEnter;
        public static event Action<DCIMAsset> OnAnyModelHoverExit;
        #endregion

        #region 供外部傳入資料項
        internal static void RaiseClicked(DCIMAsset asset) => OnAnyModelClicked?.Invoke(asset);
        internal static void RaiseHoverEnter(DCIMAsset asset) => OnAnyModelHoverEnter?.Invoke(asset);
        internal static void RaiseHoverExit(DCIMAsset asset) => OnAnyModelHoverExit?.Invoke(asset);
        #endregion
    }
}