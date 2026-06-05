using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

// ════════════════════════════════════════════════════════════════
//  WebApiConfig  —  ScriptableObject，存放所有 API 設定
//  建立方式：Assets > Create > WebAPI > API Config
// ════════════════════════════════════════════════════════════════
[CreateAssetMenu(fileName = "WebApiConfig", menuName = "WebAPI/API Config")]
public class WebApiConfig : ScriptableObject
{
    [Header("基本設定")]
    public string baseUrl       = "https://api.example.com";
    public float  timeout       = 10f;
    public int    retryCount    = 2;
    public float  retryDelay    = 1f;

    [Header("Headers（全域）")]
    public List<ApiHeader> defaultHeaders = new List<ApiHeader>
    {
        new ApiHeader { key = "Content-Type", value = "application/json" },
        new ApiHeader { key = "Accept",       value = "application/json" }
    };

    [Header("認證")]
    public AuthType authType = AuthType.None;
    [Tooltip("Bearer Token 或 API Key 值")]
    public string authToken = "";
    [Tooltip("Basic Auth 使用者名稱")]
    public string basicAuthUser = "";
    [Tooltip("Basic Auth 密碼")]
    public string basicAuthPassword = "";

    [Header("除錯")]
    public bool logRequests  = true;
    public bool logResponses = true;
}

// ────────────────────────────────────────────────────────────────
//  輔助資料型別
// ────────────────────────────────────────────────────────────────
[Serializable]
public class ApiHeader
{
    public string key   = "";
    public string value = "";
}

public enum AuthType { None, BearerToken, ApiKey, BasicAuth }
public enum HttpMethod1 { GET, POST, PUT, PATCH, DELETE }

[Serializable]
public class ApiResponse
{
    public bool   success;
    public int    statusCode;
    public string rawBody;
    public string error;

    public T Parse<T>()
    {
        try   { return JsonUtility.FromJson<T>(rawBody); }
        catch { return default; }
    }
}

// ════════════════════════════════════════════════════════════════
//  WebApiClient  —  Runtime 核心模組（MonoBehaviour）
//  掛在任何 GameObject 上，或透過 WebApiClient.Instance 取用
// ════════════════════════════════════════════════════════════════
public class WebApiClient : MonoBehaviour
{
    // ── Singleton ────────────────────────────────────────────────
    private static WebApiClient _instance;
    public static WebApiClient Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("[WebApiClient]");
                _instance = go.AddComponent<WebApiClient>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    [Tooltip("拖入 WebApiConfig ScriptableObject")]
    public WebApiConfig config;

    private void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ════════════════════════════════════════════════════════════
    //  公開 API
    // ════════════════════════════════════════════════════════════

    /// <summary>GET 請求</summary>
    public void Get(string endpoint,
                    Action<ApiResponse> onSuccess,
                    Action<ApiResponse> onError   = null,
                    Dictionary<string, string> extraHeaders = null)
        => StartCoroutine(SendRequest(HttpMethod1.GET, endpoint, null, onSuccess, onError, extraHeaders));

    /// <summary>POST 請求（物件自動序列化為 JSON）</summary>
    public void Post<T>(string endpoint, T body,
                        Action<ApiResponse> onSuccess,
                        Action<ApiResponse> onError   = null,
                        Dictionary<string, string> extraHeaders = null)
        => StartCoroutine(SendRequest(HttpMethod1.POST, endpoint, JsonUtility.ToJson(body), onSuccess, onError, extraHeaders));

    /// <summary>POST 請求（直接傳入 JSON 字串）</summary>
    public void PostRaw(string endpoint, string jsonBody,
                        Action<ApiResponse> onSuccess,
                        Action<ApiResponse> onError   = null,
                        Dictionary<string, string> extraHeaders = null)
        => StartCoroutine(SendRequest(HttpMethod1.POST, endpoint, jsonBody, onSuccess, onError, extraHeaders));

    /// <summary>PUT 請求</summary>
    public void Put<T>(string endpoint, T body,
                       Action<ApiResponse> onSuccess,
                       Action<ApiResponse> onError   = null,
                       Dictionary<string, string> extraHeaders = null)
        => StartCoroutine(SendRequest(HttpMethod1.PUT, endpoint, JsonUtility.ToJson(body), onSuccess, onError, extraHeaders));

    /// <summary>PATCH 請求</summary>
    public void Patch<T>(string endpoint, T body,
                         Action<ApiResponse> onSuccess,
                         Action<ApiResponse> onError   = null,
                         Dictionary<string, string> extraHeaders = null)
        => StartCoroutine(SendRequest(HttpMethod1.PATCH, endpoint, JsonUtility.ToJson(body), onSuccess, onError, extraHeaders));

    /// <summary>DELETE 請求</summary>
    public void Delete(string endpoint,
                       Action<ApiResponse> onSuccess,
                       Action<ApiResponse> onError   = null,
                       Dictionary<string, string> extraHeaders = null)
        => StartCoroutine(SendRequest(HttpMethod1.DELETE, endpoint, null, onSuccess, onError, extraHeaders));

