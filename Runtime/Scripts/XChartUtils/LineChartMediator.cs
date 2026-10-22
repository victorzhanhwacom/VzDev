using System;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.UIElements;
using VzDev.ApiExtensions;
using VzDev.ColorUtils;
using VzDev.XChartUtils.APIExtensions;
using XCharts.Runtime;

namespace VzDev.XChartUtils
{
    /// <summary>
    /// XChart LineChart 的中介者，負責接收資料並更新圖表。
    /// </summary>
    public class LineChartMediator : MonoBehaviour
    {
        #region Fields
        [ReadOnly, SerializeField] private List<XChartData> data;
        [Foldout("[Color Settings]"), SerializeField, OnValueChanged("UpdateWithSetting")] private bool isColorByLevel = true;
        [Foldout("[Color Settings]"), SerializeField, OnValueChanged("UpdateWithSetting"), ShowIf("isColorByLevel")] private List<KeyValueData<float, Color32>> lineLevelColor;
        [Foldout("[Color Settings]"), SerializeField, OnValueChanged("UpdateWithSetting"), HideIf("isColorByLevel")] private Color lineColor;
        [Foldout("[Settings]"), SerializeField, OnValueChanged("UpdateWithSetting")] private EnumYAxisType dataType = EnumYAxisType.Temperature;
        [Foldout("[Settings]"), SerializeField, OnValueChanged("UpdateWithSetting")] private Vector2 yAxisRange = new Vector2(0, 100);
        [Foldout("[Settings]"), SerializeField, OnValueChanged("UpdateWithSetting")] private int ySplitNumber = 2;
        [Foldout("[Settings]"), SerializeField, OnValueChanged("UpdateWithSetting")] private string serieDataName = "SerieData";
        [Foldout("[Components]"), SerializeField] private LineChart lineChart = null;
        private void OnValidate()
        {
            lineChart ??= GetComponent<LineChart>();
            // 若lineColor未設定，則使用ColorConfig.CyanColor作為預設值
            lineColor = lineColor == default ? ColorConfig.CyanColor : lineColor;

             lineLevelColor ??= new List<KeyValueData<float, Color32>>()
             {
                 new KeyValueData<float, Color32>(0f, ColorConfig.AlertColor),
                 new KeyValueData<float, Color32>(15f, ColorConfig.WarningColor),
                 new KeyValueData<float, Color32>(18f, ColorConfig.NormalColor),
                 new KeyValueData<float, Color32>(26f, ColorConfig.WarningColor),
                 new KeyValueData<float, Color32>(28f, ColorConfig.AlertColor)
             };
        }
        #endregion

        private void Awake() => UpdateWithSetting();

        [Button]
        private void UpdateWithSetting()
        {
            SwitchYAxisType(dataType);
            SetXAxisTimeToday();
            lineChart.SetYAxisMinMax(yAxisRange.x, yAxisRange.y);
            lineChart.SetYAxisSplitNumber(ySplitNumber);
            lineChart.series[0].serieName = serieDataName;
            SwitchColorMode(isColorByLevel);
        }
        [Button]
        private void ApplyOverlayLayout() => lineChart.ApplyChartLayout();

        /// <summary>
        /// 切換Y軸的資料類型，並更新圖表的Y軸格式。
        /// </summary>
        public void SwitchYAxisType(EnumYAxisType newDataType)
        {
            switch (newDataType)
            {
                case EnumYAxisType.Temperature:
                    lineChart.SetYAxisToTemperatureFormat(yAxisRange.x, yAxisRange.y);
                    break;
                case EnumYAxisType.Humidity:
                    lineChart.SetYAxisToHumidityFormat(yAxisRange.x, yAxisRange.y);
                    break;
            }
        }

        /// <summary>
        /// 圖表是否以AlertLevel的顏色來顯示，若為false則使用單一顏色。
        /// </summary>
        public void SwitchColorMode(bool isLevelColor)
        {
            isColorByLevel = isLevelColor;
            if (isColorByLevel) lineChart.SetLevelColors(lineLevelColor, yAxisRange.y);
            else lineChart.SetLineColorStyle(lineColor);
        }
        /// <summary>
        /// 設定資料
        /// </summary>
        public void SetSerieData(List<XChartData> xchartData)
        {
            if (xchartData == null || xchartData.Count == 0)
            {
                Debug.LogWarning($"Received empty or null chart data for {dataType}. No data to display.");
                return;
            }
            SetXAxisTimeToday(xchartData.Count);

            // 確保有一個名為 "DataSeries" 的系列存在
            Line dataSerie = lineChart.EnsureSerie<Line>(serieDataName);
            // 將接收到的資料設定到系列中
            dataSerie.SetTimeData(xchartData, data => data.Time, data => data.Value);
            // 刷新圖表以顯示新的資料
            lineChart.RefreshChart();
        }
        private void OnEnable() => lineChart.ToAnimate();

        /// <summary>
        /// 設定X軸為今天的時間範圍，並根據資料數量決定分割數量。
        /// </summary>
        private void SetXAxisTimeToday(int splitNumber = 5) => lineChart.SetXAxisToTimeFormat(DateTime.Today, DateTime.Now, splitNumber);

        /// <summary>
        /// 清除指定系列的資料，預設為第一個系列。
        /// </summary>
        public void ClearSerieData(int serieIndex = 0)
        {
            lineChart.series[serieIndex].data.Clear();
            lineChart.RefreshChart();
        }

        public void ToAnimate() => lineChart.ToAnimate();
    }
}
