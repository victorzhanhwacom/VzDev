using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Events;

namespace VzDev.WebUtils
{
    /// <summary>
    /// Unity <-> JavaScript 雙向參數傳遞橋接。
    /// C# -> JS:透過 DllImport 呼叫 window.VzDevBridge[functionName]。
    /// JS -> C#:透過 unityInstance.SendMessage 呼叫本物件上的 OnReceiveFromJS。
    /// </summary>
    public class WebGLBridge : MonoBehaviour
    {
        #region Fields

        [Header("接收到 JS 訊息時觸發 (payload 為原始字串,可自行解析 JSON)")]
        public UnityEvent<string> OnMessageReceived;

        #endregion

        #region DllImport

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void VzDev_SendToJS(string functionName, string payload);
#endif

        #endregion

        #region Public API - C# -> JS

        /// <summary>
        /// 呼叫瀏覽器端 window.VzDevBridge[functionName](payload)
        /// </summary>
        public void SendToJS(string functionName, string payload)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            VzDev_SendToJS(functionName, payload);
#else
            Debug.Log($"[WebGLBridge] (Editor模擬) SendToJS: {functionName} -> {payload}");
#endif
        }

        /// <summary>
        /// 給 int/float/bool 用的便利多載,內部轉字串再送出。
        /// </summary>
        public void SendToJS(string functionName, int value) => SendToJS(functionName, value.ToString());
        public void SendToJS(string functionName, float value) => SendToJS(functionName, value.ToString("F3"));
        public void SendToJS(string functionName, bool value) => SendToJS(functionName, value ? "1" : "0");

        #endregion

        #region Public API - JS -> C#

        /// <summary>
        /// 供 JS 端呼叫:
        /// unityInstance.SendMessage('WebGLBridge物件名稱', 'OnReceiveFromJS', payload);
        /// payload 建議統一用 JSON 字串,内部再依 SetValue 慣例分發。
        /// </summary>
        public void OnReceiveFromJS(string payload)
        {
            OnMessageReceived?.Invoke(payload);
        }

        #endregion
    }
}