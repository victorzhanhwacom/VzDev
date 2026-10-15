using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using XCharts.Runtime;

namespace VzDev.ApiExtensions.XChart
{
    public static class LineChartExtension
    {
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
                xAxis.splitNumber = Mathf.Min(10, splitNumber);
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

            lineChart.RefreshChart();
        }

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
            => SetYAxisToCustomFormat(lineChart, min, max, "{value}°C");

        /// <summary>
        /// 設置Y軸為濕度格式
        /// </summary>
        public static void SetYAxisToHumidityFormat(this LineChart lineChart, float min = 0, float max = 100)
            => SetYAxisToCustomFormat(lineChart, min, max, "{value}%");

        /// <summary>
        /// 設定Y軸為自訂格式
        /// </summary>
        public static void SetYAxisToCustomFormat(this LineChart lineChart, float min, float max, string formatter)
        {
            var yAxis = lineChart.EnsureChartComponent<YAxis>();
            yAxis.min = min; yAxis.max = max;
            yAxis.minMaxType = Axis.AxisMinMaxType.Custom;
            yAxis.axisLabel.formatter = formatter;
        }
        #endregion

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
        /// 透明背景、圖表撐滿容器，適合疊在 3D 場景上
        /// </summary>
        public static BaseChart ApplyOverlayLayout(this BaseChart chart)
        {
            chart.theme.transparentBackground = true;

            var grid = chart.EnsureChartComponent<GridCoord>();
            grid.left = 0;
            grid.right = 0;
            grid.top = 0.1f;     // 小於 1 視為比例，留一點空間給頂端的點
            grid.bottom = 0;
            return chart;
        }

        /// <summary>
        /// 平滑線 + 實心圓點 + 半透明面積（漸層可選）
        /// </summary>
        public static T ApplyGlowLineStyle<T>(this T serie, Color color,
            float lineWidth = 2f, float symbolSize = 6f,
            float areaAlpha = 0.25f, bool areaGradient = false) where T : Serie
        {
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
            area.color = new Color(color.r, color.g, color.b, areaAlpha);
            // 漸層：往下淡出到全透明
            area.toColor = areaGradient
                ? new Color(color.r, color.g, color.b, 0f)
                : area.color;

            return serie;
        }

        /// <summary>
        /// 極簡座標軸：隱藏軸線、刻度、標籤，可選擇保留分隔線
        /// </summary>
        public static Axis ApplyMinimalStyle(this Axis axis, bool showSplitLine, Color splitLineColor)
        {
            axis.axisLine.show = false;
            axis.axisTick.show = false;
            axis.axisLabel.show = false;

            axis.splitLine.show = showSplitLine;
            axis.splitLine.lineStyle.color = splitLineColor;
            axis.splitLine.lineStyle.width = 1f;
            return axis;
        }
    }
}