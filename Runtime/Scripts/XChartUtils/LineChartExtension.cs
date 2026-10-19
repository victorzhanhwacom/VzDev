using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using VzDev.ApiExtensions;
using XCharts.Runtime;

namespace VzDev.XChartUtils.APIExtensions
{
    public static class LineChartExtension
    {
        private static int X_SPLITER_MAX = 5;

        /// <summary>
        /// 強制刷新圖表，並播放淡入動畫。
        /// </summary>
        public static void ToAnimate(this LineChart lineChart)
        {
            lineChart.AnimationReset();
            lineChart.AnimationFadeIn();
            lineChart.RefreshChart();
        }

        /// <summary>
        /// 依名稱取得既有 Serie；找不到時才新增。
        /// 可保留在 Inspector 預先設定的樣式。
        /// </summary>
        public static T EnsureSerie<T>(this BaseChart chart, string serieName) where T : Serie
        {
            var serie = chart.GetSerie(serieName) as T;
            if (serie == null)
                serie = chart.AddSerie<T>(serieName);
            return serie;
        }

        #region 設置LineChart資料
        /// <summary>加入單筆時間資料</summary>
        public static SerieData AddTimeData(this Serie serie, DateTime time, double value)
        {
            return serie.AddXYData(ToAxisTime(time), value);
        }

        /// <summary>一次設定整批時間資料（會先清除舊資料、依時間排序）</summary>
        public static void SetTimeData<T>(this Serie serie, IEnumerable<T> records,
            Func<T, DateTime> timeSelector, Func<T, double> valueSelector)
        {
            serie.ClearData();
            foreach (var r in records.OrderBy(timeSelector))
                serie.AddXYData(ToAxisTime(timeSelector(r)), valueSelector(r));
        }
        #endregion

        #region 設定X軸為時間格式
        /// <summary>
        /// 設定X軸為時間格式，X軸的label以data的time為顯示
        /// </summary>
        public static void SetXAxisToTimeFormat(this LineChart lineChart
        , DateTime dateTimeStart, DateTime dateTimeEnd, int splitNumber = 0)
        {
            if (dateTimeEnd <= dateTimeStart)
            {
                Debug.LogError($"[SetXAxisToTimeFormat] dateTimeEnd 必須晚於 dateTimeStart");
                return;
            }

            var taiwanOffset = TimeSpan.FromHours(8);
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);

            // XCharts 顯示標籤時自行加上的時差（Editor 通常 +8，WebGL 可能是 0），計算需補償的秒數
            var shownAtZero = DateTime.SpecifyKind(DateTimeUtil.GetDateTime(0), DateTimeKind.Unspecified);
            double shift = taiwanOffset.TotalSeconds - (shownAtZero - epoch).TotalSeconds;

            // Utc 視為 UTC；Local / Unspecified 一律視為台灣時間
            double ToAxisTime(DateTime t)
            {
                var dto = t.Kind == DateTimeKind.Utc
                    ? new DateTimeOffset(t)
                    : new DateTimeOffset(DateTime.SpecifyKind(t, DateTimeKind.Unspecified), taiwanOffset);
                return dto.ToUnixTimeSeconds() + shift;
            }

            var xAxis = lineChart.EnsureChartComponent<XAxis>();
            xAxis.type = Axis.AxisType.Time;
            xAxis.boundaryGap = false;
            xAxis.minMaxType = Axis.AxisMinMaxType.Custom;
            xAxis.min = ToAxisTime(dateTimeStart);
            xAxis.max = ToAxisTime(dateTimeEnd);

            double span = (dateTimeEnd - dateTimeStart).TotalSeconds;
            double interval;

            if (splitNumber > 0)
            {
                xAxis.interval = 0;              // 必須歸零，splitNumber 才會生效
                xAxis.splitNumber = Mathf.Min(X_SPLITER_MAX, splitNumber);
                interval = span / splitNumber;   // 只用來決定標籤格式
            }
            else
            {
                double[] niceIntervals = { 1, 5, 10, 15, 30, 60, 120, 300, 600, 900, 1800,
                                   3600, 7200, 10800, 21600, 43200, 86400, 172800, 604800 };
                interval = niceIntervals[niceIntervals.Length - 1];
                foreach (var v in niceIntervals)
                    if (v >= span / 6) { interval = v; break; }
                xAxis.interval = interval;
            }

            xAxis.axisLabel.show = true;
            xAxis.axisLabel.numericFormatter =
                span > 86400 ? "MM/dd HH:mm" :
                interval < 60 ? "HH:mm:ss" : "HH:mm";

            xAxis.SetAxisLabelFont(Color.white);
            lineChart.RefreshChart();
        }

