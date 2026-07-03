using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace VzDev.UnityAPI.Extensions
{
    public static class StringExtension
    {
        /// <summary>
        /// 嘗試將字串格式化為 JSON 格式，若成功則回傳 true 並輸出格式化後的字串，否則回傳 false
        /// </summary>
        /// <returns></returns>
        public static bool TryToJsonFormat(this string self, out string formattedJson)
        {
            formattedJson = self;
            if (string.IsNullOrEmpty(self))
            {
                Debug.LogWarning($"[StringExtension] Input string is null or empty!");
                return false;
            }
            try
            {
                formattedJson = JToken.Parse(self).ToString(Formatting.Indented);
                return true;
            }
            catch (JsonReaderException)
            {
                Debug.LogWarning($"[StringExtension] Input string is not a valid JSON format!");
                return false; // 非合法 JSON，回傳 false
            }
        }

        /// <summary>
        /// 將 JSON 字串格式化為易讀的格式（縮排、換行）
        /// </summary>
        public static string ToJsonFormat(this string self)
        {
            if (string.IsNullOrEmpty(self))
            {
                Debug.LogWarning("Input string is null or empty!");
                return self;
            }
            try
            {
                 return JToken.Parse(self).ToString(Formatting.Indented);
            }
            catch (JsonReaderException)
            {
                Debug.LogWarning("Input string is not a valid JSON format!");
                return self; // 非合法 JSON，原樣回傳
            }
        }



        /////////////////////////////////////////////////



        /// [Extended] - 只取中文字
        public static string GetChineseWordsOnly(this string self)
        {
            // 使用正規表示式匹配所有中文字範圍 [\u4e00-\u9fa5]
            // MatchCollection 會抓出所有符合的字元
            MatchCollection matches = Regex.Matches(self, @"[\u4e00-\u9fa5]");
            string result = "";
            foreach (Match match in matches)
            {
                result += match.Value;
            }
            return result;
        }

        /// [Extended] - 尋找字串裡是否有關鍵單字
        public static bool ContainKeyword(this string self, StringComparison comparison, params string[] keywords)
        {
            if (string.IsNullOrEmpty(self) || keywords == null || keywords.Length == 0)
                return false;

            foreach (var key in keywords)
            {
                if (string.IsNullOrWhiteSpace(key))
                    continue;

                // 判斷是否是 ASCII 範圍內的英數字（含 A-Z, a-z, 0-9）
                bool isAsciiWord = key.All(c => c <= 127 && char.IsLetterOrDigit(c));

                if (isAsciiWord)
                {
                    // RegexOptions：是否大小寫不分
                    RegexOptions opts = RegexOptions.None;
                    if (comparison == StringComparison.OrdinalIgnoreCase ||
                        comparison == StringComparison.CurrentCultureIgnoreCase ||
                        comparison == StringComparison.InvariantCultureIgnoreCase)
                    {
                        opts |= RegexOptions.IgnoreCase;
                    }

                    string pattern;

                    // ① 若是「純數字」→ 使用數字邊界，不允許前後是數字
                    if (key.All(char.IsDigit))
                    {
                        pattern = $@"(?<![0-9]){Regex.Escape(key)}(?![0-9])";
                    }
                    else
                    {
                        // ② 若是英數混合 → 使用英數字邊界，不允許前後是 A-Z a-z 0-9
                        pattern = $@"(?<![A-Za-z0-9]){Regex.Escape(key)}(?![A-Za-z0-9])";
                    }

                    if (Regex.IsMatch(self, pattern, opts))
                        return true;
                }
                else
                {
                    // 中文、全形、或混合符號：用 IndexOf 即可
                    if (self.IndexOf(key, comparison) >= 0)
                        return true;
                }
            }

            return false;
        }

        /// [Extended] - 取出字串裡的所有數字，並向左補位數
        /// <para>+ maxLength：最多幾位數，若-1則不限制 </para>
        public static string GetNumberFromString(this string self, int padLeft = 0, int maxLength = -1)
        {
            StringBuilder sb = new System.Text.StringBuilder();
            // 抓全部數字
            for (var i = 0; i < self.Length; i++)
            {
                var c = self[i];
                if (char.IsDigit(c))
                    sb.Append(c);
            }
            string digits = sb.ToString();

            // 補左邊 0（如果 padLeft > 0）
            if (padLeft > 0)
                digits = digits.PadLeft(padLeft, '0');
            // 限制最大長度（可選）
            if (maxLength > 0 && digits.Length > maxLength)
                digits = digits.Substring(digits.Length - maxLength);
            return digits;
        }

        /// [Extended] - 依換行符號，分割成數行string
        public static string[] SplitToLines(this string self, params char[] delimiter)
        {
            if (delimiter == null || delimiter.Length == 0) delimiter = new char[] { '\r', '\n' };
            return self.Split(delimiter, StringSplitOptions.RemoveEmptyEntries);
        }

        /// [Extended] - 首英文字大寫
        public static string ToCapitalizeFirstLetter(this string str)
        {
            if (string.IsNullOrEmpty(str)) return str;
            return char.ToUpper(str[0]) + str.Substring(1);
        }

        /// [Extended] - 是否為JSON字串格式
        public static bool IsJsonFormat(this string self)
        {
            if (string.IsNullOrWhiteSpace(self)) return false;
            try
            {
                JToken.Parse(self);
                return true;
            }
            catch
            {
                return false;
            }
        }



        /// [Extended] - 轉成指定的Enum
        public static T ToEnum<T>(this string self) where T : struct
            => Enum.Parse<T>(self.Trim(), true);

        /// 移除開頭的HTTP方法
        public static string RemoveHttpTypeOnHeader(this string self) =>
            self.Trim().RemoveKeyWord("http://", EnumKeyWordPosition.StartWith)
                .RemoveKeyWord("https://", EnumKeyWordPosition.StartWith);

        /// 移除關鍵字 (任一處 / 開頭 / 結尾)
        public static string RemoveKeyWord(this string self, string keyword, EnumKeyWordPosition keyWordPosition = EnumKeyWordPosition.Any)
        {
            if (string.IsNullOrEmpty(keyword))
            {
                Debug.LogWarning("keyword is null or empty!");
                return self;
            }
            switch (keyWordPosition)
            {
                default:
                case EnumKeyWordPosition.Any:
                    return self.Replace(keyword, string.Empty);
                case EnumKeyWordPosition.StartWith:
                    return self.StartsWith(keyword) ? self.Substring(keyword.Length) : self;
                case EnumKeyWordPosition.EndWith:
                    return self.EndsWith(keyword) ? self.Substring(0, self.Length - keyword.Length) : self;
            }
        }


        /// 將含有https / http的字串，轉成有link標籤的超鏈結
        /// <para>+ 若不包含https/http的話，直接回傳原string</para>
        public static string ToUrlLinkHtmlTag(this string self)
        {
            if (string.IsNullOrEmpty(self))
                return self;
            // 改良版 Regex：可匹配 http/https，含參數、子網域、路徑
            string pattern = @"(https?:\/\/[^\s]+)";
            return Regex.Replace(self, pattern, match =>
            {
                string url = match.Value;
                // 加上正確引號的 TMP 超連結格式
                return $"<link=\"{url}\"><color=#2986cc><u>{url}</u></color></link>";
            });
        }

        /// 關鍵字位置
        public enum EnumKeyWordPosition
        {
            Any, StartWith, EndWith
        }
    }
}