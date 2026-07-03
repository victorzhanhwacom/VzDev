using System.Runtime.InteropServices;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;

namespace VzDev.WebGLUtils
{
    public class WebGLBridge : MonoBehaviour
    {
        [InfoBox("Set {JS端函式名稱} 與 {傳給JS端的字串}，傳遞給JS端")]

        [Header("接收到 JS 訊息時觸發")]
        public UnityEvent<string> OnMessageReceived;

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void Unity_SendToJS(string functionName, string payload);
#endif

        public void SendToJS(string functionName, string payload)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            Unity_SendToJS(functionName, payload);
#else
            Debug.Log($"[WebGLBridge] (Editor模擬) SendToJS: {functionName} -> {payload}");
#endif
        }

        public void SendToJS(string functionName, int value) => SendToJS(functionName, value.ToString());
        public void SendToJS(string functionName, float value) => SendToJS(functionName, value.ToString("F3"));
        public void SendToJS(string functionName, bool value) => SendToJS(functionName, value ? "1" : "0");

        public void OnReceiveFromJS(string payload) => OnMessageReceived?.Invoke(payload);
    }
}