using System;
using Codice.Client.Common;

namespace VzDev.DCIMUtils.DataUtils
{
    /// <summary>
    /// 點位模型資訊
    /// </summary>
    public abstract class PointModelData
    {
        public string deviceCode;

        public TimeStampData timeStampData = new TimeStampData();
    }
}