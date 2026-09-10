using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using VzDev.CoroutineUtils;

namespace VzDev.FileUtils
{
    public static class TextFileLoader
    {
        /// <summary>
        /// 使用 Coroutine 方式讀取文字檔案，適用於 WebGL 平台下透過 UnityWebRequest 非同步讀取 StreamingAssets。
        /// </summary>
        public static Coroutine LoadTextFileCoroutine(string filePath, Action<string> onComplete, Action<string> onError=null) 
        => CoroutineManager.Run(LoadCoroutine(filePath, onComplete, onError));

        private static IEnumerator LoadCoroutine(string filePath, Action<string> onComplete, Action<string> onError=null)
        {
            using (UnityWebRequest req = UnityWebRequest.Get(filePath))
            {
                yield return req.SendWebRequest();
                if (req.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"找不到檔案: {filePath}\n\t({req.error})");
                    onComplete?.Invoke(null);
                    yield break;
                }
                onComplete?.Invoke(req.downloadHandler.text);
            }
        }


        /// <summary>
        /// 直接從指定路徑讀取文字檔案，不適用於 WebGL 平台，僅適用於 PC / Mac / Linux 平台。
        /// </summary>
        public static void LoadTextFileDirectly(string filePath, Action<string> onComplete, Action<string> onError)
        {
            if (File.Exists(filePath))
            {
                string content = System.IO.File.ReadAllText(filePath);
                onComplete?.Invoke(content);
            }
            else
            {
                onError?.Invoke($"File not found: {filePath}");
            }
        }
    }
}
