using System;
using System.Runtime.Serialization;
using VzDev.DateTimeUtils;

namespace VzDev.DataUtils
{
    [Serializable]
    public class TimeStampData
    {
        public string timeStamp;

        public DateTime LastUpdateTime { get; private set; }

        [OnDeserialized]
        private void OnDeserialize(StreamingContext context) => LastUpdateTime = DateTimeHelper.StringToDateTime(timeStamp);
    }
}
