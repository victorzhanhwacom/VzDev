using NaughtyAttributes;
using UnityEngine;
using VzDev.ApiExtensions.XChart;
using XCharts.Runtime;

namespace VzDev.XChartUtils
{
    [RequireComponent(typeof(LineChart))]
    public class LineChartMediator : MonoBehaviour
    {
        #region Fields
        [Foldout("[Components]"), SerializeField] private LineChart lineChart;
        [Foldout("[XAxis]"), SerializeField, MinMaxSlider(0, 100)] private Vector2 xAxisMinMax = new Vector2(0, 24);
        [Foldout("[XAxis]"), SerializeField] private string xAxisLabelPrefix, xAxisLabelSuffix = ":00";
        [Foldout("[XAxis]"), SerializeField] private int xAxisSplitNumber = 6;

        [Foldout("[YAxis]"), SerializeField, MinMaxSlider(0, 100)] private Vector2 yAxisMinMax = new Vector2(0, 100);
        [Foldout("[YAxis]"), SerializeField] private string yAxisLabelPrefix, yAxisLabelSuffix = "°C";
        [Foldout("[YAxis]"), SerializeField] private int yAxisSplitNumber = 2;
        #endregion

        private void UpdateUI()
        {
            if (lineChart == null) return;
            UpdateXAxis();
            UpdateYAxis();
        }

        [Button]
        private void UpdateXAxis()
        {
            lineChart.SetXAxisMinMax(xAxisMinMax.x, xAxisMinMax.y);
            lineChart.SetXAxisLabelFormatter(xAxisLabelPrefix, xAxisLabelSuffix);
            lineChart.SetXAxisSplitNumber(xAxisSplitNumber);
        }

        [Button]
        private void UpdateYAxis()
        {
            lineChart.SetYAxisMinMax(yAxisMinMax.x, yAxisMinMax.y);
            lineChart.SetYAxisLabelFormatter(yAxisLabelPrefix, yAxisLabelSuffix);
            lineChart.SetYAxisSplitNumber(yAxisSplitNumber);
        }

        private void OnValidate()
        {
            if (lineChart == null) lineChart = GetComponent<LineChart>();
        }
    }
}
