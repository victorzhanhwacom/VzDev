namespace VzDev.DCIMUtils.DataUtils
{
    /// <summary>
    /// 感應器資訊
    /// </summary>
    public abstract class SensorData
    {
        public string deviceCode;
        public TimeStampData timeStampData = new TimeStampData();
    }
}