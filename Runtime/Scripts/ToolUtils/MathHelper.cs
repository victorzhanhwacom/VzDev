using UnityEngine;

namespace VzDev.MathUtils
{
    /// 數學工具
    public static class MathHelper
    {
        /// 是否在範圍值內
        public static bool IsInRange(float value, float min = 0, float max = 1) => (value >= min && value <= max);
        
        /// [字串]轉換成小數點後N位
        public static string ToDotNumberString(float value, int n = 1) =>
            value.ToString((n > 0) ? $"0.{new string('#', n)}" : "F0");

        /// [float值]轉換成小數點後N位
        public static float ToDotNumberFloat(float value, int n = 1) =>
            Mathf.Round(value * Mathf.Pow(10, n)) / Mathf.Pow(10, n);

        /// [float值]轉換成0~1的百分比數值
        public static float ToPercent01(float value, float maxValue = 100, int n = 1) =>
            ToDotNumberFloat(value / maxValue, n);

        /// 以每step為一刻度，計算目標數字於該刻度的上限值
        public static int GetNumberLevelMax(float targetValue, int step = 10)
            => Mathf.CeilToInt(targetValue / (float)step) * step;
    }
}