using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Unity Editor 擴充工具：Hierarchy 物件向上移動一階層
/// 熱鍵可在 Tools > Hierarchy Move Settings 自訂
/// </summary>
[InitializeOnLoad]
public static class HierarchyMoveUp
{
    // ──────────────────────────────────────────
    // 熱鍵設定的 PlayerPrefs Key（Editor 專用）
    // ──────────────────────────────────────────
    private const string PREF_KEYCODE  = "HMU_KeyCode";
    private const string PREF_CTRL     = "HMU_Ctrl";
    private const string PREF_SHIFT    = "HMU_Shift";
    private const string PREF_ALT      = "HMU_Alt";

    // 預設熱鍵：Ctrl + Up
    private static KeyCode  _keyCode = KeyCode.UpArrow;
    private static bool     _ctrl    = true;
    private static bool     _shift   = false;
    private static bool     _alt     = false;

    // ──────────────────────────────────────────
    // 初始化（Editor 啟動時自動執行）
    // ──────────────────────────────────────────
    static HierarchyMoveUp()
    {
        LoadHotkey();
        SceneView.duringSceneGui += OnSceneGUI;
        EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyGUI;
    }

    // ──────────────────────────────────────────
    // 讀寫熱鍵設定
    // ──────────────────────────────────────────
    public static void LoadHotkey()
    {
        _keyCode = (KeyCode)PlayerPrefs.GetInt(PREF_KEYCODE, (int)KeyCode.UpArrow);
        _ctrl    = PlayerPrefs.GetInt(PREF_CTRL,  1) == 1;
        _shift   = PlayerPrefs.GetInt(PREF_SHIFT, 0) == 1;
        _alt     = PlayerPrefs.GetInt(PREF_ALT,   0) == 1;
    }

    public static void SaveHotkey(KeyCode key, bool ctrl, bool shift, bool alt)
    {
        _keyCode = key;
        _ctrl    = ctrl;
        _shift   = shift;
        _alt     = alt;
        PlayerPrefs.SetInt(PREF_KEYCODE, (int)key);
        PlayerPrefs.SetInt(PREF_CTRL,   ctrl  ? 1 : 0);
        PlayerPrefs.SetInt(PREF_SHIFT,  shift ? 1 : 0);
        PlayerPrefs.SetInt(PREF_ALT,    alt   ? 1 : 0);
        PlayerPrefs.Save();
    }

    public static (KeyCode key, bool ctrl, bool shift, bool alt) GetHotkey()
        => (_keyCode, _ctrl, _shift, _alt);

    public static string HotkeyLabel()
    {
        var parts = new List<string>();
        if (_ctrl)  parts.Add("Ctrl");
        if (_shift) parts.Add("Shift");
        if (_alt)   parts.Add("Alt");
        parts.Add(_keyCode.ToString());
        return string.Join(" + ", parts);
    }

    // ──────────────────────────────────────────
    // 偵測熱鍵（Scene View）
    // ──────────────────────────────────────────
    private static void OnSceneGUI(SceneView sv)
    {
        DetectAndExecute();
    }

    // 也偵測 Hierarchy 視窗內的按鍵
    private static void OnHierarchyGUI(int instanceID, Rect selectionRect)
    {
        DetectAndExecute();
    }

    private static void DetectAndExecute()
    {
        var e = Event.current;
        if (e == null || e.type != EventType.KeyDown) return;

        bool ctrlMatch  = _ctrl  == (e.control || e.command);
        bool shiftMatch = _shift == e.shift;
        bool altMatch   = _alt   == e.alt;
        bool keyMatch   = e.keyCode == _keyCode;

        if (keyMatch && ctrlMatch && shiftMatch && altMatch)
        {
            MoveSelectionUp();
            e.Use(); // 消耗事件，避免觸發其他行為
        }
    }

    // ──────────────────────────────────────────
    // 核心：將選取物件移動到父層的父層
    // ──────────────────────────────────────────
    [MenuItem("VzDev/Hierarchy Move Up %UP")]   // 固定選單備用（Ctrl+Up）
    public static void MoveSelectionUp()
    {
        var selected = Selection.gameObjects;
        if (selected == null || selected.Length == 0)
        {
            Debug.LogWarning("[HierarchyMoveUp] 請先在 Hierarchy 選取物件。");
            return;
        }

        Undo.SetCurrentGroupName("Move Up in Hierarchy");
        int group = Undo.GetCurrentGroup();
        bool anyMoved = false;

        foreach (var go in selected)
        {
            var parent = go.transform.parent;
            if (parent == null)
            {
                Debug.LogWarning($"[HierarchyMoveUp] 「{go.name}」已在最上層，無法再往上移。");
                continue;
            }

            var grandParent = parent.parent; // null 代表移到 Scene 根層
            int siblingIndex = parent.GetSiblingIndex(); // 插入到原父層的位置後面

            Undo.SetTransformParent(go.transform, grandParent, "Move Up in Hierarchy");
            go.transform.SetSiblingIndex(siblingIndex + 1);
            anyMoved = true;
        }

        Undo.CollapseUndoOperations(group);

        if (anyMoved)
            EditorApplication.RepaintHierarchyWindow();
    }
}

