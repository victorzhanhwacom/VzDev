using NaughtyAttributes;
using UnityEngine;
using System;

namespace VzDev.NetUtils
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
        [SerializeField, OnValueChanged("OnValidate")] public bool useProxyInPublishedWebGL = false;

        [field: SerializeField, ReadOnly, Space(20)] private string URL;
        #endregion

        public string GetURL()
        {
            if (Application.platform == RuntimePlatform.WindowsEditor)
            {
                return $"{httpType}://{ip.Trim()}:{port}{surfix}";
            }
            else
            {
                return URL;
            }
        }

        private void Awake() => OnValidate();

        private void OnValidate()
        {
            URL = (useProxyInPublishedWebGL) ? surfix : $"{httpType}://{ip.Trim()}:{port}{surfix}";
        }

        #region 設定Config
        public void SetConfig(EnumHttpType httpType, string ip, int port = 80, string surfix = "/api", bool useProxyInPublishedWebGL = false)
        {
            this.httpType = httpType;
            this.ip = ip;
            this.port = port;
            this.surfix = surfix;
            this.useProxyInPublishedWebGL = useProxyInPublishedWebGL;
            OnValidate();
        }
        #endregion
    }
}