using System.Collections.Generic;
using XCharts.Runtime;

namespace VzDev.ApiExtensions.XChart
{
    public static class LineChartExtension
    {
        public static void ToAnimate(this LineChart lineChart)
        {
            lineChart.AnimationReset();
            lineChart.AnimationFadeIn();
        }


        /// <summary>
        /// 設置LineChart資料
        /// </summary>
        public static void SetData(this LineChart lineChart, Dictionary<int, float> data)
        {
            List<SerieData> serieData = lineChart.series[0].data;
            serieData.Clear();
            foreach (var item in data)
            {
                lineChart.AddData(item.Key, item.Value);
            }
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
    }
}