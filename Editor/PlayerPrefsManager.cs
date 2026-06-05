using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Unity Editor 擴充工具：PlayerPrefs 視覺化管理器
/// 選單路徑：Tools > PlayerPrefs Manager
/// </summary>
public class PlayerPrefsManager : EditorWindow
{
    // ──────────────────────────────────────────
    // 資料結構
    // ──────────────────────────────────────────
    private enum PrefType { String, Int, Float }

    private class PrefEntry
    {
        public string key      = "";
        public string strValue = "";
        public int    intValue = 0;
        public float  floValue = 0f;
        public PrefType type   = PrefType.String;
        public bool   isDirty  = false;   // 值已被修改但尚未 Save
    }

    // ──────────────────────────────────────────
    // 狀態
    // ──────────────────────────────────────────
    private List<PrefEntry> _entries      = new List<PrefEntry>();
    private PrefEntry       _newEntry     = new PrefEntry();
    private string          _searchQuery  = "";
    private Vector2         _scrollPos;
    private bool            _confirmClear = false;

    // 已知的 Prefs Key 清單（可在此手動維護，也可在 Runtime 收集）
    // 若要讓工具自動列出，請在 Player 端用 PlayerPrefs.SetString("__keys__", ...) 儲存
    private static readonly string KEYS_REGISTRY = "__pref_manager_keys__";

    // ──────────────────────────────────────────
    // 開啟視窗
    // ──────────────────────────────────────────
    [MenuItem("VzDev/Tools/PlayerPrefs Manager %&#p")]   // Ctrl+Alt+Shift+P
    public static void ShowWindow()
    {
        var win = GetWindow<PlayerPrefsManager>("🗄 PlayerPrefs Manager");
        win.minSize = new Vector2(540, 400);
        win.LoadAll();
    }

    // ──────────────────────────────────────────
    // 載入 / 儲存
    // ──────────────────────────────────────────
    private void LoadAll()
    {
        _entries.Clear();
        string raw = PlayerPrefs.GetString(KEYS_REGISTRY, "");
        if (string.IsNullOrEmpty(raw)) return;

        foreach (var key in raw.Split('|').Where(k => !string.IsNullOrEmpty(k)))
        {
            var e = BuildEntry(key);
            if (e != null) _entries.Add(e);
        }
    }

    private PrefEntry BuildEntry(string key)
    {
        // 嘗試判斷型別（優先 Int > Float > String）
        string typeKey = KEYS_REGISTRY + "_type_" + key;
        string typeName = PlayerPrefs.GetString(typeKey, "");

        PrefType t = PrefType.String;
        if (typeName == "Int")   t = PrefType.Int;
        else if (typeName == "Float") t = PrefType.Float;

        var e = new PrefEntry { key = key, type = t };
        RefreshEntryValue(e);
        return e;
    }

    private void RefreshEntryValue(PrefEntry e)
    {
        switch (e.type)
        {
            case PrefType.Int:    e.intValue = PlayerPrefs.GetInt(e.key, 0);         break;
            case PrefType.Float:  e.floValue = PlayerPrefs.GetFloat(e.key, 0f);      break;
            default:              e.strValue = PlayerPrefs.GetString(e.key, "");     break;
        }
        e.isDirty = false;
    }

    private void SaveEntry(PrefEntry e)
    {
        switch (e.type)
        {
            case PrefType.Int:   PlayerPrefs.SetInt(e.key, e.intValue);    break;
            case PrefType.Float: PlayerPrefs.SetFloat(e.key, e.floValue);  break;
            default:             PlayerPrefs.SetString(e.key, e.strValue); break;
        }
        // 記錄型別
        PlayerPrefs.SetString(KEYS_REGISTRY + "_type_" + e.key, e.type.ToString());
        PlayerPrefs.Save();
        e.isDirty = false;
    }

    private void RegisterKey(string key)
    {
        string raw = PlayerPrefs.GetString(KEYS_REGISTRY, "");
        var keys = new HashSet<string>(raw.Split('|').Where(k => !string.IsNullOrEmpty(k)));
        keys.Add(key);
        PlayerPrefs.SetString(KEYS_REGISTRY, string.Join("|", keys));
        PlayerPrefs.Save();
    }

    private void DeleteEntry(PrefEntry e)
    {
        PlayerPrefs.DeleteKey(e.key);
        PlayerPrefs.DeleteKey(KEYS_REGISTRY + "_type_" + e.key);
        // 從清單移除
        string raw = PlayerPrefs.GetString(KEYS_REGISTRY, "");
        var keys = new HashSet<string>(raw.Split('|').Where(k => !string.IsNullOrEmpty(k)));
        keys.Remove(e.key);
        PlayerPrefs.SetString(KEYS_REGISTRY, string.Join("|", keys));
        PlayerPrefs.Save();
        _entries.Remove(e);
    }

