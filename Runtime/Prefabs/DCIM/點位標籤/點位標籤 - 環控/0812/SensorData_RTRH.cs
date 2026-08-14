
using System;

namespace VzDev.DataUtils
{
    /// <summary>
    /// 溫度/濕度點位設備資訊
    /// </summary>
    [Serializable]
    public class SensorData_RTRH : SensorData
    {
        public float rtValue;
        public int rhValue;

        public string RtValueString => $"{rtValue:#.#}°C";
        public string RhValueString => $"{rhValue}%";
    }
}
