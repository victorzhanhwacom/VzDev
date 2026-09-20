using UnityEngine;
using VzDev.UnityAPI.Extensions;

namespace VzDev.DCIMUtils.Extensions
{
    public static class DCIMExtension
    {
        #region 取得模型的deviceCode

        /// <summary>
        /// 取得模型的deviceCode
        /// </summary>
        public static string GetDeviceCode(this string self) => self.GetStringBetweenMarks("[", "]");
        /// <summary>
        /// 取得模型的deviceCode
        /// </summary>
        public static string GetDeviceCode(this GameObject self) => GetDeviceCode(self.name);
        /// <summary>
        /// 取得模型的deviceCode
        /// </summary>
        public static string GetModelDeviceCode(this Transform self) => GetDeviceCode(self.name);
        #endregion
    }
}
