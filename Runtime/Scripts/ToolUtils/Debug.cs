using UnityEngine;

namespace VzDev.ToolUtils
{
    /// <summary>
    /// Debug工具類別，提供Log、LogWarning、LogError等方法，並可選擇是否在Runtime時輸出訊息。
    /// </summary>
    public static class Debug
    {
        /// <summary>
        /// 嘗試輸出Log訊息，若logEnabled為false則不輸出訊息。
        /// </summary>
        public static void TryLog(bool logEnabled, object message, LogType logType = LogType.Log, Object callerClass = null)
        {
            if (logEnabled) LogMessage(logType, message, callerClass);
        }

        /// <summary>
        /// 判斷目標物件是否為null，若為null則輸出警告訊息。
        /// </summary>
        public static bool CheckIsNotNull(Object target, string msgOnNull, Object callerClass = null)
        {
            if (target == null) LogWarning(msgOnNull, callerClass);
            return target != null;
        }

        /// <summary>
        /// 設定Log訊息的格式，若callerClass不為null，則在訊息前加上callerClass的名稱。
        /// </summary>
        private static string FormatMessage(string message, Object callerClass) => callerClass != null
                ? string.Concat("[", callerClass, "]\n", message)
                : message;


        public static void Log(object message, Object callerClass = null) => LogMessage(LogType.Log, message, callerClass);

        public static void LogWarning(object message, Object callerClass = null) => LogMessage(LogType.Warning, message, callerClass);

        public static void LogError(object message, Object callerClass = null) => LogMessage(LogType.Error, message, callerClass);

        private static void LogMessage(LogType logType, object message, Object callerClass)
        {
            string msg = message?.ToString() ?? string.Empty;
            msg = FormatMessage(msg, callerClass);
            switch (logType)
            {
                case LogType.Log:
                    UnityEngine.Debug.Log(msg, callerClass);
                    break;
                case LogType.Warning:
                    UnityEngine.Debug.LogWarning(msg, callerClass);
                    break;
                case LogType.Error:
                    UnityEngine.Debug.LogError(msg, callerClass);
                    break;
                case LogType.Assert:
                    UnityEngine.Debug.LogAssertion(msg, callerClass);
                    break;
                case LogType.Exception:
                    UnityEngine.Debug.LogException(new System.Exception(msg), callerClass);
                    break;
            }
        }
    }
}