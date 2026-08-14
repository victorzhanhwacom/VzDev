using UnityEngine;
using VzDev.UnityAPI.Extensions;

namespace VzDev.DCIMUtils.Extensions
{
    public static class DCIMExtension
    {
        #region 取得模型的deviceCode
        public static string GetDeviceCode(this string self) => self.GetStringBetweenMarks("[", "]");
        public static string GetDeviceCode(this GameObject self) => GetDeviceCode(self.name);
        public static string GetModelDeviceCode(this Transform self) => GetDeviceCode(self.name);
        #endregion
    }
}
