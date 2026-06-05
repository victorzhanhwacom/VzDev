#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

// ════════════════════════════════════════════════════════════════
//  WebApiTesterWindow  —  Editor 測試視窗（無需任何額外套件）
//  選單路徑：Tools > WebAPI Tester
// ════════════════════════════════════════════════════════════════
public class WebApiTesterWindow : EditorWindow
{
    // ── 設定參照 ────────────────────────────────────────────────
    private WebApiConfig _config;

    // ── 請求設定 ────────────────────────────────────────────────
    private HttpMethod1 _method   = HttpMethod1.GET;
    private string     _endpoint = "/users";
    private string     _jsonBody = "{\n  \"key\": \"value\"\n}";

    // ── 額外 Headers ────────────────────────────────────────────
    private List<ApiHeader> _extraHeaders    = new List<ApiHeader>();
    private bool            _showExtraHeaders = false;

    // ── 回應區 ──────────────────────────────────────────────────
    private string  _responseText  = "";
    private int     _responseCode  = 0;
    private bool    _isSuccess     = false;
    private bool    _isSending     = false;
    private float   _elapsedTime   = 0f;
    private double  _sendStartTime = 0;
    private Vector2 _responseScroll;

    // ── 歷史紀錄 ────────────────────────────────────────────────
    private List<RequestHistory> _history     = new List<RequestHistory>();
    private bool                 _showHistory = false;
    private Vector2              _historyScroll;

    // ── 取消 Token ──────────────────────────────────────────────
    private CancellationTokenSource _cts;

    // ── 樣式 ────────────────────────────────────────────────────
    private GUIStyle _responseStyle;
    private GUIStyle _successStyle;
    private GUIStyle _errorStyle;
    private GUIStyle _sectionStyle;
    private bool     _stylesInit = false;

    // ────────────────────────────────────────────────────────────
    [MenuItem("Tools/WebAPI Tester")]
    public static void ShowWindow()
    {
        var win = GetWindow<WebApiTesterWindow>("🌐 WebAPI Tester");
        win.minSize = new Vector2(500, 620);
    }

    private void OnDisable()
    {
        _cts?.Cancel();
        _cts?.Dispose();
    }

