using System;
using System.Linq;

namespace VzDev.EnumUtils
{
    public static class EnumHelper<T> where T : struct, Enum
    {
        #region 從字串中找出關鍵字對應的 Enum 值   
        private static readonly (string Name, T Value)[] _lookup = BuildLookup();
        private static (string Name, T Value)[] BuildLookup()
        {
            return Enum.GetValues(typeof(T))
                .Cast<T>()
                .Where(v => Convert.ToInt32(v) != 0)
                .Select(v => (v.ToString().ToUpperInvariant(), v))
                .ToArray();
        }
        public static T GetEnumFromString(string input)
        {
            if (string.IsNullOrEmpty(input))
                return default;

            // 關鍵修正：只轉一次大寫，之後迴圈內每次比對都是 Ordinal（快）
            string upperInput = input.ToUpperInvariant();

            foreach (var (name, value) in _lookup)
            {
                if (upperInput.Contains(name, StringComparison.Ordinal))
                    return value;
            }

            return default;
        }
        #endregion
    }
}
