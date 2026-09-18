using UnityEngine;

namespace VzDev.UnityAPI.Extensions
{
    public static class FloatExtension
    {

        /// <summary>
        /// Mathf.Pow(10, n)的效能比單純乘法還要差很多，所以直接用陣列存好10的次方數，避免每次都要計算
        /// </summary>
        private static readonly float[] Pow10 ={
            1f, 10f, 100f, 1000f, 10000f, 100000f,
            1000000f, 10000000f, 100000000f, 1000000000f
        };

        /// <summary>
        /// 將浮點數四捨五入到指定的小數位數
        /// </summary>
        public static float RoundToDecimals(this float value, int decimalPlaces = 1)
        {
            decimalPlaces = Mathf.Clamp(decimalPlaces, 0, Pow10.Length - 1);
            float multiplier = Pow10[decimalPlaces];
            return Mathf.Round(value * multiplier) / multiplier;
        }

        /// <summary>
        /// 將浮點數轉換為指定小數位數的字串，並去除尾端多餘的零
        /// <para>+ decimalPlaces: 小數位數</para>
        /// <para>+ haveSeparator: 是否要加上千分位分隔符號</para>
        /// </summary>
        public static string ToTrimZeroString(this float value, int decimalPlaces = 1, bool haveSeparator = false)
        {
            string format;
            if (haveSeparator)
            {
                format = decimalPlaces <= 0
                ? "#,##0"
                : "#,##0." + new string('#', decimalPlaces);
            }
            else
            {
                format = decimalPlaces <= 0 ? "0" : "0." + new string('#', decimalPlaces);
            }
            return value.ToString(format, System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}