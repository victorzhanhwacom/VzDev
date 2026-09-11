using VzDev.Net.WebAPI;
using NaughtyAttributes;
using UnityEngine;
using System;

namespace VictorDev.Net
{
    /// 設定IP、Port
    [CreateAssetMenu(fileName = "IPConfig", menuName = "VzDev/Net/IPConfig")]
    public class IPConfigSO : ScriptableObject
    {
        #region Variables
        [SerializeField] private EnumHttpType httpType;
        [SerializeField] private string ip;
        [SerializeField] private int port = 80;
        [SerializeField] public string surfix = "/api";

        [field: SerializeField, ReadOnly, Space(20)] public string URL { get; private set; }
        #endregion

        private void OnValidate() => URL = $"{httpType}://{ip.Trim()}:{port}{surfix}";


        #region 設定Config
        public void SetConfig(string httpType, string ip, string port)
        {
            EnumHttpType parsedHttpType;
            if (Enum.TryParse(httpType, out parsedHttpType) == false)
            {
                Debug.Log($"⚠️Invalid httpType: {httpType}. Using default value.");
                parsedHttpType = EnumHttpType.http; // Default value
            }
            SetConfig(parsedHttpType, ip, int.TryParse(port, out int parsedPort) ? parsedPort : 80);
        }

        public void SetConfig(EnumHttpType httpType, string ip, int port)
        {
            this.httpType = httpType;
            this.ip = ip;
            this.port = port;
            OnValidate();
        }
        #endregion
    }
}