    // ════════════════════════════════════════════════════════════
    //  樣式初始化
    // ════════════════════════════════════════════════════════════
    private void InitStyles()
    {
        if (_stylesInit) return;
        _stylesInit = true;

        _responseStyle = new GUIStyle(EditorStyles.textArea)
        {
            wordWrap  = true,
            fontSize  = 11,
            richText  = true
        };
        _responseStyle.normal.textColor = new Color(0.85f, 0.95f, 0.85f);

        _successStyle = new GUIStyle(EditorStyles.boldLabel);
        _successStyle.normal.textColor = new Color(0.3f, 0.9f, 0.4f);

        _errorStyle = new GUIStyle(EditorStyles.boldLabel);
        _errorStyle.normal.textColor = new Color(1f, 0.35f, 0.35f);

        _sectionStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 12 };
    }

    // ════════════════════════════════════════════════════════════
    //  主繪製
    // ════════════════════════════════════════════════════════════
    private void OnGUI()
    {
        InitStyles();
        DrawToolbar();
        EditorGUILayout.Space(4);

        EditorGUILayout.BeginHorizontal();
        {
            // 左欄
            EditorGUILayout.BeginVertical(GUILayout.Width(position.width * 0.5f - 4));
            DrawConfigSection();
            EditorGUILayout.Space(4);
            DrawRequestSection();
            EditorGUILayout.Space(4);
            DrawExtraHeadersSection();
            EditorGUILayout.Space(6);
            DrawSendButton();
            EditorGUILayout.EndVertical();

            GUILayout.Space(4);

            // 右欄
            EditorGUILayout.BeginVertical();
            DrawResponseSection();
            EditorGUILayout.Space(4);
            DrawHistorySection();
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndHorizontal();

        if (_isSending)
        {
            _elapsedTime = (float)(EditorApplication.timeSinceStartup - _sendStartTime);
            Repaint();
        }
    }

    // ── 工具列 ──────────────────────────────────────────────────
    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label("🌐  WebAPI Tester", EditorStyles.boldLabel, GUILayout.Width(160));
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("清除歷史", EditorStyles.toolbarButton, GUILayout.Width(64)))
            _history.Clear();
        if (GUILayout.Button("清除回應", EditorStyles.toolbarButton, GUILayout.Width(64)))
            _responseText = "";
        EditorGUILayout.EndHorizontal();
    }

    // ── Config 區 ───────────────────────────────────────────────
    private void DrawConfigSection()
    {
        GUILayout.Label("⚙ API 設定", _sectionStyle);
        _config = (WebApiConfig)EditorGUILayout.ObjectField(
            "WebApiConfig", _config, typeof(WebApiConfig), false);

        if (_config == null)
        {
            EditorGUILayout.HelpBox(
                "請指定一個 WebApiConfig ScriptableObject，\n" +
                "或到 Assets > Create > WebAPI > API Config 建立一個。",
                MessageType.Warning);
        }
        else
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label($"Base URL：{_config.baseUrl}", EditorStyles.miniLabel);
            GUILayout.Label($"逾時：{_config.timeout}s　│　重試：{_config.retryCount} 次", EditorStyles.miniLabel);
            GUILayout.Label($"認證：{_config.authType}", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();

            if (GUILayout.Button("📝 編輯 Config", EditorStyles.miniButton))
                Selection.activeObject = _config;
        }
    }

    // ── 請求設定 ────────────────────────────────────────────────
    private void DrawRequestSection()
    {
        GUILayout.Label("📤 請求", _sectionStyle);

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("方法", GUILayout.Width(36));
        _method = (HttpMethod1)EditorGUILayout.EnumPopup(_method, GUILayout.Width(80));
        GUILayout.Label("Endpoint", GUILayout.Width(58));
        _endpoint = EditorGUILayout.TextField(_endpoint);
        EditorGUILayout.EndHorizontal();

        if (_config != null)
        {
            string full = _config.baseUrl.TrimEnd('/') + "/" + _endpoint.TrimStart('/');
            EditorGUILayout.SelectableLabel(full,
                new GUIStyle(EditorStyles.miniLabel)
                    { normal = { textColor = new Color(0.5f, 0.8f, 1f) } },
                GUILayout.Height(16));
        }

        bool needsBody = _method == HttpMethod1.POST ||
                         _method == HttpMethod1.PUT  ||
                         _method == HttpMethod1.PATCH;
        if (needsBody)
        {
            GUILayout.Label("JSON Body", EditorStyles.boldLabel);
            _jsonBody = EditorGUILayout.TextArea(_jsonBody, GUILayout.Height(90));
        }
    }

    // ── 額外 Headers ────────────────────────────────────────────
    private void DrawExtraHeadersSection()
    {
        _showExtraHeaders = EditorGUILayout.Foldout(_showExtraHeaders, "➕ 額外 Headers（本次請求）");
        if (!_showExtraHeaders) return;

        for (int i = 0; i < _extraHeaders.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            _extraHeaders[i].key   = EditorGUILayout.TextField(_extraHeaders[i].key,   GUILayout.MinWidth(100));
            _extraHeaders[i].value = EditorGUILayout.TextField(_extraHeaders[i].value, GUILayout.MinWidth(100));
            if (GUILayout.Button("✕", EditorStyles.miniButton, GUILayout.Width(22)))
            { _extraHeaders.RemoveAt(i); break; }
            EditorGUILayout.EndHorizontal();
        }
        if (GUILayout.Button("＋ 新增 Header", EditorStyles.miniButton))
            _extraHeaders.Add(new ApiHeader());
    }

    // ── 發送按鈕 ────────────────────────────────────────────────
    private void DrawSendButton()
    {
        GUI.enabled = _config != null && !_isSending;
        GUI.backgroundColor = _isSending ? Color.grey : new Color(0.3f, 0.7f, 1f);

        string label = _isSending
            ? $"⏳ 發送中… {_elapsedTime:F1}s"
            : $"▶  發送 {_method} 請求";

        if (GUILayout.Button(label, GUILayout.Height(36)))
            _ = SendRequestAsync();

        GUI.backgroundColor = Color.white;
        GUI.enabled = true;

        if (_isSending && GUILayout.Button("⏹ 取消", EditorStyles.miniButton))
        {
            _cts?.Cancel();
            _isSending    = false;
            _responseText = "⚠ 已取消請求";
            Repaint();
        }
    }

    // ── 回應區 ──────────────────────────────────────────────────
    private void DrawResponseSection()
    {
        GUILayout.Label("📥 回應", _sectionStyle);

        if (_responseCode > 0)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("狀態碼：");
            GUILayout.Label(_responseCode.ToString(),
                _isSuccess ? _successStyle : _errorStyle);
            GUILayout.Label($"({(_isSuccess ? "成功" : "失敗")})");
            if (_elapsedTime > 0)
                GUILayout.Label($"耗時：{_elapsedTime:F2}s", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.LabelField("Response Body", EditorStyles.boldLabel);
        var bgRect = EditorGUILayout.BeginVertical();
        EditorGUI.DrawRect(bgRect, new Color(0.1f, 0.12f, 0.1f));
        _responseScroll = EditorGUILayout.BeginScrollView(_responseScroll,
            GUILayout.MinHeight(160), GUILayout.ExpandHeight(true));
        EditorGUILayout.SelectableLabel(
            string.IsNullOrEmpty(_responseText) ? "（尚無回應）" : _responseText,
            _responseStyle, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();

        if (!string.IsNullOrEmpty(_responseText))
            if (GUILayout.Button("📋 複製到剪貼簿", EditorStyles.miniButton))
                GUIUtility.systemCopyBuffer = _responseText;
    }

    // ── 歷史記錄 ────────────────────────────────────────────────
    private void DrawHistorySection()
    {
        _showHistory = EditorGUILayout.Foldout(_showHistory, $"📜 請求歷史（{_history.Count}）");
        if (!_showHistory || _history.Count == 0) return;

        _historyScroll = EditorGUILayout.BeginScrollView(_historyScroll, GUILayout.MaxHeight(140));
        for (int i = _history.Count - 1; i >= 0; i--)
        {
            var h = _history[i];
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            var codeStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = h.success
                    ? new Color(0.3f, 0.9f, 0.4f)
                    : new Color(1f, 0.4f, 0.4f) }
            };
            GUILayout.Label($"[{h.code}]", codeStyle,          GUILayout.Width(38));
            GUILayout.Label(h.method.ToString(), EditorStyles.miniLabel, GUILayout.Width(44));
            GUILayout.Label(h.endpoint,          EditorStyles.miniLabel);
            GUILayout.Label(h.time,              EditorStyles.miniLabel, GUILayout.Width(54));
            if (GUILayout.Button("重用", EditorStyles.miniButton, GUILayout.Width(32)))
            {
                _method   = h.method;
                _endpoint = h.endpoint;
                if (h.body != null) _jsonBody = h.body;
            }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();
    }

    // ════════════════════════════════════════════════════════════
    //  發送請求（async / await，不依賴任何額外套件）
    // ════════════════════════════════════════════════════════════
    private async Task SendRequestAsync()
    {
        if (_config == null) return;

        _cts?.Dispose();
        _cts = new CancellationTokenSource();

        _isSending     = true;
        _responseText  = "";
        _responseCode  = 0;
        _sendStartTime = EditorApplication.timeSinceStartup;

        string body = (_method == HttpMethod1.POST ||
                       _method == HttpMethod1.PUT  ||
                       _method == HttpMethod1.PATCH)
            ? _jsonBody : null;

        Dictionary<string, string> extra = null;
        if (_extraHeaders.Count > 0)
        {
            extra = new Dictionary<string, string>();
            foreach (var h in _extraHeaders)
                if (!string.IsNullOrEmpty(h.key)) extra[h.key] = h.value;
        }

        ApiResponse result = null;
        try
        {
            result = await SendAsync(_config, _method, _endpoint, body, extra, _cts.Token);
        }
        catch (TaskCanceledException)
        {
            _isSending    = false;
            _responseText = "⚠ 已取消請求";
            Repaint();
            return;
        }

        _elapsedTime  = (float)(EditorApplication.timeSinceStartup - _sendStartTime);
        _isSending    = false;
        _responseCode = result.statusCode;
        _isSuccess    = result.success;
        _responseText = result.success
            ? PrettyJson(result.rawBody)
            : $"Error: {result.error}\n\n{result.rawBody}";

        _history.Add(new RequestHistory
        {
            method   = _method,
            endpoint = _endpoint,
            body     = body,
            code     = result.statusCode,
            success  = result.success,
            time     = System.DateTime.Now.ToString("HH:mm:ss")
        });

        Repaint();
    }

    // ── async UnityWebRequest 包裝 ──────────────────────────────
    private static async Task<ApiResponse> SendAsync(
        WebApiConfig cfg,
        HttpMethod1 method,
        string endpoint,
        string jsonBody,
        Dictionary<string, string> extraHeaders,
        CancellationToken ct)
    {
        string url = cfg.baseUrl.TrimEnd('/') + "/" + endpoint.TrimStart('/');

        UnityWebRequest req;
        byte[] bodyBytes = jsonBody != null ? Encoding.UTF8.GetBytes(jsonBody) : null;

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
            default:
                req = UnityWebRequest.Get(url);
                break;
        }

        req.timeout = (int)cfg.timeout;

        foreach (var h in cfg.defaultHeaders)
            if (!string.IsNullOrEmpty(h.key)) req.SetRequestHeader(h.key, h.value);

        switch (cfg.authType)
        {
            case AuthType.BearerToken:
                req.SetRequestHeader("Authorization", $"Bearer {cfg.authToken}");
                break;
            case AuthType.ApiKey:
                req.SetRequestHeader("X-Api-Key", cfg.authToken);
                break;
            case AuthType.BasicAuth:
                string enc = System.Convert.ToBase64String(
                    Encoding.UTF8.GetBytes($"{cfg.basicAuthUser}:{cfg.basicAuthPassword}"));
                req.SetRequestHeader("Authorization", $"Basic {enc}");
                break;
        }

        if (extraHeaders != null)
            foreach (var kv in extraHeaders) req.SetRequestHeader(kv.Key, kv.Value);

        if (cfg.logRequests)
            Debug.Log($"[WebApiTester] → {method} {url}" +
                      (jsonBody != null ? $"\nBody: {jsonBody}" : ""));

        // 送出並等待（透過 TaskCompletionSource 橋接）
        var tcs = new System.Threading.Tasks.TaskCompletionSource<bool>();
        var op  = req.SendWebRequest();
        op.completed += _ => tcs.SetResult(true);

        // 支援取消
        ct.Register(() =>
        {
            req.Abort();
            if (!tcs.Task.IsCompleted) tcs.TrySetCanceled();
        });

        await tcs.Task;

        if (ct.IsCancellationRequested)
            throw new TaskCanceledException();

        var resp = new ApiResponse
        {
            statusCode = (int)req.responseCode,
            rawBody    = req.downloadHandler?.text ?? "",
            success    = req.result == UnityWebRequest.Result.Success &&
                         req.responseCode >= 200 && req.responseCode < 300,
            error      = req.result != UnityWebRequest.Result.Success ? req.error : null
        };

        if (cfg.logResponses)
        {
            if (resp.success)
                Debug.Log($"[WebApiTester] ← {resp.statusCode} OK\n{resp.rawBody}");
            else
                Debug.LogWarning($"[WebApiTester] ← {resp.statusCode} ERROR: {resp.error}\n{resp.rawBody}");
        }

        req.Dispose();
        return resp;
    }

    // ── JSON 美化 ───────────────────────────────────────────────
    private string PrettyJson(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return "(空回應)";
        try
        {
            int  indent   = 0;
            var  sb       = new StringBuilder();
            bool inString = false;
            foreach (char c in raw)
            {
                if (c == '"' && (sb.Length == 0 || sb[sb.Length - 1] != '\\'))
                    inString = !inString;
                if (!inString)
                {
                    if (c == '{' || c == '[')
                    { sb.Append(c); sb.Append('\n'); indent++; sb.Append(new string(' ', indent * 2)); continue; }
                    if (c == '}' || c == ']')
                    { sb.Append('\n'); indent = Mathf.Max(0, indent - 1); sb.Append(new string(' ', indent * 2)); sb.Append(c); continue; }
                    if (c == ',')
                    { sb.Append(c); sb.Append('\n'); sb.Append(new string(' ', indent * 2)); continue; }
                    if (c == ':')  { sb.Append(": "); continue; }
                    if (c == ' ' || c == '\n' || c == '\r' || c == '\t') continue;
                }
                sb.Append(c);
            }
            return sb.ToString();
        }
        catch { return raw; }
    }

    // ── 歷史資料 ────────────────────────────────────────────────
    private class RequestHistory
    {
        public HttpMethod1 method;
        public string     endpoint;
        public string     body;
        public int        code;
        public bool       success;
        public string     time;
    }
}
#endif