        #region 設定軸向的文字樣式
        /// <summary>
        /// 設定軸向的文字樣式
        /// </summary>
        public static void SetAxisLabelFont(this BaseChart chart, Color? color = null, int fontSize = 14)
        {
            chart.EnsureChartComponent<XAxis>().SetAxisLabelFont(color, fontSize);
            chart.EnsureChartComponent<YAxis>().SetAxisLabelFont(color, fontSize);
        }
        /// <summary>
        /// 設定軸向的文字樣式
        /// </summary>
        public static void SetAxisLabelFont(this Axis axis, Color? color = null, int fontSize = 14)
        {
            axis.axisLabel.textStyle.fontSize = fontSize;
            if (color.HasValue)
            {
                axis.axisLabel.textStyle.color = color.Value;
            }
        }
        #endregion


        // 軸與資料共用的換算（Utc 視為 UTC；Local / Unspecified 視為台灣時間）
        private static double ToAxisTime(DateTime t)
        {
            var taiwanOffset = TimeSpan.FromHours(8);
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);
            var shownAtZero = DateTime.SpecifyKind(DateTimeUtil.GetDateTime(0), DateTimeKind.Unspecified);
            double shift = taiwanOffset.TotalSeconds - (shownAtZero - epoch).TotalSeconds;

