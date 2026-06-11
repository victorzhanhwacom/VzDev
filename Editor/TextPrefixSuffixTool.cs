// 放置路徑：Assets/Editor/TextPrefixSuffixTool.cs
// 支援 Legacy UI.Text、TextMeshProUGUI、TextMeshPro（3D）
// 使用 System.Type 反射偵測 TMP，不依賴任何 Scripting Define Symbol

using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Reflection;

public class TextPrefixSuffixTool : EditorWindow
{
    // ── TMP 型別（啟動時反射取得，找不到就 null） ────────
    private static readonly Type s_tmpUGUIType =
        Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro") ??
        Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro.Runtime") ??
        FindTypeInAllAssemblies("TMPro.TextMeshProUGUI");

    private static readonly Type s_tmp3DType =
        Type.GetType("TMPro.TextMeshPro, Unity.TextMeshPro") ??
        Type.GetType("TMPro.TextMeshPro, Unity.TextMeshPro.Runtime") ??
        FindTypeInAllAssemblies("TMPro.TextMeshPro");

    // ── 輸入欄位 ──────────────────────────────────────────
    private string prefix      = "";
    private string suffix      = "";
    private int    targetIndex = 1;   // 1-based

    // ── 預覽 ──────────────────────────────────────────────
    private Vector2 scrollPos;
    private Vector2 selectionScrollPos;

    private const string UNDO_NAME = "Add Text Prefix/Suffix";

    // ═════════════════════════════════════════════════════
    //  開啟視窗
    // ═════════════════════════════════════════════════════
    [MenuItem("Tools/Text Prefix & Suffix Tool")]
    public static void ShowWindow()
    {
        var w = GetWindow<TextPrefixSuffixTool>("Text Prefix / Suffix");
        w.minSize = new Vector2(620, 380);
    }

    // ═════════════════════════════════════════════════════
    //  GUI
    // ═════════════════════════════════════════════════════
    private void OnGUI()
    {
        DrawHeader();
        EditorGUILayout.Space(6);

        var allFound = CollectAllTextComponentsPerObject();
        var targets  = PickTargetsByIndex(allFound, targetIndex);

        // 左右分割
        EditorGUILayout.BeginHorizontal();

        // 左欄：設定（固定）+ 選取列表（可捲動）+ 按鈕（釘底）
        EditorGUILayout.BeginVertical(GUILayout.Width(300), GUILayout.ExpandHeight(true));
        DrawInputSection();
        EditorGUILayout.Space(8);
        DrawSelectionInfo(allFound, targets);  // 內部含 ScrollView，不會撐大
        GUILayout.FlexibleSpace();
        DrawActionButtons(targets);            // 永遠在左欄底部
        EditorGUILayout.EndVertical();

        // 垂直分隔線
        var lineRect = EditorGUILayout.GetControlRect(false, GUILayout.Width(1), GUILayout.ExpandHeight(true));
        EditorGUI.DrawRect(lineRect, new Color(0.4f, 0.4f, 0.4f));

        // 右欄：預覽（佔滿剩餘寬度）
        EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        DrawPreviewSection(targets);
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();
    }

    // ─── 標題 ─────────────────────────────────────────────
    private void DrawHeader()
    {
        var style = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize  = 13,
            alignment = TextAnchor.MiddleCenter
        };
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Text Prefix / Suffix Tool", style);