/// <summary>
/// 熱鍵設定視窗
/// </summary>
public class HierarchyMoveSettings : EditorWindow
{
    private KeyCode _pendingKey;
    private bool    _pendingCtrl;
    private bool    _pendingShift;
    private bool    _pendingAlt;
    private bool    _isListening = false;

    [MenuItem("VzDev/Tools/Hierarchy Move Settings")]
    public static void ShowWindow()
    {
        var win = GetWindow<HierarchyMoveSettings>("⌨ Move Up 熱鍵設定");
        win.minSize = new Vector2(340, 220);
        win.maxSize = new Vector2(340, 220);
        // 載入目前設定
        var (key, ctrl, shift, alt) = HierarchyMoveUp.GetHotkey();
        win._pendingKey   = key;
        win._pendingCtrl  = ctrl;
        win._pendingShift = shift;
        win._pendingAlt   = alt;
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(10);
        GUILayout.Label("Hierarchy 向上移動一階層 — 熱鍵設定", EditorStyles.boldLabel);
        EditorGUILayout.Space(6);

        // 目前生效的熱鍵
        var style = new GUIStyle(EditorStyles.helpBox) { fontSize = 12, alignment = TextAnchor.MiddleCenter };
        EditorGUILayout.LabelField($"目前熱鍵：{HierarchyMoveUp.HotkeyLabel()}", style, GUILayout.Height(28));

        EditorGUILayout.Space(10);
        GUILayout.Label("修改熱鍵：", EditorStyles.boldLabel);

        // 修飾鍵
        EditorGUILayout.BeginHorizontal();
        _pendingCtrl  = GUILayout.Toggle(_pendingCtrl,  "Ctrl",  "Button", GUILayout.Height(28));
        _pendingShift = GUILayout.Toggle(_pendingShift, "Shift", "Button", GUILayout.Height(28));
        _pendingAlt   = GUILayout.Toggle(_pendingAlt,   "Alt",   "Button", GUILayout.Height(28));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(6);

        // 主鍵：下拉選單 或 錄製
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("主鍵：", GUILayout.Width(36));
        _pendingKey = (KeyCode)EditorGUILayout.EnumPopup(_pendingKey);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(4);

        // 錄製按鈕
        GUI.backgroundColor = _isListening ? Color.yellow : Color.white;
        if (GUILayout.Button(_isListening ? "🔴 按下任意鍵以錄製..." : "🎙 點此錄製按鍵", GUILayout.Height(28)))
            _isListening = !_isListening;
        GUI.backgroundColor = Color.white;

        if (_isListening) WatchForKey();

        EditorGUILayout.Space(10);

        // 預覽
        string preview = BuildPreview();
        EditorGUILayout.LabelField($"預覽：{preview}",
            new GUIStyle(EditorStyles.centeredGreyMiniLabel) { fontSize = 11 });

        EditorGUILayout.Space(6);

        // 套用 / 重置
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("✅ 套用", GUILayout.Height(30)))
        {
            HierarchyMoveUp.SaveHotkey(_pendingKey, _pendingCtrl, _pendingShift, _pendingAlt);
            ShowNotification(new GUIContent($"熱鍵已更新：{HierarchyMoveUp.HotkeyLabel()}"));
        }
        if (GUILayout.Button("↩ 恢復預設 (Ctrl+↑)", GUILayout.Height(30)))
        {
            _pendingKey   = KeyCode.UpArrow;
            _pendingCtrl  = true;
            _pendingShift = false;
            _pendingAlt   = false;
            HierarchyMoveUp.SaveHotkey(_pendingKey, _pendingCtrl, _pendingShift, _pendingAlt);
            ShowNotification(new GUIContent("已恢復預設熱鍵"));
        }
        EditorGUILayout.EndHorizontal();
    }

    private void WatchForKey()
    {
        var e = Event.current;
        if (e == null) return;

        // 忽略純修飾鍵按下
        if (e.type == EventType.KeyDown &&
            e.keyCode != KeyCode.None &&
            e.keyCode != KeyCode.LeftControl  && e.keyCode != KeyCode.RightControl &&
            e.keyCode != KeyCode.LeftShift    && e.keyCode != KeyCode.RightShift   &&
            e.keyCode != KeyCode.LeftAlt      && e.keyCode != KeyCode.RightAlt     &&
            e.keyCode != KeyCode.LeftCommand  && e.keyCode != KeyCode.RightCommand)
        {
            _pendingKey   = e.keyCode;
            _pendingCtrl  = e.control || e.command;
            _pendingShift = e.shift;
            _pendingAlt   = e.alt;
            _isListening  = false;
            e.Use();
            Repaint();
        }
    }

    private string BuildPreview()
    {
        var parts = new List<string>();
        if (_pendingCtrl)  parts.Add("Ctrl");
        if (_pendingShift) parts.Add("Shift");
        if (_pendingAlt)   parts.Add("Alt");
        parts.Add(_pendingKey.ToString());
        return string.Join(" + ", parts);
    }
}