            var dto = t.Kind == DateTimeKind.Utc
                ? new DateTimeOffset(t)
                : new DateTimeOffset(DateTime.SpecifyKind(t, DateTimeKind.Unspecified), taiwanOffset);
            return dto.ToUnixTimeSeconds() + shift;
        }
        #endregion

        #region 設定Y軸為溫度格式 / 濕度格式
        /// <summary>
        /// 設置Y軸為溫度格式
        /// </summary>
        public static void SetYAxisToTemperatureFormat(this LineChart lineChart, float min = 0, float max = 40)
            => SetYAxisToCustomFormat(lineChart, min, max, "{value}°c", "溫度 {c:F1}°c");

        /// <summary>
        /// 設置Y軸為濕度格式
        /// </summary>
        public static void SetYAxisToHumidityFormat(this LineChart lineChart, float min = 0, float max = 100)
            => SetYAxisToCustomFormat(lineChart, min, max, "{value}%", "濕度 {c:F0}%");

        /// <summary>
        /// 設定Y軸為自訂格式
        /// </summary>
        public static void SetYAxisToCustomFormat(this LineChart lineChart, float min, float max, string formatter, string tooltipFormatter = null)
        {
            var yAxis = lineChart.EnsureChartComponent<YAxis>();
            yAxis.min = min; yAxis.max = max;
            yAxis.minMaxType = Axis.AxisMinMaxType.Custom;
            yAxis.axisLabel.formatter = formatter;
            yAxis.SetAxisLabelFont(Color.white);

            lineChart.SetTooltipStyle(tooltipFormatter, titelColor: Color.black, textColor: Color.black);
            lineChart.RefreshChart();
        }
        #endregion

        /// <summary>
        /// 設定Tooltip樣式
        /// </summary>
        public static Tooltip SetTooltipStyle(this BaseChart chart, string tooltipFormatter = null
        , Color? backgroundColor = null, Color? titelColor = null, Color? textColor = null, int? fontSize = null)
        {
            var tooltip = chart.EnsureChartComponent<Tooltip>();
            tooltip.show = true;
            if (string.IsNullOrEmpty(tooltipFormatter) == false) tooltip.itemFormatter = tooltipFormatter;
            if (backgroundColor.HasValue) tooltip.backgroundColor = backgroundColor.Value;
            if (titelColor.HasValue) tooltip.titleLabelStyle.textStyle.color = titelColor.Value;
            if (textColor.HasValue) tooltip.contentLabelStyles[0].textStyle.color = textColor.Value;
            if (fontSize.HasValue) tooltip.titleLabelStyle.textStyle.fontSize = fontSize.Value;

            tooltip.titleHeight = tooltip.itemHeight = 15;
            tooltip.paddingLeftRight = tooltip.paddingTopBottom = 5;
            return tooltip;
        }


        #region 設定軸
        #region 設定X軸
        public static void SetXAxisMinMax(this LineChart lineChart, float min, float max)
            => SetAxisMinMax<XAxis>(lineChart, min, max);
        public static void SetXAxisLabelFormatter(this LineChart lineChart, string prefix = "", string suffix = "")
            => SetAxisLabelFormatter<XAxis>(lineChart, prefix, suffix);
        public static void SetXAxisSplitNumber(this LineChart lineChart, int splitNumber)
            => SetAxisSplitNumber<XAxis>(lineChart, splitNumber);

        #endregion

        #region 設定Y軸
        public static void SetYAxisMinMax(this LineChart lineChart, float min, float max)
            => SetAxisMinMax<YAxis>(lineChart, min, max);
        public static void SetYAxisLabelFormatter(this LineChart lineChart, string prefix = "", string suffix = "")
            => SetAxisLabelFormatter<YAxis>(lineChart, prefix, suffix);
        public static void SetYAxisSplitNumber(this LineChart lineChart, int splitNumber)
            => SetAxisSplitNumber<YAxis>(lineChart, splitNumber);
        #endregion

        private static void SetAxisMinMax<T>(this LineChart lineChart, float min, float max) where T : Axis
        {
            T axis = lineChart.EnsureChartComponent<T>();
            axis.minMaxType = Axis.AxisMinMaxType.Custom;
            axis.min = min;
            axis.max = max;
        }
        private static void SetAxisLabelFormatter<T>(this LineChart lineChart, string prefix = "", string suffix = "") where T : Axis
        {
            T axis = lineChart.EnsureChartComponent<T>();
            axis.axisLabel.formatter = $"{prefix}{{value}}{suffix}";
        }
        private static void SetAxisSplitNumber<T>(this LineChart lineChart, int splitNumber) where T : Axis
        {
            T axis = lineChart.EnsureChartComponent<T>();
            axis.splitNumber = splitNumber;
        }
        #endregion

        /// <summary>
        /// 透明背景、圖表撐滿容器設定邊界，適合疊在 3D 場景上
        /// </summary>
        public static BaseChart ApplyChartLayout(this BaseChart chart)
        {
            chart.theme.transparentBackground = true;
            var grid = chart.EnsureChartComponent<GridCoord>();
            grid.left = 0.08f;
            grid.right = 0.05f;
            grid.top = 0.1f;     // 小於 1 視為比例，留一點空間給頂端的點
            grid.bottom = 0.07f;

            chart.EnsureChartComponent<Title>().show = false;
            chart.SetTooltipStyle(titelColor: Color.black, textColor: Color.black);
            return chart;
        }

        /// <summary>
        /// 平滑線 + 實心圓點 + 半透明面積（漸層可選）
        /// </summary>
        public static Serie SetLineColorStyle(this BaseChart chart, Color color,
            float lineWidth = 1f, float symbolSize = 3f, int serieIndex = 0)
        {

            chart.EnsureChartComponent<VisualMap>().show = false; // 關閉 VisualMap，避免覆蓋掉自訂的顏色

            Serie serie = chart.series[serieIndex];
            // 線
            serie.lineType = LineType.Smooth;
            serie.lineStyle.show = true;
            serie.lineStyle.color = color;
            serie.lineStyle.width = lineWidth;

            // 資料點
            serie.symbol.show = true;
            serie.symbol.type = SymbolType.Circle;
            serie.symbol.size = symbolSize;
            serie.itemStyle.color = color;
            serie.itemStyle.borderWidth = 0;

            // 面積填色
            var area = serie.EnsureComponent<AreaStyle>();
            area.show = true;
            area.toTop = true;
            area.opacity = 0.1f;

            area.color = new Color(0, 0, 0, 1f);
            area.toColor = new Color(color.r, color.g, color.b, 1f);
            return serie;
        }

        /// <summary>
        /// 依多段等級設定 VisualMap 顏色。
        /// levels 的 key 為該區段下限，下一筆的 key 為上限；最後一段延伸到 dataMax。
        /// </summary>
        public static VisualMap SetLevelColors(this BaseChart chart, IList<KeyValueData<float, Color32>> levels,
            double dataMax, int serieIndex = 0, bool workOnArea = true)
        {
            if (levels == null || levels.Count == 0) return null;

            var sorted = levels.OrderBy(l => l.Key).ToList();
            double dataMin = sorted[0].Key;
            if (dataMax <= sorted[sorted.Count - 1].Key)
                dataMax = sorted[sorted.Count - 1].Key + 1; // 確保最後一段有範圍

            var visualMap = chart.EnsureChartComponent<VisualMap>();
            visualMap.show = true;
            visualMap.showUI = false;
            visualMap.type = VisualMap.Type.Piecewise;
            visualMap.serieIndex = serieIndex;
            visualMap.dimension = 1;   // Y 值
            visualMap.min = dataMin;
            visualMap.max = dataMax;
            visualMap.workOnLine = true;
            visualMap.workOnArea = workOnArea;

            visualMap.inRange.Clear();
            for (int i = 0; i < sorted.Count; i++)
            {
                double min = sorted[i].Key;
                double max = (i + 1 < sorted.Count) ? sorted[i + 1].Key : dataMax;
                visualMap.inRange.Add(new VisualMapRange
                {
                    min = min,
                    max = max,
                    color = sorted[i].Value
                });
            }
            chart.SetSerieAreaStyle(new Color(255, 255, 255, 0), new Color(255, 255, 255, 255));
            // 面積填色
            var area = chart.series[serieIndex].EnsureComponent<AreaStyle>();
            area.show = true;
            area.toTop = true;
            area.opacity = 0.3f;

            chart.RefreshChart();
            return visualMap;
        }

        /// <summary> 設定 Serie 的面積樣式（AreaStyle）</summary>        
        public static void SetSerieAreaStyle(this BaseChart chart, Color color, Color toColor, int serieIndex = 0)
        {
            var areaStyle = chart.series[serieIndex].EnsureComponent<AreaStyle>();
            areaStyle.color = color;
            areaStyle.toColor = toColor;
        }

        /// <summary>動態調整門檻（用於即時資料）</summary>
        public static void UpdateThreshold(this VisualMap visualMap, double threshold)
        {
            if (visualMap.inRange.Count < 2) return;
            visualMap.inRange[0].max = threshold;
            visualMap.inRange[1].min = threshold;
        }
    }
}