using VzDev.Net.WebAPI;
using NaughtyAttributes;
using UnityEngine;

namespace VictorDev.Net.WebAPI
{
    /// WebAPI Authorization設定
    [CreateAssetMenu(fileName = "WebApiAuthorization", menuName = "VictorDev/Net/WebApiAuthorization")]
    public class WebApiAuthorizationSO : ScriptableObject
    {
        #region Variables

        [SerializeField] private EnumAuthorizationType authorizationType = EnumAuthorizationType.Bearer;

        [TextArea(1, 23), ShowIf(nameof(IsHaveAuth)), SerializeField]
        private string token;

        public EnumAuthorizationType AuthorizationType => authorizationType;
        public string Token => token;
        

        #endregion

        public void SetToken(string data, EnumAuthorizationType authorizationType)
        {
            this.authorizationType = authorizationType;
            SetToken(data);
        }
        public void SetToken(string data) => token = data;

        [Button]
        private void ClearToken()
        {
            authorizationType = EnumAuthorizationType.NoAuth;
            token = string.Empty;
        }
        
        
        public bool IsHaveAuth => authorizationType != EnumAuthorizationType.NoAuth;
    }
}