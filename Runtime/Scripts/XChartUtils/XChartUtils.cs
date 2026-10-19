using System;
using System.Collections.Generic;

namespace VzDev.XChartUtils
{
    #region 圖表Y軸類型
    public enum EnumYAxisType
    {
        Temperature,
        Humidity
    }
    #endregion

    #region 資料結構
    /// <summary>
    /// ChartData結構，包含時間與數值，用於XCharts的資料呈現
    /// </summary>
    [Serializable]
    public class XChartData
    {
        public DateTime Time;
        public double Value;
        public XChartData(DateTime time, double value)
        {
            Time = time;
            Value = value;
        }
    }
    #endregion

    #region 介面
    /// <summary>
    /// 供XCharts使用的資料接收介面，實作此介面即可接收ChartData資料
    /// </summary>
    public interface IXChartData
    {
        /// 將資料轉換為XChartData結構
        /// </summary>
        XChartData ToXChartData();
    }
    #endregion
}
