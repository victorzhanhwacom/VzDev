using System;

namespace VzDev.DataUtils
{
    [Serializable]
    public class TimeStampData 
    {
        public string timeStamp;

        private DateTime _timeStamp;
        public DateTime LastUpdateTime
        {
            get
            {
                if (_timeStamp == DateTime.MinValue)
                {
                    if (DateTime.TryParse(timeStamp, out DateTime parsedTime))
                    {
                        _timeStamp = parsedTime;
                    }
                }
                return _timeStamp;
            }
        }
    }
}
