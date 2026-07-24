using System;
using System.Collections.Generic;
using System.Linq;

namespace VzDev
{
    public static class EnumExtensions
    {
        /// <summary>
        /// 檢查字串是否包含指定 enum 的任一個值，並回傳第一個符合的 enum。
        /// 找不到則回傳 null。
        /// </summary>
        public static T? GetMatchedEnum<T>(this string source) where T : struct, Enum
        {
            foreach (T value in Enum.GetValues(typeof(T)))
            {
                if (source.Contains(value.ToString()))
                    return value;
            }
            return null;
        }

        /// <summary>
        /// 回傳字串中所有符合的 enum 值（可能不只一個）。
        /// </summary>
        public static List<T> GetMatchedEnums<T>(this string source) where T : struct, Enum
        {
            return Enum.GetValues(typeof(T))
                       .Cast<T>()
                       .Where(value => source.Contains(value.ToString()))
                       .ToList();
        }
    }
}