using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using VzDev.DebugUtils;
using VzDev.MathUtils;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace VzDev.ColorUtils
{
    public static class ColorHelper
    {
        private static readonly Color defaultColor = Color.white;

        /// <summary>
        /// 依百分比取得對應的顏色，並在兩個顏色之間進行線性插值
        /// <para>+ percent為0~100之float值</para>
        /// <para>+ Array 和 List 都實作了 IReadOnlyList </para>
        /// </summary>
        public static Color GetColorLerpFromThresholds(float percent, IReadOnlyList<ColorThresholdItem> colorThresholds)
        {
            if (colorThresholds == null || colorThresholds.Count == 0)
            {
                Debug.LogWarning("colorThresholds為空，無法取得顏色，返回預設顏色");
                return defaultColor;
            }
            // 小於最小閾值，返回第一個顏色
            if (percent <= colorThresholds[0].threshold) return colorThresholds[0].color;
            // 大於最大閾值，返回最後一個顏色
            if (percent >= colorThresholds[^1].threshold) return colorThresholds[^1].color;

            // 找出 percent 落在哪一段區間 [lower, upper]
            for (int i = 0; i < colorThresholds.Count - 1; i++)
            {
                ColorThresholdItem lower = colorThresholds[i];
                ColorThresholdItem upper = colorThresholds[i + 1];

                if (lower.threshold <= percent && percent <= upper.threshold)
                {
                    float t = Mathf.InverseLerp(lower.threshold, upper.threshold, percent);
                    return Color.Lerp(lower.color, upper.color, t);
                }
            }
            return defaultColor; // 預設顏色
        }

        /// <summary>
        /// 將字串(16進制)轉成Color
        /// <param>自動偵測補上#</param>
        public static Color StringToColor(string hexString)
        {
            if (string.IsNullOrEmpty(hexString)) return defaultColor;
            hexString = hexString.Trim();
            if (!hexString.StartsWith("#")) hexString = "#" + hexString;
            if (ColorUtility.TryParseHtmlString(hexString, out var color)) return color;
            return defaultColor;
        }

        /// <summary>
        /// 將RGB數值轉成Color (0~255)
        /// </summary>
        public static bool GetColorFromRgba(out Color? color, int r, int g, int b, int a = 255)
            => GetColorFromRgba(out color, r / 255f, g / 255f, b / 255f, a / 255f);

        /// <summary>
        /// 將RGB數值轉成Color (0~1)
        /// </summary>
        public static bool GetColorFromRgba(out Color? color, float r, float g, float b, float a = 1)
        {
            bool isValid = MathHelper.IsInRange(r) && MathHelper.IsInRange(g) && MathHelper.IsInRange(b) && MathHelper.IsInRange(a);
            color = isValid ? new Color(r, g, b, a) : defaultColor;
            if (isValid == false) Debug.LogWarning("RGBA值超出0~1範圍，無法建立Color");
            return isValid;
        }

        /// 將色碼字串轉成Color (#RRGGBB、#RRGGBBAA)
        public static bool GetColorFromHtmlString(out Color? color, string colorCode)
        {
            if (colorCode.StartsWith("#") == false) colorCode = "#" + colorCode;
            bool isSuccess = ColorUtility.TryParseHtmlString(colorCode, out Color result);
            color = isSuccess ? result : null;
            if (isSuccess == false) Debug.LogWarning("colorCode內容有誤，無法建立Color");
            return isSuccess;
        }


        /////////////// 20260828 ///////////////

        /// 依溫度取得相對應顏色(綠→紅)
        public static Color GetTemperatureColor(float temperature, float minValue = 0, float maxValue = 100)
        => GetLevelColor(temperature, minValue, maxValue);

        /// 依濕度取得相對應顏色(黃→藍)
        public static Color GetHumidityColor(float humidity, float minValue = 0, float maxValue = 100)
            => GetLevelColor(humidity, minValue, maxValue, Color.yellow, Color.blue);

        /// 依等級取得相對應顏色
        public static Color GetLevelColor(float value, float minValue = 0, float maxValue = 100, Color? minColor = null, Color? maxColor = null)
        {
            float t = Mathf.InverseLerp(minValue, maxValue, value);
            return Color.Lerp(minColor ?? Color.green, minColor ?? Color.red, t);
        }


        /// OLD========================================================================

        public static Color blue => RgbToColor(128, 255, 255);
        public static Color green => RgbToColor(30, 255, 30);
        public static Color yellow => RgbToColor(255, 255, 30);
        public static Color orange => RgbToColor(255, 180, 30);
        public static Color red => RgbToColor(255, 30, 30);

        /// <summary>
        /// 設定溫度顏色等級 {機房理想溫度 20~27°c}
        /// <para>+ T: 可使用TextMeshProUGUI、Image</para>
        /// </summary>
        public static Tween ChangeColorLevel_Temperature<T>(float value, T target, float duration = 2f)
            where T : Graphic
        {
            // 根據 value 的範圍來決定顏色
            Color targetColor = red;

            List<Tuple<float, Color>> levelColors = new List<Tuple<float, Color>>()
            {
                new Tuple<float, Color>(20f, blue),
                new Tuple<float, Color>(27f, yellow),
                new Tuple<float, Color>(30f, orange),
                new Tuple<float, Color>(40f, red),
            };
            for (int i = 0; i < levelColors.Count; i++)
            {
                //每組顏色Threshold比對
                if (value <= levelColors[i].Item1)
                {
                    // 小於最低門檻值
                    if (i == 0) targetColor = levelColors[0].Item2;
                    else
                    {
                        Tuple<float, Color> before = levelColors[i - 1];
                        Tuple<float, Color> after = levelColors[i];

                        float t = Mathf.InverseLerp(before.Item1, before.Item1, value);
                        targetColor = Color.Lerp(before.Item2, after.Item2, t);
                    }

                    break;
                }
            }

            return target.DOColor(targetColor, duration);
        }

        /// <summary>
        /// 依百分比取得各等級Color
        /// <para>+ percentaget為0~1之float值</para>
        /// <para>+ 設置各等級{Threshold值0~1, Color}</para>
        /// </summary>
        public static Color GetColorLevelFromPercentage(float percentage01,
            List<Tuple<float, Color>> levelColors = null)
        {
            // 确保百分比在0到1之间
            percentage01 = Mathf.Clamp01(percentage01);

            if (levelColors == null)
                levelColors = new List<Tuple<float, Color>>()
                {
                    new Tuple<float, Color>(0.1f, green),
                    new Tuple<float, Color>(0.3f, yellow),
                    new Tuple<float, Color>(0.5f, orange),
                    new Tuple<float, Color>(1f, red),
                };
            for (int i = 0; i < levelColors.Count; i++)
            {
                if (percentage01 <= levelColors[i].Item1)
                {
                    if (i == 0) return levelColors[0].Item2;
                    else
                    {
                        Tuple<float, Color> before = levelColors[i - 1];
                        Tuple<float, Color> after = levelColors[i];
                        return Color.Lerp(before.Item2, after.Item2,
                            (percentage01 - before.Item1) / (after.Item1 - before.Item1));
                    }
                }
            }

            return Color.white;
        }


        /// <summary>
        /// 依百分比取得Color
        /// <para>+ percentaget為0~1之float值</para>
        /// <para>+ 顏色從綠色到紅色</para>
        /// </summary>
        public static Color GetColorFromPercentage_OLD(float percentage, Color? colorStart = null,
            Color? colorEnd = null)
        {
            /*   if(colorStart == null) colorStart = green;
               if(colorEnd == null) colorEnd = red;*/

            // 确保百分比在0到1之间
            percentage = Mathf.Clamp01(percentage);
            // 使用Color.Lerp进行线性插值
            return Color.Lerp((Color)colorStart, (Color)colorEnd, percentage);
        }

        public static Color RgbToColor(float r, float g, float b)
        {
            return new Color(r / 255f, g / 255f, b / 255f);
        }

        /// <summary>
        /// 將Hex十六進制(0xFFFFFF)轉成Color
        /// <para>+ int hex = 0xFFFFFF</para>
        /// </summary>
        public static Color HexToColor(int hexColor, float alpha = 1f)
        {
            // 將十六進制顏色值轉換為 Color（除以255.0f以正確縮放到0到1之間）
            float r = ((hexColor >> 16) & 0xFF) / 255.0f;
            float g = ((hexColor >> 8) & 0xFF) / 255.0f;
            float b = (hexColor & 0xFF) / 255.0f;
            return new Color(r, g, b, alpha); // Alpha 設為 1.0，表示完全不透明
        }


        /// 顏色等級設定(使用率)
        public static readonly List<ColorLevel> UsageColorLevels = new()
        {
            new ColorLevel(0.4f, Color.green),
            new ColorLevel(0.6f, Color.yellow),
            new ColorLevel(0.8f, new Color(1f, 0.647f, 0f)), // 橙色
            new ColorLevel(1f, Color.red),
        };

        /// 顏色等級設定(剩餘率)
        public static readonly List<ColorLevel> RemainColorLevels = new()
        {
            new ColorLevel(0.4f, Color.red),
            new ColorLevel(0.6f, new Color(1f, 0.647f, 0f)), // 橙色
            new ColorLevel(0.8f, Color.yellow),
            new ColorLevel(1f, Color.green),
        };

        public static Color GetColorFromPercentage(float percent01) =>
            GetColorFromPercentage(percent01, UsageColorLevels);

        public static Color GetColorFromPercentage(float percent01, List<ColorLevel> colorLevels)
        {
            if (colorLevels == null || colorLevels.Count == 0) colorLevels = UsageColorLevels;
            //從小到大的排序，方便LINQ依序比對
            List<ColorLevel> sortedColorLevels = colorLevels.OrderBy(colorLevel => colorLevel.percent01).ToList();
            ColorLevel result = sortedColorLevels.Where(colorLevel => percent01 <= colorLevel.percent01)?.FirstOrDefault();
            return result?.color ?? colorLevels.Last().color;
        }

        public static Tween ToBlink<T>(T target, Color color1, Color color2, float duration = 1f,
            Ease ease = Ease.InOutQuad) where T : Graphic
        {
            target.DOKill();
            target.color = color1;
            // 設置 Image 的顏色在 color1 和 color2 之間循環變化
            return target.DOColor(color2, duration)
                .SetLoops(-1, LoopType.Yoyo) // 無限循環，Yoyo 模式（往返）
                .SetEase(ease); // 平滑的過渡效果
        }

        /// 顏色闕值設定
        [Serializable]
        public class ColorLevel
        {
            [Header("[闕值]")][Range(0, 1)] public float percent01;
            public Color color;

            public ColorLevel(float percent01, Color color)
            {
                this.percent01 = percent01;
                this.color = color;
            }
        }

        /// 從HDR Color裡取得Intensity
        public static float GetIntensity(Color color) => Mathf.Max(color.r, color.g, color.b);

        /// 改變材質BaseColor的Alpha值
        public static void ChangeAlpha(Material targetMat, float alpha)
        {
            Color baseColor = targetMat.GetColor("_BaseColor");
            baseColor.a = Mathf.Clamp01(alpha); // 半透明
            targetMat.SetColor("_BaseColor", baseColor);
        }
    }
}