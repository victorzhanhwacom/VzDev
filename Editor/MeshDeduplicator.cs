using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

/// <summary>
/// DCIM Mesh Deduplicator（幾何矩版）
///
/// 使用旋轉不變的幾何簽名（MeshSignature），正確識別
/// 「形狀相同但旋轉不同（含 90°/180°/270° 整數倍）」的 Mesh 並統一參照。
///
/// 放置位置：Assets/Editor/MeshDeduplicator.cs
/// 使用方式：Unity 選單 Tools > DCIM > Mesh Deduplicator
/// </summary>
public class MeshDeduplicator : EditorWindow
{
    // ── UI 狀態 ──────────────────────────────────────────────
    private GameObject _rootObject;
    private bool       _dryRun        = true;
    private bool       _applyToScene  = true;
    private bool       _saveMeshAsset = true;
    private string     _savePath      = "Assets/DCIM/DeduplicatedMeshes";
    private Vector2    _scroll;

    // ── 分析結果 ─────────────────────────────────────────────
    private List<MeshGroup> _groups;
    private int             _totalMeshes;
    private int             _uniqueMeshes;
    private int             _duplicatesRemoved;

    // ─────────────────────────────────────────────────────────
    [MenuItem("Tools/DCIM/Mesh Deduplicator")]
    public static void ShowWindow()
    {
        var w = GetWindow<MeshDeduplicator>("Mesh Deduplicator");
        w.minSize = new Vector2(440, 580);
    }

