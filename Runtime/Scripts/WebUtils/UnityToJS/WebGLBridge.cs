using System.Runtime.InteropServices;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;

namespace VzDev.WebGLUtils
{
        public class WebGLBridge : MonoBehaviour
        {
                [Foldout("[Events]")] public UnityEvent<string> OnReceiveMessageFromJS;
                [Foldout("[Events]")] public UnityEvent<string> OnSendMessageToJS;

                /// <summary>
                /// 傳送訊息給 JS 端，需對應到 JS 端的函式名稱
                /// </summary>
                public void SendToJS(string functionName, string payload)
                {
#if UNITY_WEBGL && !UNITY_EDITOR
            Unity_SendToJS(functionName, payload);
#else
                        Debug.Log($"[WebGLBridge] (Editor模擬) SendToJS: {functionName} -> {payload}");
#endif
                        OnSendMessageToJS?.Invoke(payload);
                }

                public void SendToJS(string functionName, int value) => SendToJS(functionName, value.ToString());
                public void SendToJS(string functionName, float value) => SendToJS(functionName, value.ToString("F3"));
                public void SendToJS(string functionName, bool value) => SendToJS(functionName, value ? "1" : "0");

                /// <summary>
                /// 接收 JS 傳來的訊息，需對應到 JS 端的函式名稱
                /// </summary>
                public void ReceiveFromJS(string payload) => OnReceiveMessageFromJS?.Invoke(payload);
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void Unity_SendToJS(string functionName, string payload);
#endif
        }
}