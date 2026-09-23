using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using VzDev.ApiExtensions;
using VzDev.FileUtils;
using VictorDev.Managers;
using VzDev.UnityAPI.Extensions;

namespace VzDev.NetUtils
{
    public static class WebApiCaller
    {
        /// Get 請求
        public static void GetAsync(string url, Action<string> onSuccess, Action<string> onFailed = null)
        {
            WebApiRequestSO data = ScriptableObject.CreateInstance<WebApiRequestSO>();
            data.SetUrl(url);
            SendRequest(data, onSuccess, onFailed);
        }

        /// Post 請求
        public static void PostAsync(string url, string bodyJson, Action<string> onSuccess, Action<string> onFailed = null)
        {
            WebApiRequestSO data = ScriptableObject.CreateInstance<WebApiRequestSO>();
            data.SetUrl(url, EnumHttpMethod.POST);
            data.SetBodyRawJson(bodyJson);
            SendRequest(data, onSuccess, onFailed);
        }

        public static void StopRequest(WebApiRequestSO apiRequestSo) => TaskManager.Cancel($"SendRequest_{apiRequestSo.name}");

        /// 統一處理呼叫WebAPI (WebApiRequest類別)
        public static void SendRequest(WebApiRequestSO apiRequestSo, Action<string> onSuccess, Action<string> onFailed = null)
        {
            if (apiRequestSo == null)
            {
                Debug.LogWarning($"WebApiRequest is null");
                return;
            }

            onFailed ??= DefaultOnFailed;

            Debug.Log($"{apiRequestSo.name}: [{apiRequestSo.EnumHttpMethod}] {apiRequestSo.URL}");

            TaskManager.Run($"SendRequest_{apiRequestSo.name}", RunTask);

            // 執行Task：呼叫WebAPI（統一走 UnityWebRequest，所有 method 共用同一條路）
            async Task RunTask(CancellationToken token)
            {
                try
                {
                    token.ThrowIfCancellationRequested(); // 支援取消

                    var (rawBytes, headers) = await SendByUnityWebRequest(apiRequestSo, token);

                    string responseContent = string.Empty;

                    // 依回傳資料型態進行資料處理（原本吃 HttpContent，這裡改吃 UnityWebRequest 的 Header 字典）
                    switch (FileHelper.GetResponseDataTypeFromHttpHeader(headers))
                    {
                        case NetUtils.EnumResponseDataType.Json:
                            responseContent = Encoding.UTF8.GetString(rawBytes);
                            responseContent = responseContent.ToJsonFormat();
                            break;
                        case NetUtils.EnumResponseDataType.Excel:
                            string fileName = FileHelper.GetFileNameFromHttpHeader(headers);
                            //responseContent = await FileHelper.SaveFileWithPopupWindow(rawBytes, fileName);
                            break;
                        case NetUtils.EnumResponseDataType.Text:
                            responseContent = Encoding.UTF8.GetString(rawBytes);
                            break;
                    }

                    Debug.Log($"SendRequest Success: [{apiRequestSo.EnumHttpMethod}]\n{responseContent}");
                    onSuccess?.Invoke(responseContent);
                }
                catch (OperationCanceledException)
                {
                    Debug.LogWarning($"SendRequest Cancelled: {apiRequestSo.name}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"SendRequest Exception: {ex.Message}");
                    onFailed?.Invoke(ex.Message);
                }
            }
        }

        /// 統一用 UnityWebRequest 送出任意 HTTP Method（GET / POST / PUT / PATCH / DELETE...），WebGL 相容
        private static async Task<(byte[] rawBytes, Dictionary<string, string> headers)> SendByUnityWebRequest(
            WebApiRequestSO apiRequest,
            CancellationToken token)
        {
            using var request = new UnityWebRequest(apiRequest.URL, apiRequest.EnumHttpMethod.ToString());

            // ===== Body =====
            if (!string.IsNullOrEmpty(apiRequest.BodyRawJson))
            {
                byte[] bodyBytes = Encoding.UTF8.GetBytes(apiRequest.BodyRawJson);
                request.uploadHandler = new UploadHandlerRaw(bodyBytes);
                request.SetRequestHeader("Content-Type", apiRequest.MediaType);
            }

            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Accept", "application/json");

            // ===== 關鍵：複製 Header（含 Authorization）=====
            if (apiRequest.HttpRequestMessage != null)
            {
                foreach (var header in apiRequest.HttpRequestMessage.Headers)
                {
                    foreach (var value in header.Value)
                    {
                        request.SetRequestHeader(header.Key, value);
                    }
                }
            }

            // ===== Send =====
            var operation = request.SendWebRequest();

            using (token.Register(() =>
                   {
                       if (!operation.isDone) request.Abort();
                   }))
            {
                while (!operation.isDone)
                {
                    token.ThrowIfCancellationRequested();
                    await Task.Yield();
                }
            }

            token.ThrowIfCancellationRequested();

            if (request.result != UnityWebRequest.Result.Success)
            {
                throw new Exception($"[{(int)request.responseCode}] {request.error}");
            }

            var responseHeaders = request.GetResponseHeaders() ?? new Dictionary<string, string>();
            return (request.downloadHandler.data, responseHeaders);
        }

        /// 預設的OnFail事件處理
        private static void DefaultOnFailed(string msg) => Debug.LogWarning($"DefaultOnFailed! \n{msg}");
    }
}