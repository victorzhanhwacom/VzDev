// ============================================================
//  PrefixObjectFinder.cs
//  放置路徑：Assets/Editor/PrefixObjectFinder.cs
//  Unity Editor 選單：Tools > Prefix Object Finder
// ============================================================

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace VzDev.EditorTools
{
    /// <summary>
    /// Unity Editor 工具：前綴物件搜尋器，根據物件名稱的前綴字，在場景中搜尋並列出所有符合的 GameObject。
    /// </summary>
    public class PrefixObjectFinder : EditorWindow
    {
        // ── 狀態 ──────────────────────────────────────────────
        private int _prefixLength = 4;
        private string _derivedPrefix = string.Empty;
        private string _sourceName = string.Empty;
        private List<GameObject> _results = new();

        private Vector2 _scroll;

        // ── 樣式（延遲初始化，確保 GUI skin 已載入）──────────
        private GUIStyle _headerStyle;
        private GUIStyle _prefixBadgeStyle;
        private GUIStyle _resultRowStyle;
        private bool _stylesReady;

        // ── 開啟視窗 ──────────────────────────────────────────
        [MenuItem("Tools/Prefix Object Finder")]
        public static void Open()
        {
            var win = GetWindow<PrefixObjectFinder>("Prefix Finder");
            win.minSize = new Vector2(340, 420);
        }

        // ── GUI 主體 ──────────────────────────────────────────
        private void OnGUI()
        {
            InitStyles();

            DrawHeader();
            GUILayout.Space(8);
            DrawSourceSection();
            GUILayout.Space(6);
            DrawPrefixControl();
            GUILayout.Space(10);
            DrawActionButton();
            GUILayout.Space(10);
            DrawResults();
        }

        // ────────────────────────────────────────────────────────
        //  區塊繪製
        // ────────────────────────────────────────────────────────

        private void DrawHeader()
        {
            EditorGUILayout.LabelField("Prefix Object Finder", _headerStyle);
            var rect = GUILayoutUtility.GetLastRect();
            rect.y += EditorGUIUtility.singleLineHeight + 2;
            rect.height = 1;
            EditorGUI.DrawRect(rect, new Color(1f, 1f, 1f, 0.08f));
            GUILayout.Space(4);
        }

        private void DrawSourceSection()
        {
            // 即時讀取 Hierarchy 單選
            var active = Selection.activeGameObject;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("選取物件", EditorStyles.boldLabel);
                GUILayout.Space(2);

                if (active == null)
                {
                    EditorGUILayout.HelpBox("請在 Hierarchy 中單選一個物件。", MessageType.Info);
                    _sourceName = string.Empty;
                    _derivedPrefix = string.Empty;
                }
                else
                {
                    _sourceName = active.name;

                    // 計算前綴預覽
                    _derivedPrefix = _sourceName.Length >= _prefixLength
                        ? _sourceName[.._prefixLength]
                        : _sourceName;

                    EditorGUILayout.LabelField("物件名稱", _sourceName, EditorStyles.wordWrappedLabel);

                    // 前綴徽章
                    GUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("比對前綴", GUILayout.Width(64));
                    GUILayout.Label(_derivedPrefix, _prefixBadgeStyle);
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();
                }
            }
        }

        private void DrawPrefixControl()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("前綴長度 (x)", GUILayout.Width(100));

                // IntSlider — 最多限制到選取名稱長度（或預設 32）
                int maxLen = string.IsNullOrEmpty(_sourceName) ? 32 : Mathf.Max(1, _sourceName.Length);
                _prefixLength = EditorGUILayout.IntSlider(_prefixLength, 1, maxLen);
            }
        }

        private void DrawActionButton()
        {
            bool canSearch = Selection.activeGameObject != null;

            using (new EditorGUI.DisabledScope(!canSearch))
            {
                if (GUILayout.Button("搜尋場景中符合物件", GUILayout.Height(32)))
                    RunSearch();
            }
        }

        private void DrawResults()
        {
            if (_results.Count == 0 && string.IsNullOrEmpty(_derivedPrefix))
                return;

            // 標題列
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(
                    $"結果  ({_results.Count} 個符合 \"{_derivedPrefix}\")",
                    EditorStyles.boldLabel);

                if (_results.Count > 0 && GUILayout.Button("全選", GUILayout.Width(48)))
                    SelectAll();
            }

            EditorGUI.DrawRect(
                GUILayoutUtility.GetRect(1, 1, GUILayout.ExpandWidth(true)),
                new Color(1f, 1f, 1f, 0.08f));
            GUILayout.Space(4);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            for (int i = 0; i < _results.Count; i++)
            {
                var go = _results[i];
                if (go == null) continue;

                bool isSelected = Selection.objects.Contains(go);

                // 交替底色
                var rowRect = EditorGUILayout.BeginHorizontal(
                    isSelected ? _resultRowStyle : GUIStyle.none,
                    GUILayout.Height(22));

                // 物件 icon
                var icon = EditorGUIUtility.ObjectContent(go, typeof(GameObject)).image;
                GUILayout.Label(icon, GUILayout.Width(20), GUILayout.Height(20));

                // 名稱 — 點擊 Ping
                if (GUILayout.Button(go.name, EditorStyles.label))
                {
                    EditorGUIUtility.PingObject(go);
                    Selection.activeGameObject = go;
                }

                GUILayout.FlexibleSpace();

                // 階層路徑（小字）
                EditorGUILayout.LabelField(
                    GetPath(go),
                    EditorStyles.miniLabel,
                    GUILayout.MaxWidth(160));

                EditorGUILayout.EndHorizontal();

                // 懸浮高亮
                if (Event.current.type == EventType.MouseMove && rowRect.Contains(Event.current.mousePosition))
                    Repaint();
            }

            EditorGUILayout.EndScrollView();
        }

        // ────────────────────────────────────────────────────────
        //  核心邏輯
        // ────────────────────────────────────────────────────────

        private void RunSearch()
        {
            _results.Clear();

            if (string.IsNullOrEmpty(_derivedPrefix)) return;

            // 取得場景所有 GameObject（含 inactive）
            var allObjects = Resources.FindObjectsOfTypeAll<GameObject>()
                .Where(go => go.scene.IsValid() && !string.IsNullOrEmpty(go.scene.path));

            foreach (var go in allObjects)
            {
                if (go.name.StartsWith(_derivedPrefix, System.StringComparison.Ordinal))
                    _results.Add(go);
            }

            // 自動 Ping 第一個（排除自身）
            var others = _results.Where(g => g != Selection.activeGameObject).ToList();
            if (others.Count > 0)
                EditorGUIUtility.PingObject(others[0]);

            Debug.Log($"[PrefixFinder] prefix=\"{_derivedPrefix}\" → 找到 {_results.Count} 個物件");
        }

        private void SelectAll()
        {
            Selection.objects = _results.Where(g => g != null).Cast<Object>().ToArray();
        }

        // ────────────────────────────────────────────────────────
        //  工具函式
        // ────────────────────────────────────────────────────────

        private static string GetPath(GameObject go)
        {
            var parts = new List<string>();
            var t = go.transform;
            while (t != null) { parts.Add(t.name); t = t.parent; }
            parts.Reverse();
            string full = string.Join("/", parts);
            return full.Length > 40 ? "…" + full[^38..] : full;
        }

        private void InitStyles()
        {
            if (_stylesReady) return;

            _headerStyle = new GUIStyle(EditorStyles.largeLabel)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                padding = new RectOffset(4, 0, 6, 0),
            };

            _prefixBadgeStyle = new GUIStyle(EditorStyles.helpBox)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(10, 10, 2, 2),
                normal = { textColor = new Color(0.4f, 0.9f, 1f) },
            };

            _resultRowStyle = new GUIStyle(EditorStyles.helpBox)
            {
                margin = new RectOffset(0, 0, 1, 1),
                padding = new RectOffset(4, 4, 2, 2),
            };

            _stylesReady = true;
        }

        // 每次 Selection 改變時重繪，使前綴即時更新
        private void OnSelectionChange() => Repaint();
    }
}