    // ── GUI ───────────────────────────────────────────────────
    [System.Obsolete]
    private void OnGUI()
    {
        EditorGUILayout.Space(8);
        GUILayout.Label("DCIM Mesh Deduplicator", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "使用旋轉不變的幾何矩簽名，識別形狀相同但旋轉不同（含 90°/180°/270°）的 Mesh 並統一參照，降低 draw call 數量。",
            MessageType.Info);

        EditorGUILayout.Space(6);

        _rootObject = (GameObject)EditorGUILayout.ObjectField(
            "根物件（留空 = 整個場景）", _rootObject, typeof(GameObject), true);

        EditorGUILayout.Space(4);
        GUILayout.Label("選項", EditorStyles.boldLabel);
        _dryRun        = EditorGUILayout.Toggle("僅預覽（Dry Run）",        _dryRun);
        _applyToScene  = EditorGUILayout.Toggle("套用至場景物件",             _applyToScene);
        _saveMeshAsset = EditorGUILayout.Toggle("儲存去重後 Mesh 為 Asset",  _saveMeshAsset);
        if (_saveMeshAsset)
            _savePath = EditorGUILayout.TextField("儲存路徑", _savePath);

        EditorGUILayout.Space(8);

        if (GUILayout.Button("🔍  掃描 Mesh 重複狀況", GUILayout.Height(36)))
            Analyze();

        if (_groups == null) return;

        // ── 結果顯示 ─────────────────────────────────────────
        EditorGUILayout.Space(6);
        GUILayout.Label("── 掃描結果 ──", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"總 Mesh 數量：{_totalMeshes}");
        EditorGUILayout.LabelField($"唯一 Mesh 數：{_uniqueMeshes}");
        EditorGUILayout.LabelField(
            $"可合併重複數：{_duplicatesRemoved}",
            _duplicatesRemoved > 0 ? EditorStyles.boldLabel : EditorStyles.label);

        EditorGUILayout.Space(4);
        _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.Height(200));
        foreach (var g in _groups.Where(g => g.Members.Count > 1))
        {
            EditorGUILayout.LabelField(
                $"▶ 群組（{g.Members.Count} 個相同 mesh）：", EditorStyles.boldLabel);
            foreach (var m in g.Members)
                EditorGUILayout.LabelField($"    {m.renderer.name}  [{m.originalMesh.name}]");
        }
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(4);
        GUI.enabled = _duplicatesRemoved > 0;
        if (GUILayout.Button(
                _dryRun ? "⚠️  Dry Run 模式 — 按此切換為套用" : "✅  套用去重",
                GUILayout.Height(36)))
        {
            if (_dryRun)
                _dryRun = false;    // 第一次點：解除 dry run
            else
                Apply();            // 第二次點：真正套用
            Repaint();
        }
        GUI.enabled = true;
    }

    // ── 掃描 ─────────────────────────────────────────────────
    [System.Obsolete]
    private void Analyze()
    {
        _groups = new List<MeshGroup>();

        MeshFilter[] filters = _rootObject != null
            ? _rootObject.GetComponentsInChildren<MeshFilter>(true)
            : FindObjectsOfType<MeshFilter>();

        _totalMeshes       = filters.Length;
        _duplicatesRemoved = 0;

        var sigDict = new Dictionary<string, MeshGroup>();

        // signature 快取：同一個 mesh asset 不重複計算
        var sigCache = new Dictionary<int, string>();

        int processed = 0;
        foreach (var mf in filters)
        {
            processed++;
            if (processed % 50 == 0)
                EditorUtility.DisplayProgressBar(
                    "掃描中...", $"{processed}/{_totalMeshes}", (float)processed / _totalMeshes);

            var mesh = mf.sharedMesh;
            if (mesh == null) continue;
            var renderer = mf.GetComponent<MeshRenderer>();
            if (renderer == null) continue;

            int meshId = mesh.GetInstanceID();
            if (!sigCache.TryGetValue(meshId, out string sig))
            {
                sig = MeshSignature.Compute(mesh);
                sigCache[meshId] = sig;
            }

            if (!sigDict.TryGetValue(sig, out var group))
            {
                group = new MeshGroup { CanonicalMesh = mesh };
                sigDict[sig] = group;
                _groups.Add(group);
            }

            group.Members.Add(new MeshMember
            {
                filter       = mf,
                renderer     = renderer,
                originalMesh = mesh
            });
        }

        EditorUtility.ClearProgressBar();

        _uniqueMeshes = _groups.Count;
        foreach (var g in _groups.Where(g => g.Members.Count > 1))
            _duplicatesRemoved += g.Members.Count - 1;

        Debug.Log($"[MeshDeduplicator] 掃描完成：{_totalMeshes} 個 mesh，" +
                  $"{_uniqueMeshes} 種唯一形狀，可減少 {_duplicatesRemoved} 個重複參照");
        Repaint();
    }

    // ── 套用 ─────────────────────────────────────────────────
    private void Apply()
    {
        if (_groups == null)
        {
            Debug.LogWarning("[MeshDeduplicator] 請先執行掃描");
            return;
        }

        if (_saveMeshAsset)
            EnsureDirectory(_savePath);

        int replaced = 0;

        foreach (var group in _groups.Where(g => g.Members.Count > 1))
        {
            Mesh canonical = group.CanonicalMesh;

            // 另存 canonical mesh asset（避免日後 FBX reimport 覆蓋）
            if (_saveMeshAsset)
            {
                string assetPath = $"{_savePath}/{SanitizeFileName(canonical.name)}_dedup.asset";
                var existing = AssetDatabase.LoadAssetAtPath<Mesh>(assetPath);
                if (existing == null)
                {
                    var copy = UnityEngine.Object.Instantiate(canonical);
                    copy.name = canonical.name + "_dedup";
                    AssetDatabase.CreateAsset(copy, assetPath);
                    canonical = copy;
                }
                else
                {
                    canonical = existing;
                }
                group.CanonicalMesh = canonical;
            }

            // 從第二個 member 開始替換（第一個本身就是 canonical）
            for (int i = 1; i < group.Members.Count; i++)
            {
                var member = group.Members[i];
                if (ReferenceEquals(member.originalMesh, canonical)) continue;

                Undo.RecordObject(member.filter, "MeshDeduplicator: replace mesh");
                member.filter.sharedMesh = canonical;
                EditorUtility.SetDirty(member.filter);
                replaced++;

                Debug.Log($"[MeshDeduplicator] 替換：{member.renderer.name} " +
                          $"[{member.originalMesh.name}] → [{canonical.name}]");
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[MeshDeduplicator] 完成！共替換 {replaced} 個重複 mesh 參照");
        _dryRun = true; // 套用後重置為 dry run，防止誤觸
    }

    // ── 工具方法 ─────────────────────────────────────────────
    private static void EnsureDirectory(string path)
    {
        string[] parts = path.Split('/');
        string cur = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = cur + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(cur, parts[i]);
            cur = next;
        }
    }

    private static string SanitizeFileName(string s)
    {
        // 1. 替換非法字元
        foreach (char c in System.IO.Path.GetInvalidFileNameChars())
            s = s.Replace(c, '_');

        // 2. 移除已有的 _dedup 後綴，避免重複套用時產生 _dedup_dedup
        if (s.EndsWith("_dedup"))
            s = s.Substring(0, s.Length - 6);

        // 3. 計算完整 asset 路徑的 byte 長度，預留 "_dedup.asset"（12 bytes）和路徑前綴
        //    Unity 限制單一檔名 250 bytes（UTF-8），這裡保守取 180 bytes 給檔名本體
        const int MaxFileNameBytes = 180;
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(s);
        if (bytes.Length > MaxFileNameBytes)
        {
            // 截斷到 MaxFileNameBytes，並附上原始名稱的短 hash 確保唯一性
            int hash = s.GetHashCode();
            string hashStr = ((uint)hash).ToString("X8"); // 8 碼十六進位
            // 截斷時需小心 UTF-8 多位元組字元邊界
            s = TruncateUtf8(s, MaxFileNameBytes - hashStr.Length - 1) + "_" + hashStr;
        }

        return s;
    }

    /// <summary>安全截斷 UTF-8 字串到指定 byte 上限，不切斷多位元組字元</summary>
    private static string TruncateUtf8(string s, int maxBytes)
    {
        var enc   = System.Text.Encoding.UTF8;
        byte[] buf = enc.GetBytes(s);
        if (buf.Length <= maxBytes) return s;

        // 從 maxBytes 往前找合法的字元邊界
        int cut = maxBytes;
        while (cut > 0 && (buf[cut] & 0xC0) == 0x80) cut--;
        return enc.GetString(buf, 0, cut);
    }

    // ── 資料結構 ─────────────────────────────────────────────
    private class MeshGroup
    {
        public Mesh             CanonicalMesh;
        public List<MeshMember> Members = new List<MeshMember>();
    }

    private class MeshMember
    {
        public MeshFilter   filter;
        public MeshRenderer renderer;
        public Mesh         originalMesh;
    }
}
