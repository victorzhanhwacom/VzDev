using System.Runtime.InteropServices;
using UnityEngine;

namespace VzDev.NetUtils.WebGL
{
    public class WebBridge : MonoBehaviour
    {
        // 宣告外部 JavaScript 方法
        [DllImport("__Internal")]
        private static extern void ShowAlert(string str);

        [DllImport("__Internal")]
        private static extern void SendDataToPage(int score);

        void Start()
        {
            // 呼叫網頁 alert
            if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                ShowAlert("Hello from Unity WebGL!");
            }
        }

        public void OnGameOver(int finalScore)
        {
            // 遊戲結束時，把分數傳給網頁
            if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                SendDataToPage(finalScore);
            }
        }
    }
}