        // 顯示 TMP 偵測狀態
        string tmpStatus = (s_tmpUGUIType != null)
            ? "TMP 已偵測到 ✓"
            : "TMP 未安裝（僅支援 Legacy Text）";
        var subStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel);
        EditorGUILayout.LabelField(tmpStatus, subStyle);

        DrawHorizontalLine(Color.gray);
    }

    // ─── 輸入欄位 ─────────────────────────────────────────
    private void DrawInputSection()
    {
        EditorGUILayout.LabelField("設定", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");

        EditorGUI.BeginChangeCheck();

        prefix = EditorGUILayout.TextField(
            new GUIContent("前綴 (Prefix)", "加在文字最前面的字串"), prefix);
        suffix = EditorGUILayout.TextField(
            new GUIContent("後綴 (Suffix)", "加在文字最後面的字串"), suffix);

        EditorGUILayout.Space(4);

        int newIdx = EditorGUILayout.IntField(
            new GUIContent("目標第幾個 (Index)",
                "對每個選取物件，depth-first 掃描子物件，取第 N 個文字組件（1 = 最上層第一個）"),
            targetIndex);
        targetIndex = Mathf.Max(1, newIdx);

        if (EditorGUI.EndChangeCheck()) Repaint();

        EditorGUILayout.EndVertical();
    }

    // ─── 選取狀態資訊 ─────────────────────────────────────
    private void DrawSelectionInfo(
        Dictionary<GameObject, List<TextComponentWrapper>> allFound,
        List<TextComponentWrapper> targets)
    {
        DrawHorizontalLine(new Color(0.4f, 0.4f, 0.4f));
        EditorGUILayout.LabelField("目前選取", EditorStyles.boldLabel);

        int selCount = Selection.gameObjects != null ? Selection.gameObjects.Length : 0;

        if (selCount == 0)
        {
            EditorGUILayout.HelpBox(
                "請在 Hierarchy 選取物件。支援多選，會 depth-first 掃描子物件的文字組件。",
                MessageType.Info);
            return;
        }

        if (allFound.Count == 0)
        {
            EditorGUILayout.HelpBox(
                $"已選 {selCount} 個物件，但找不到任何文字組件（含子物件）。",
                MessageType.Warning);
            return;
        }

        // ── 列表：限制最大高度，超過就 scroll ────────────
        selectionScrollPos = EditorGUILayout.BeginScrollView(
            selectionScrollPos, GUILayout.MaxHeight(200));
        EditorGUILayout.BeginVertical("box");
        foreach (var kv in allFound)
        {
            int  count    = kv.Value.Count;
            bool idxValid = targetIndex <= count;
            string picked = idxValid
                ? $"  →  #{targetIndex}：{kv.Value[targetIndex - 1].Name}"
                : $"  →  ⚠ 只有 {count} 個，Index {targetIndex} 超出範圍";

            var s = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = idxValid
                    ? EditorGUIUtility.isProSkin ? Color.white : Color.black
                    : new Color(1f, 0.6f, 0.2f) }
            };
            EditorGUILayout.LabelField($"{kv.Key.name}（共 {count} 個）{picked}", s);
        }
        EditorGUILayout.EndVertical();
        EditorGUILayout.EndScrollView();

        // ── 摘要 HelpBox（固定在列表下方）────────────────
        if (targets.Count > 0)
            EditorGUILayout.HelpBox(
                $"將套用至 {targets.Count} 個物件的第 {targetIndex} 個文字組件。",
                MessageType.None);
        else
            EditorGUILayout.HelpBox(
                $"所有選取物件的文字組件數量均少於 {targetIndex}，無可套用目標。",
                MessageType.Warning);
    }

    // ─── 預覽（右欄，填滿高度） ──────────────────────────
    private void DrawPreviewSection(List<TextComponentWrapper> targets)
    {
        EditorGUILayout.LabelField("預覽結果", EditorStyles.boldLabel);
        DrawHorizontalLine(Color.gray);
        EditorGUILayout.Space(4);

        if (targets.Count == 0)
        {
            EditorGUILayout.HelpBox("選取物件並設定前綴／後綴後，這裡會顯示套用結果。", MessageType.Info);
            return;
        }

        // 填滿右欄剩餘高度
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.ExpandHeight(true));

        foreach (var t in targets)
        {
            EditorGUILayout.BeginVertical("box");

            // 組件名稱（tooltip 顯示完整名稱）
            EditorGUILayout.LabelField(
                new GUIContent(t.Name, t.Name),
                EditorStyles.miniBoldLabel);

            EditorGUILayout.BeginHorizontal();

            // 原始文字
            var origStyle = new GUIStyle(EditorStyles.label)
            {
                wordWrap  = true,
                fontStyle = FontStyle.Normal,
                normal    = { textColor = new Color(0.7f, 0.7f, 0.7f) }
            };
            EditorGUILayout.LabelField(t.GetText(), origStyle, GUILayout.ExpandWidth(true));

            EditorGUILayout.LabelField("→", GUILayout.Width(20));

            // 預覽結果（高亮）
            var previewStyle = new GUIStyle(EditorStyles.label)
            {
                wordWrap  = true,
                fontStyle = FontStyle.Bold,
                normal    = { textColor = new Color(0.4f, 0.9f, 0.6f) }
            };
            EditorGUILayout.LabelField(BuildText(t.GetText()), previewStyle, GUILayout.ExpandWidth(true));

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2);
        }

        EditorGUILayout.EndScrollView();
    }

    // ─── 按鈕 ─────────────────────────────────────────────
    private void DrawActionButtons(List<TextComponentWrapper> targets)
    {
        DrawHorizontalLine(new Color(0.4f, 0.4f, 0.4f));

        bool hasInput = (prefix != null && prefix.Length > 0) ||
                        (suffix != null && suffix.Length > 0);
        bool canApply = targets.Count > 0 && hasInput;

        using (new EditorGUI.DisabledScope(!canApply))
        {
            if (GUILayout.Button("套用 Apply", GUILayout.Height(36)))
                ApplyChanges(targets);
        }

        EditorGUILayout.Space(4);

        if (GUILayout.Button("清空欄位 Clear", GUILayout.Height(28)))
        {
            prefix = ""; suffix = "";
            GUI.FocusControl(null);
            Repaint();
        }

        EditorGUILayout.Space(4);

        if (!hasInput && targets.Count > 0)
            EditorGUILayout.HelpBox("請至少輸入前綴或後綴其中一項。", MessageType.Warning);
    }

    // ═════════════════════════════════════════════════════
    //  核心：depth-first 收集每個選取物件底下的所有文字組件
    // ═════════════════════════════════════════════════════
    private Dictionary<GameObject, List<TextComponentWrapper>>
        CollectAllTextComponentsPerObject()
    {
        var result = new Dictionary<GameObject, List<TextComponentWrapper>>();
        if (Selection.gameObjects == null) return result;

        foreach (var go in Selection.gameObjects)
        {
            if (go == null) continue;
            var list = new List<TextComponentWrapper>();
            GatherDFS(go.transform, list);
            if (list.Count > 0)
                result[go] = list;
        }
        return result;
    }

    private void GatherDFS(Transform tf, List<TextComponentWrapper> list)
    {
        var w = GetTextWrapper(tf.gameObject);
        if (w != null) list.Add(w);

        for (int i = 0; i < tf.childCount; i++)
            GatherDFS(tf.GetChild(i), list);
    }

    /// <summary>
    /// 用反射取得單一 GameObject 上的文字組件（不往子物件找）。
    /// 優先順序：UI.Text → TextMeshProUGUI → TextMeshPro
    /// </summary>
    private TextComponentWrapper GetTextWrapper(GameObject go)
    {
        // Legacy UI.Text
        var legacyText = go.GetComponent<UnityEngine.UI.Text>();
        if (legacyText != null) return new TextComponentWrapper(legacyText);

        // TextMeshProUGUI（反射）
        if (s_tmpUGUIType != null)
        {
            var c = go.GetComponent(s_tmpUGUIType);
            if (c != null) return new TextComponentWrapper(c, s_tmpUGUIType);
        }

        // TextMeshPro 3D（反射）
        if (s_tmp3DType != null)
        {
            var c = go.GetComponent(s_tmp3DType);
            if (c != null) return new TextComponentWrapper(c, s_tmp3DType);
        }

        return null;
    }

    // ─── Index 篩選 ───────────────────────────────────────
    private List<TextComponentWrapper> PickTargetsByIndex(
        Dictionary<GameObject, List<TextComponentWrapper>> allFound, int index)
    {
        var result  = new List<TextComponentWrapper>();
        int zeroIdx = index - 1;
        foreach (var kv in allFound)
            if (zeroIdx < kv.Value.Count)
                result.Add(kv.Value[zeroIdx]);
        return result;
    }

    // ═════════════════════════════════════════════════════
    //  套用（支援 Undo）
    // ═════════════════════════════════════════════════════
    private void ApplyChanges(List<TextComponentWrapper> targets)
    {
        Undo.SetCurrentGroupName(UNDO_NAME);
        int group = Undo.GetCurrentGroup();

        foreach (var t in targets)
        {
            Undo.RecordObject(t.Component, UNDO_NAME);
            t.SetText(BuildText(t.GetText()));
            EditorUtility.SetDirty(t.Component);
        }

        Undo.CollapseUndoOperations(group);
        Debug.Log($"[TextPrefixSuffixTool] 套用至 {targets.Count} 個組件（第 {targetIndex} 個）。" +
                  $"前綴：\"{prefix}\"  後綴：\"{suffix}\"");
    }

    // ═════════════════════════════════════════════════════
    //  工具
    // ═════════════════════════════════════════════════════
    private string BuildText(string original) => $"{prefix}{original}{suffix}";

    private static void DrawHorizontalLine(Color color)
    {
        var rect = EditorGUILayout.GetControlRect(false, 1);
        EditorGUI.DrawRect(rect, color);
    }

    private void OnSelectionChange() => Repaint();

    /// <summary>掃描所有已載入 Assembly 找型別（TMP assembly 名稱不固定時的保底）</summary>
    private static Type FindTypeInAllAssemblies(string fullTypeName)
    {
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            var t = asm.GetType(fullTypeName);
            if (t != null) return t;
        }
        return null;
    }

    // ═════════════════════════════════════════════════════
    //  包裝類：統一處理 Legacy Text / TMP（反射存取 .text）
    // ═════════════════════════════════════════════════════
    private class TextComponentWrapper
    {
        public readonly UnityEngine.Object Component;
        public readonly string             Name;

        private readonly Func<string>   _getter;
        private readonly Action<string> _setter;

        // Legacy UI.Text
        public TextComponentWrapper(UnityEngine.UI.Text t)
        {
            Component = t;
            Name      = $"{t.gameObject.name} (UI.Text)";
            _getter   = () => t.text;
            _setter   = v => t.text = v;
        }

        // TMP（透過反射存取 .text property）
        public TextComponentWrapper(UnityEngine.Component c, Type type)
        {
            Component = c;
            Name      = $"{c.gameObject.name} ({type.Name})";

            var prop = type.GetProperty("text",
                BindingFlags.Public | BindingFlags.Instance);

            _getter = () => prop?.GetValue(c) as string ?? "";
            _setter = v => prop?.SetValue(c, v);
        }

        public string GetText()         => _getter();
        public void   SetText(string v) => _setter(v);
    }
}