    // ──────────────────────────────────────────
    // 樣式（懶載入）
    // ──────────────────────────────────────────
    private GUIStyle _headerStyle;
    private GUIStyle _rowEvenStyle;
    private GUIStyle _rowOddStyle;
    private GUIStyle _dirtyLabelStyle;
    private bool     _stylesInit = false;

    private void InitStyles()
    {
        if (_stylesInit) return;
        _stylesInit = true;

        _headerStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize  = 13,
            alignment = TextAnchor.MiddleLeft
        };

        _rowEvenStyle = new GUIStyle(GUIStyle.none);
        _rowEvenStyle.normal.background = MakeTex(1, 1, new Color(0.22f, 0.22f, 0.22f, 0.4f));

        _rowOddStyle = new GUIStyle(GUIStyle.none);
        _rowOddStyle.normal.background = MakeTex(1, 1, new Color(0.18f, 0.18f, 0.18f, 0.2f));

        _dirtyLabelStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            normal = { textColor = new Color(1f, 0.75f, 0.2f) }
        };
    }

    private static Texture2D MakeTex(int w, int h, Color col)
    {
        var pix = new Color[w * h];
        for (int i = 0; i < pix.Length; i++) pix[i] = col;
        var t = new Texture2D(w, h);
        t.SetPixels(pix);
        t.Apply();
        return t;
    }

    // ──────────────────────────────────────────
    // 主繪製
    // ──────────────────────────────────────────
    private void OnGUI()
    {
        InitStyles();

        DrawToolbar();
        EditorGUILayout.Space(4);
        DrawAddNewSection();
        EditorGUILayout.Space(6);
        DrawEntryList();
        EditorGUILayout.Space(6);
        DrawFooter();
    }

    // ── 工具列 ──────────────────────────────
    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        GUILayout.Label("🗄 PlayerPrefs Manager", _headerStyle, GUILayout.Width(220));

        GUILayout.FlexibleSpace();

        GUILayout.Label("🔍", GUILayout.Width(18));
        _searchQuery = EditorGUILayout.TextField(_searchQuery, EditorStyles.toolbarSearchField, GUILayout.Width(160));

        if (GUILayout.Button("⟳ 重新載入", EditorStyles.toolbarButton, GUILayout.Width(72)))
        {
            LoadAll();
            _confirmClear = false;
        }

        if (GUILayout.Button("💾 全部儲存", EditorStyles.toolbarButton, GUILayout.Width(72)))
        {
            foreach (var e in _entries.Where(e => e.isDirty)) SaveEntry(e);
        }

        EditorGUILayout.EndHorizontal();
    }

    // ── 新增區 ───────────────────────────────
    private void DrawAddNewSection()
    {
        var bgRect = EditorGUILayout.BeginVertical();
        EditorGUI.DrawRect(bgRect, new Color(0.15f, 0.35f, 0.55f, 0.25f));

        GUILayout.Label("  ＋ 新增 Pref", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Key", GUILayout.Width(32));
        _newEntry.key  = EditorGUILayout.TextField(_newEntry.key, GUILayout.MinWidth(120));

        GUILayout.Label("型別", GUILayout.Width(30));
        _newEntry.type = (PrefType)EditorGUILayout.EnumPopup(_newEntry.type, GUILayout.Width(60));

        GUILayout.Label("值", GUILayout.Width(14));
        DrawValueField(_newEntry, GUILayout.MinWidth(100));

        GUI.enabled = !string.IsNullOrWhiteSpace(_newEntry.key);
        if (GUILayout.Button("新增", GUILayout.Width(44)))
        {
            if (_entries.Any(e => e.key == _newEntry.key))
            {
                EditorUtility.DisplayDialog("重複的 Key",
                    $"Key「{_newEntry.key}」已存在，請直接在列表中編輯。", "OK");
            }
            else
            {
                var created = new PrefEntry
                {
                    key      = _newEntry.key,
                    type     = _newEntry.type,
                    strValue = _newEntry.strValue,
                    intValue = _newEntry.intValue,
                    floValue = _newEntry.floValue
                };
                SaveEntry(created);
                RegisterKey(created.key);
                _entries.Add(created);
                // 重置
                _newEntry = new PrefEntry();
            }
        }
        GUI.enabled = true;

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
    }

    // ── 列表 ─────────────────────────────────
    private void DrawEntryList()
    {
        // 表頭
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label("Key",      EditorStyles.boldLabel, GUILayout.MinWidth(140));
        GUILayout.Label("型別",     EditorStyles.boldLabel, GUILayout.Width(60));
        GUILayout.Label("值",       EditorStyles.boldLabel, GUILayout.MinWidth(140));
        GUILayout.Label("",                                 GUILayout.Width(130)); // 操作按鈕佔位
        EditorGUILayout.EndHorizontal();

        _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

        var filtered = string.IsNullOrEmpty(_searchQuery)
            ? _entries
            : _entries.Where(e => e.key.ToLower().Contains(_searchQuery.ToLower())).ToList();

        if (filtered.Count == 0)
        {
            EditorGUILayout.HelpBox(
                _entries.Count == 0
                    ? "尚無任何 Pref。請使用上方「新增 Pref」來建立。"
                    : "沒有符合搜尋條件的結果。",
                MessageType.Info);
        }

        PrefEntry toDelete = null;

        for (int i = 0; i < filtered.Count; i++)
        {
            var e = filtered[i];
            var rowStyle = (i % 2 == 0) ? _rowEvenStyle : _rowOddStyle;

            EditorGUILayout.BeginHorizontal(rowStyle, GUILayout.Height(22));

            // Key（唯讀顯示，可複製）
            EditorGUILayout.SelectableLabel(e.key, GUILayout.MinWidth(140), GUILayout.Height(18));

            // 型別（可切換，切換後自動轉型）
            var newType = (PrefType)EditorGUILayout.EnumPopup(e.type, GUILayout.Width(60));
            if (newType != e.type)
            {
                e.type    = newType;
                e.isDirty = true;
            }

            // 值欄位
            EditorGUI.BeginChangeCheck();
            DrawValueField(e, GUILayout.MinWidth(140));
            if (EditorGUI.EndChangeCheck()) e.isDirty = true;

            // 髒標記
            if (e.isDirty)
                GUILayout.Label("● 未存", _dirtyLabelStyle, GUILayout.Width(42));
            else
                GUILayout.Label("",                          GUILayout.Width(42));

            // 儲存按鈕
            GUI.enabled = e.isDirty;
            if (GUILayout.Button("儲存", EditorStyles.miniButtonLeft, GUILayout.Width(36)))
                SaveEntry(e);
            GUI.enabled = true;

            // 重置按鈕
            if (GUILayout.Button("重置", EditorStyles.miniButtonMid, GUILayout.Width(36)))
                RefreshEntryValue(e);

            // 刪除按鈕
            GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
            if (GUILayout.Button("刪除", EditorStyles.miniButtonRight, GUILayout.Width(36)))
            {
                if (EditorUtility.DisplayDialog("確認刪除",
                    $"確定要刪除 Key「{e.key}」？此操作無法復原。", "刪除", "取消"))
                    toDelete = e;
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();
        }

        if (toDelete != null) DeleteEntry(toDelete);

        EditorGUILayout.EndScrollView();
    }

    // ── 頁尾 ─────────────────────────────────
    private void DrawFooter()
    {
        EditorGUILayout.BeginHorizontal();

        GUILayout.Label($"共 {_entries.Count} 筆 Pref", EditorStyles.miniLabel);
        int dirtyCount = _entries.Count(e => e.isDirty);
        if (dirtyCount > 0)
            GUILayout.Label($"（{dirtyCount} 筆未儲存）", _dirtyLabelStyle);

        GUILayout.FlexibleSpace();

        if (!_confirmClear)
        {
            GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
            if (GUILayout.Button("⚠ 清除所有 PlayerPrefs", GUILayout.Width(170)))
                _confirmClear = true;
            GUI.backgroundColor = Color.white;
        }
        else
        {
            GUILayout.Label("確定嗎？", EditorStyles.boldLabel);
            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("確定清除", GUILayout.Width(70)))
            {
                PlayerPrefs.DeleteAll();
                PlayerPrefs.Save();
                _entries.Clear();
                _confirmClear = false;
            }
            GUI.backgroundColor = Color.white;
            if (GUILayout.Button("取消", GUILayout.Width(50)))
                _confirmClear = false;
        }

        EditorGUILayout.EndHorizontal();
    }

    // ── 值欄位（依型別切換）─────────────────
    private void DrawValueField(PrefEntry e, params GUILayoutOption[] options)
    {
        switch (e.type)
        {
            case PrefType.Int:
                e.intValue = EditorGUILayout.IntField(e.intValue, options);
                break;
            case PrefType.Float:
                e.floValue = EditorGUILayout.FloatField(e.floValue, options);
                break;
            default:
                e.strValue = EditorGUILayout.TextField(e.strValue, options);
                break;
        }
    }
}
