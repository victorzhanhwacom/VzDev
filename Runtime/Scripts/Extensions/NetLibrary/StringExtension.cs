using System;
using UnityEngine;

namespace VzDev.NetLibrary.Extensions
{
    public static class StringExtension
    {
        /// <summary>
        /// 判斷字串是否有值（非null、非空字串、非全空白字串）
        /// </summary>
        public static bool IsValueExist(this string self)
        {
            if (ReferenceEquals(self, null)) return false;

            int len = self.Length;
            if (len == 0) return false;

            // Scan through characters to avoid the string allocation caused by Trim()
            for (int i = 0; i < len; i++)
            {
                if (!char.IsWhiteSpace(self[i]))
                {
                    return true; // Found a valid character, string is neither empty nor just whitespaces
                }
            }
            return false;
        }

        #region 是否包含關鍵字（單一/多重）
        /// <summary>
        /// 判斷字串是否包含任一關鍵字（預設忽略大小寫）
        /// </summary>
        public static bool ContainKeyword(this string self, params string[] keywords) => self.ContainKeyword(StringComparison.OrdinalIgnoreCase, keywords);

        /// <summary>
        /// 判斷字串是否包含任一關鍵字（可指定比較類型）
        /// </summary>
        public static bool ContainKeyword(this string self, StringComparison comparisonType, params string[] keywords)
        {
            if (string.IsNullOrEmpty(self) || keywords == null || keywords.Length == 0)
            {
                Debug.LogWarning("ContainKeyword: Input string or keywords are null or empty, returning false.");
                return false;
            }

            for (int i = 0; i < keywords.Length; i++)
            {
                if (string.IsNullOrEmpty(keywords[i])) continue;
                if (self.ContainKeyword(keywords[i], comparisonType)) return true;
            }
            return false;
        }

        /// <summary>
        /// 判斷字串是否包含關鍵字（可指定比較類型）
        /// </summary>
        public static bool ContainKeyword(this string self, string keyword,
            StringComparison comparisonType = StringComparison.OrdinalIgnoreCase)
        {
            if (string.IsNullOrEmpty(self) || string.IsNullOrEmpty(keyword))
            {
                Debug.LogWarning("ContainKeyword: Input string or keyword is null or empty, returning false.");
                return false;
            }
            return self.IndexOf(keyword, comparisonType) >= 0;
        }

        #endregion
    }
}