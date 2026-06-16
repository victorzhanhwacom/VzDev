using UnityEngine;

namespace VzDev.Extensions
{
    /// <summary>
    /// Debug工具類別，提供Log、LogWarning、LogError等方法，並可選擇是否在Runtime時輸出訊息。
    /// </summary>
    public static class Debug
    {
        private static bool IsLogOnRuntime;

        /// <summary>
        /// 設定是否在Runtime時輸出Log訊息，預設為false，表示只在編輯器中輸出訊息。
        /// </summary>
        public static void SetLogOnRuntime(bool isOn) => IsLogOnRuntime = isOn;

        /// <summary>
        /// 輸出一般訊息，若在Runtime且IsLogOnRuntime為false則不輸出訊息
        /// </summary>
        public static void Log(object message, object callerClass = null) => LogMessage(EnumLogType.Log, message, callerClass);

        /// <summary>
        /// 輸出警告訊息，若在Runtime且IsLogOnRuntime為false則不輸出訊息
        /// </summary>
        public static void LogWarning(object message, object callerClass = null) => LogMessage(EnumLogType.LogWarning, message, callerClass);

        /// <summary>
        /// 輸出錯誤訊息，若在Runtime且IsLogOnRuntime為false則不輸出訊息
        /// </summary>
        public static void LogError(object message, object callerClass = null) => LogMessage(EnumLogType.LogError, message, callerClass);

        /// <summary>
        /// 根據Log類型輸出訊息，若在Runtime且IsLogOnRuntime為false則不輸出訊息。
        /// <para> + callerClass使用： GetType().Name / nameOf(T) </para>
        /// </summary>
        private static void LogMessage(EnumLogType logType, object message, object callerClass)
        {
            /// Runtime且logEnabled為false則不輸出訊息
            if (Application.isPlaying && IsLogOnRuntime == false) return;

            string msg = message?.ToString() ?? string.Empty;
            message = callerClass != null
            ? string.Concat("[", callerClass, "]\n", msg)
            : msg;

            switch (logType)
            {
                case EnumLogType.Log:
                    UnityEngine.Debug.Log(message);
                    break;
                case EnumLogType.LogWarning:
                    UnityEngine.Debug.LogWarning(message);
                    break;
                case EnumLogType.LogError:
                    UnityEngine.Debug.LogError(message);
                    break;
            }
        }

        /// <summary>
        /// 斷言條件是否為真，若為假則輸出警告訊息
        /// </summary>
        public static bool IsTrue(bool condition, string msgOnFalse, object callerClass = null)
        {
            if (!condition) LogWarning(msgOnFalse, callerClass);
            return condition;
        }

        private enum EnumLogType
        {
            Log,
            LogWarning,
            LogError
        }
    }
}