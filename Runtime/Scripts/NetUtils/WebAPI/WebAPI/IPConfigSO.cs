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

        [field: SerializeField, ReadOnly, Space(20)] public string URL { get; private set; }
        #endregion

        private void OnValidate() => URL = $"{httpType}://{ip.Trim()}:{port}{surfix}";


        #region 設定Config
        public void SetConfig(EnumHttpType httpType, string ip, int port=80, string surfix = "/api")
        {
            this.httpType = httpType;
            this.ip = ip;
            this.port = port;
            this.surfix = surfix;
            OnValidate();
        }
        #endregion
    }
}