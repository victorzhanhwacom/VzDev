using System;
using NaughtyAttributes;
using Newtonsoft.Json;
using UnityEngine;
using VzDev.Frameworks;
using VzDev.NetUtils;

namespace VzDev
{
    /// <summary>
    /// 系統Config設定
    /// </summary>
    public class SystemConfigHandler : SingletonMonoBehaviour<SystemConfigHandler>
    {
        public static Action<SystemConfig> OnGetSystemConfigAction;

        #region Fields
        [SerializeField, ReadOnly] private SystemConfig systemConfig;
        [SerializeField, Expandable] private IPConfigSO ipConfig;

        public static SystemConfig SystemConfig => Instance.systemConfig;
        #endregion

        public void ParseJson(string json)
        {
            systemConfig = JsonConvert.DeserializeObject<SystemConfig>(json);

            ipConfig?.SetConfig(systemConfig.webapi.httpType, systemConfig.webapi.ip, systemConfig.webapi.port, systemConfig.webapi.surfix
            , systemConfig.webapi.usingProxyURL);
            Debug.Log($"LogEnabled: {systemConfig.system.logEnabled}");

#if UNITY_EDITOR
            Debug.Log($"Set LogEnabled when EditorMode: true");
            Debug.unityLogger.logEnabled = true;
#else
            Debug.unityLogger.logEnabled = systemConfig.system.logEnabled;
#endif

            Debug.Log($"WebAPI URL: {ipConfig?.GetURL()}");
            Debug.Log($"TimeoutTimer: {systemConfig.webapi.requestTimeoutSec}, TimeIntervalTimer: {systemConfig.webapi.requestIntervalSec}");

            OnGetSystemConfigAction?.Invoke(systemConfig);
        }
        public static bool IsDemo => Instance.systemConfig.system.isDemo;
        public static bool IsLogEnabled => Instance.systemConfig.system.logEnabled;
        public static string BuildingCode => Instance.systemConfig.system.buildingCode;
    }

    [Serializable]
    public struct SystemConfig
    {
        #region JsonProperty
        [JsonProperty]
        [field: SerializeField]
        public SystemNode system { get; private set; }

        [JsonProperty]
        [field: SerializeField]
        public WebApiNode webapi { get; private set; }
        #endregion

        [Serializable]
        public struct SystemNode
        {
            [JsonProperty]
            [field: SerializeField]
            public bool isDemo { get; private set; }
            [JsonProperty]
            [field: SerializeField]
            public bool logEnabled { get; private set; }
            [JsonProperty]
            [field: SerializeField]
            public string buildingCode { get; private set; }
        }

        [Serializable]
        public struct WebApiNode
        {
            [JsonProperty]
            [field: SerializeField]
            public EnumHttpType httpType { get; private set; }
            [JsonProperty]
            [field: SerializeField]
            public string ip { get; private set; }
            [JsonProperty]
            [field: SerializeField]
            public int port { get; private set; }
            [JsonProperty]
            [field: SerializeField]
            public string surfix { get; private set; }
            [JsonProperty]
            [field: SerializeField]
            public bool usingProxyURL { get; private set; }

            [JsonProperty]
            [field: SerializeField]
            public float requestTimeoutSec { get; private set; }
            [JsonProperty]
            [field: SerializeField]
            public float requestIntervalSec { get; private set; }
        }
    }
}
