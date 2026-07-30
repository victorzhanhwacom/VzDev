using System.Linq;

namespace VzDev.ApiExtensions
{
    public static class ArrayExtension
    {
        #region 依關鍵字進行過濾

        /// [Extended] -  陣列合併，並去除重覆值
        public static T[] Combine<T>(this T[] self, T[] others) => others.Concat(self).Distinct().ToArray();

        /// [Extended] -  列出所有陣列元素
        public static string ToPrint<T>(this T[] self) => string.Join(", ", self);

        #endregion  
    }
}