using NaughtyAttributes;
using UnityEngine;

namespace VzDev.WebGLUtils
{
        /// <summary>
        /// 接收JS端訊息 (For 用戶登入)
        /// </summary>
        public class WebGLBridge_User : WebGLBridge
        {
                [field: SerializeField, ReadOnly]
                public string UserToken { get; private set; } = string.Empty;
                public void SetUserToken(string userToken)
                {
                        UserToken = userToken;
                        OnReceiveMessageFromJS?.Invoke(UserToken);
                }
                private const string MethodName_OnUnityInitialized = "OnUnityReady";

                private void Start() => InvokeOnUnityInitialized();

                /// <summary>
                /// 通知JS端Unity已經初始化完成
                /// </summary>
                private void InvokeOnUnityInitialized() => SendToJS(MethodName_OnUnityInitialized, true);
        }
}