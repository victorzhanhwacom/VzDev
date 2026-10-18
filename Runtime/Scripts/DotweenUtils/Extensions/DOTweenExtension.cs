using DG.Tweening;
using TMPro;
using UnityEngine;

namespace VzDev.DOTweenUtils.Extensions
{
    public static class DOTweenExtension
    {
        #region TextMeshProUGUI 數字跳動動畫
        // 注意：TMP SetText 的 {0:N} 中 N = 小數位數，不是 .NET 標準格式字串
        private static readonly string[] DecimalFormats =
        {
            "{0:0}", "{0:1}", "{0:2}", "{0:3}", "{0:4}", "{0:5}"
        };
        private static readonly double[] Pow10 = { 1, 10, 100, 1000, 10000, 100000 };
        /// <summary>數字從 from 變化到 to（每幀 0 GC）</summary>
        /// <param name="decimals">最多小數位數，範圍 0～5</param>
        /// <param name="suffix">後綴文字，例如 " kW"。不可包含 { 或 }</param>
        /// <param name="trimZeros">是否去掉小數尾端的 0（19.0 → 19、19.50 → 19.5）</param>
        public static Tweener DONumber(this TMP_Text target, float from, float to, float duration = 0.5f,
            int decimals = 0, string suffix = null, bool trimZeros = false)
        {
            decimals = Mathf.Clamp(decimals, 0, DecimalFormats.Length - 1);

            // 開始時建好 0～decimals 位的格式，動畫中只挑選、不產生字串
            string[] formats = new string[decimals + 1];
            for (int i = 0; i <= decimals; i++)
                formats[i] = DecimalFormats[i] + suffix;

            float current = from;
            target.SetText(formats[trimZeros ? GetDisplayDecimals(current, decimals) : decimals], current);

            return DOTween.To(() => current, x =>
                {
                    current = x;
                    target.SetText(formats[trimZeros ? GetDisplayDecimals(x, decimals) : decimals], x);
                }, to, duration)
                .SetTarget(target)
                .SetLink(target.gameObject);   // GameObject 銷毀時自動 Kill
        }

        /// <summary>四捨五入到 maxDecimals 位後，實際需要的小數位數</summary>
        private static int GetDisplayDecimals(float value, int maxDecimals)
        {
            long scaled = (long)System.Math.Round(System.Math.Abs((double)value) * Pow10[maxDecimals],
                System.MidpointRounding.AwayFromZero);

            int d = maxDecimals;
            while (d > 0 && scaled % 10 == 0)
            {
                scaled /= 10;
                d--;
            }
            return d;
        }
        #endregion
    }
}