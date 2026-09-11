using VzDev.Net.WebAPI;
using NaughtyAttributes;
using UnityEngine;
using Debug = VzDev.ToolUtils.Debug;

namespace VictorDev.Net
{
    /// 設定IP、Port
    [CreateAssetMenu(fileName = "IPConfig", menuName = "VictorDev/Net/IPConfig")]
    public class IPConfigSO : ScriptableObject
    {
        #region Variables
        [SerializeField] private EnumHttpType httpType;
        [Space(10), SerializeField] private string ip;
        [Space(10), SerializeField] private int port = 80;

        public string IP => ip.Trim();
        public int Port => port;
        #endregion
        
        public string URL
        {
            get
            {
                string result = $"{httpType}://{IP}";
                if(Port != 80) result += $":{Port}";
                return result;
            }
        }

        [Button]
        private void LogURL() => Debug.Log($"URL:  {URL}");
    }
}