    // ════════════════════════════════════════════════════════════
    //  核心 Coroutine（供 Runtime 使用）
    // ════════════════════════════════════════════════════════════
    private IEnumerator SendRequest(HttpMethod1 method,
                                    string endpoint,
                                    string jsonBody,
                                    Action<ApiResponse> onSuccess,
                                    Action<ApiResponse> onError,
                                    Dictionary<string, string> extraHeaders)
    {
        int attempt = 0;
        int maxAttempts = (config != null ? config.retryCount : 0) + 1;

        while (attempt < maxAttempts)
        {
            attempt++;
            ApiResponse result = null;
            yield return BuildAndSend(method, endpoint, jsonBody, extraHeaders,
                                      r => result = r);

            if (result.success)
            {
                onSuccess?.Invoke(result);
                yield break;
            }

            bool isNetworkError = result.statusCode == 0 || result.statusCode >= 500;
            if (isNetworkError && attempt < maxAttempts)
            {
                if (config != null && config.logRequests)
                    Debug.LogWarning($"[WebApiClient] 第 {attempt} 次失敗，{config.retryDelay}s 後重試…");
                yield return new WaitForSeconds(config != null ? config.retryDelay : 1f);
                continue;
            }

            onError?.Invoke(result);
            yield break;
        }
    }

    // ════════════════════════════════════════════════════════════
    //  靜態版本（供 Editor 呼叫，不需要 MonoBehaviour）
    // ════════════════════════════════════════════════════════════
    public static IEnumerator StaticSendRequest(WebApiConfig cfg,
                                                HttpMethod1 method,
                                                string endpoint,
                                                string jsonBody,
                                                Action<ApiResponse> callback)
    {
        yield return BuildAndSendStatic(cfg, method, endpoint, jsonBody, null, callback);
    }

    // ────────────────────────────────────────────────────────────
    //  內部共用：組裝 UnityWebRequest 並送出
    // ────────────────────────────────────────────────────────────
    private IEnumerator BuildAndSend(HttpMethod1 method,
                                     string endpoint,
                                     string jsonBody,
                                     Dictionary<string, string> extraHeaders,
                                     Action<ApiResponse> callback)
    {
        yield return BuildAndSendStatic(config, method, endpoint, jsonBody, extraHeaders, callback);
    }

    public static IEnumerator BuildAndSendStatic(WebApiConfig cfg,
                                                  HttpMethod1 method,
                                                  string endpoint,
                                                  string jsonBody,
                                                  Dictionary<string, string> extraHeaders,
                                                  Action<ApiResponse> callback)
    {
        string url = (cfg != null ? cfg.baseUrl.TrimEnd('/') : "") + "/" + endpoint.TrimStart('/');

        UnityWebRequest req;
        byte[] bodyBytes = (jsonBody != null) ? Encoding.UTF8.GetBytes(jsonBody) : null;

        switch (method)
        {
            case HttpMethod1.POST:
                req = new UnityWebRequest(url, "POST");
                if (bodyBytes != null) req.uploadHandler = new UploadHandlerRaw(bodyBytes);
                req.downloadHandler = new DownloadHandlerBuffer();
                break;
            case HttpMethod1.PUT:
                req = new UnityWebRequest(url, "PUT");
                if (bodyBytes != null) req.uploadHandler = new UploadHandlerRaw(bodyBytes);
                req.downloadHandler = new DownloadHandlerBuffer();
                break;
            case HttpMethod1.PATCH:
                req = new UnityWebRequest(url, "PATCH");
                if (bodyBytes != null) req.uploadHandler = new UploadHandlerRaw(bodyBytes);
                req.downloadHandler = new DownloadHandlerBuffer();
                break;
            case HttpMethod1.DELETE:
                req = UnityWebRequest.Delete(url);
                req.downloadHandler = new DownloadHandlerBuffer();
                break;
            default: // GET
                req = UnityWebRequest.Get(url);
                break;
        }

        // 逾時
        req.timeout = (int)(cfg != null ? cfg.timeout : 10f);

        // 全域 Headers
        if (cfg != null)
        {
            foreach (var h in cfg.defaultHeaders)
                if (!string.IsNullOrEmpty(h.key)) req.SetRequestHeader(h.key, h.value);
        }

        // 認證
        if (cfg != null)
        {
            switch (cfg.authType)
            {
                case AuthType.BearerToken:
                    req.SetRequestHeader("Authorization", $"Bearer {cfg.authToken}");
                    break;
                case AuthType.ApiKey:
                    req.SetRequestHeader("X-Api-Key", cfg.authToken);
                    break;
                case AuthType.BasicAuth:
                    string encoded = Convert.ToBase64String(
                        Encoding.UTF8.GetBytes($"{cfg.basicAuthUser}:{cfg.basicAuthPassword}"));
                    req.SetRequestHeader("Authorization", $"Basic {encoded}");
                    break;
            }
        }

        // 額外 Headers
        if (extraHeaders != null)
            foreach (var kv in extraHeaders) req.SetRequestHeader(kv.Key, kv.Value);

        // Log 請求
        if (cfg != null && cfg.logRequests)
            Debug.Log($"[WebApiClient] → {method} {url}" +
                      (jsonBody != null ? $"\nBody: {jsonBody}" : ""));

        yield return req.SendWebRequest();

        var resp = new ApiResponse
        {
            statusCode = (int)req.responseCode,
            rawBody    = req.downloadHandler?.text ?? "",
            success    = !req.result.ToString().Contains("Error") &&
                         req.responseCode >= 200 && req.responseCode < 300,
            error      = req.result.ToString().Contains("Error") ? req.error : null
        };

        // Log 回應
        if (cfg != null && cfg.logResponses)
        {
            if (resp.success)
                Debug.Log($"[WebApiClient] ← {resp.statusCode} OK\n{resp.rawBody}");
            else
                Debug.LogWarning($"[WebApiClient] ← {resp.statusCode} ERROR: {resp.error}\n{resp.rawBody}");
        }

        req.Dispose();
        callback?.Invoke(resp);
    }
}
