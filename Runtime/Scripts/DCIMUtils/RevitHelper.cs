namespace VzDev.DCIMUtils
{
    public static class RevitHelper
    {
        /// <summary>
        /// 取得模型DeviceID，格式為[xxxxxx]
        /// </summary>
        public static string GetDeviceID(string modelName)
        {
            int start = modelName.IndexOf('[') + 1;
            int end = modelName.IndexOf(']');
            string content = modelName.Substring(start, end - start);
            return content;
        }
    }
}
