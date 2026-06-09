using UnityEngine;

namespace VzDev.UnityAPI.Extensions
{
    public static class FloatExtension
    {
        /// <summary>
        /// 將浮點數四捨五入到指定的小數位數
        /// </summary>
        public static float RoundToDecimals(this float value, int decimalPlaces = 2)
        {
            decimalPlaces = Mathf.Max(0, decimalPlaces); // Ensure non-negative
            float multiplier = Mathf.Pow(10f, decimalPlaces);
            return Mathf.Round(value * multiplier) / multiplier;
        }
    }
}