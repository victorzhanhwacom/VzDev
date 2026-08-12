
using System;

namespace VzDev.DCIMUtils.DataUtils
{
    /// <summary>
    /// 溫度/濕度點位設備資訊
    /// </summary>
    [Serializable]
    public class PointModelData_RTRH : PointModelData
    {
        public float rtValue;
        public int rhValue;

        public string RtValueString => $"{rtValue:#.#}°C";
        public string RhValueString => $"{rhValue}%";
    }
}
