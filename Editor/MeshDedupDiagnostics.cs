using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

/// <summary>
/// Mesh 去重診斷工具
/// 放在 Editor/ 資料夾，選單：Tools > DCIM > Mesh Dedup Diagnostics
/// </summary>
public class MeshDedupDiagnostics : EditorWindow
{
    private GameObject _rootObject;
    private Vector2    _scroll;
    private string     _report = "";

    [MenuItem("Tools/DCIM/Mesh Dedup Diagnostics")]
    public static void ShowWindow()
    {
        var w = GetWindow<MeshDedupDiagnostics>("Mesh Dedup Diagnostics");
        w.minSize = new Vector2(500, 600);
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(8);
        GUILayout.Label("Mesh 去重診斷工具", EditorStyles.boldLabel);

        _rootObject = (GameObject)EditorGUILayout.ObjectField(
            "根物件（留空 = 整個場景）", _rootObject, typeof(GameObject), true);

        EditorGUILayout.Space(6);

        if (GUILayout.Button("① 檢查：套用後 mesh 參照是否真的統一", GUILayout.Height(32)))
            CheckMeshReferences();

        if (GUILayout.Button("② 檢查：SRP Batcher 相容性（shader/keyword）", GUILayout.Height(32)))
            CheckSRPBatcherCompatibility();

        if (GUILayout.Button("③ 檢查：樓梯/牆壁的 mesh signature 比對", GUILayout.Height(32)))
            CheckStairWallSignatures();

        if (GUILayout.Button("④ 全部診斷一次執行", GUILayout.Height(36)))
        {
            _report = "";
            CheckMeshReferences();
            CheckSRPBatcherCompatibility();
            CheckStairWallSignatures();
        }

        EditorGUILayout.Space(4);
        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        EditorGUILayout.TextArea(_report, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();

        if (GUILayout.Button("複製 Report 到剪貼簿"))
            EditorGUIUtility.systemCopyBuffer = _report;
    }

    // ── ① mesh 參照統一性檢查 ────────────────────────────────
    private void CheckMeshReferences()
    {
        Log("=== ① Mesh 參照統一性檢查 ===\n");

        MeshFilter[] filters = GetFilters();
        var meshGroups = new Dictionary<string, List<(MeshFilter mf, Mesh mesh)>>();

        foreach (var mf in filters)
        {
            var mesh = mf.sharedMesh;
            if (mesh == null) continue;

            // 用 MeshSignature 計算，理論上相同形狀應在同一組
            string sig = MeshSignature.Compute(mesh);
            if (!meshGroups.ContainsKey(sig))
                meshGroups[sig] = new List<(MeshFilter, Mesh)>();
            meshGroups[sig].Add((mf, mesh));
        }

        int problemGroups = 0;
        foreach (var kvp in meshGroups.Where(g => g.Value.Count > 1))
        {
            // 檢查同組內是否仍有不同的 mesh instance
            var distinctMeshes = kvp.Value.Select(x => x.mesh.GetInstanceID()).Distinct().ToList();
            if (distinctMeshes.Count > 1)
            {
                problemGroups++;
                Log($"[未去重群組] signature 相同但仍有 {distinctMeshes.Count} 種不同 mesh instance：");
                foreach (var (mf, mesh) in kvp.Value)
                    Log($"    {mf.name}  mesh='{mesh.name}'  instanceID={mesh.GetInstanceID()}");
                Log("");
            }
        }

        if (problemGroups == 0)
            Log("✅ 所有相同 signature 的 mesh 都已統一參照，去重套用成功。\n");
        else
            Log($"❌ 有 {problemGroups} 個群組去重未成功（見上方），mesh 參照仍然不同。\n");
    }

    // ── ② SRP Batcher 相容性檢查 ────────────────────────────
    private void CheckSRPBatcherCompatibility()
    {
        Log("=== ② SRP Batcher 相容性檢查 ===\n");

        MeshRenderer[] renderers = _rootObject != null
            ? _rootObject.GetComponentsInChildren<MeshRenderer>(true)
            : FindObjectsOfType<MeshRenderer>();

        // 按 shader 分組
        var shaderGroups = new Dictionary<string, int>();
        var nonBatchable = new List<string>();

        foreach (var r in renderers)
        {
            if (r.sharedMaterial == null)
            {
                nonBatchable.Add($"{r.name}：無 Material");
                continue;
            }

            var shader = r.sharedMaterial.shader;
            if (shader == null)
            {
                nonBatchable.Add($"{r.name}：無 Shader");
                continue;
            }

            string sName = shader.name;
            if (!shaderGroups.ContainsKey(sName)) shaderGroups[sName] = 0;
            shaderGroups[sName]++;
        }

        Log("場景中使用的 Shader 種類（每種 shader 都是獨立的 SRP Batcher 合批邊界）：");
        foreach (var kvp in shaderGroups.OrderByDescending(x => x.Value))
            Log($"    {kvp.Value,5} 個物件  →  {kvp.Key}");

        if (shaderGroups.Count > 1)
            Log($"\n⚠️  有 {shaderGroups.Count} 種不同 Shader，不同 Shader 之間無法合批。\n" +
                "    建議：將所有構件統一使用同一個 Shader（如 Universal Render Pipeline/Lit）。\n");
        else
            Log("\n✅ 所有物件使用相同 Shader，Shader 層面可合批。\n");

        // Material instance 重複性檢查
        var matGroups = new Dictionary<int, (string name, int count)>();
        foreach (var r in renderers)
        {
            if (r.sharedMaterial == null) continue;
            int id = r.sharedMaterial.GetInstanceID();
            if (!matGroups.ContainsKey(id))
                matGroups[id] = (r.sharedMaterial.name, 0);
            matGroups[id] = (matGroups[id].name, matGroups[id].count + 1);
        }

        Log($"Material instance 種類數：{matGroups.Count}（越少越好）");
        var topMats = matGroups.OrderByDescending(x => x.Value.count).Take(10);
        foreach (var kvp in topMats)
            Log($"    使用 {kvp.Value.count,5} 次  →  '{kvp.Value.name}'  (instanceID={kvp.Key})");
        Log("");
    }

    // ── ③ 樓梯 / 牆壁 signature 比對 ────────────────────────
    private void CheckStairWallSignatures()
    {
        Log("=== ③ 樓梯 / 牆壁 Mesh Signature 比對 ===\n");

        MeshFilter[] filters = GetFilters();

        // 找出名稱含關鍵字的物件
        string[] keywords = { "Stair", "Wall", "樓梯", "牆", "階梯", "Wall", "stair" };

        var suspects = filters
            .Where(mf => mf.sharedMesh != null &&
                         keywords.Any(k => mf.name.IndexOf(k, System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                                          mf.sharedMesh.name.IndexOf(k, System.StringComparison.OrdinalIgnoreCase) >= 0))
            .ToList();

        if (suspects.Count == 0)
        {
            Log("找不到名稱含 Stair/Wall/樓梯/牆 的物件，請手動選取後執行。\n");
            return;
        }

        Log($"找到 {suspects.Count} 個疑似樓梯/牆壁物件，列出其 mesh signature 組成：\n");

        // 按 signature 分組，看哪些應該相同但沒被合併
        var sigGroups = new Dictionary<string, List<MeshFilter>>();
        foreach (var mf in suspects)
        {
            string sig = MeshSignature.Compute(mf.sharedMesh);
            if (!sigGroups.ContainsKey(sig)) sigGroups[sig] = new List<MeshFilter>();
            sigGroups[sig].Add(mf);
        }

        int sameSignatureDiffMesh = 0;
        foreach (var kvp in sigGroups.OrderByDescending(g => g.Value.Count))
        {
            var distinctMeshes = kvp.Value.Select(mf => mf.sharedMesh.GetInstanceID()).Distinct().ToList();
            string status = distinctMeshes.Count == 1 ? "✅ 已統一" : $"❌ 仍有 {distinctMeshes.Count} 種 mesh";
            Log($"[Signature 群組 | {kvp.Value.Count} 個物件 | {status}]");
            foreach (var mf in kvp.Value.Take(5)) // 最多顯示 5 個
                Log($"    {mf.name}  mesh='{mf.sharedMesh.name}'  " +
                    $"rotation={mf.transform.rotation.eulerAngles}  " +
                    $"instanceID={mf.sharedMesh.GetInstanceID()}");
            if (kvp.Value.Count > 5)
                Log($"    ...（省略 {kvp.Value.Count - 5} 個）");
            Log("");

            if (distinctMeshes.Count > 1) sameSignatureDiffMesh++;
        }

        if (sameSignatureDiffMesh == 0)
            Log("✅ 樓梯/牆壁的 mesh 已全部統一，問題不在 mesh 層面。\n");
        else
            Log($"❌ 有 {sameSignatureDiffMesh} 個群組 signature 相同但 mesh 仍不同，需要重新套用去重。\n");

        // 額外：印出 vertex/triangle 數量分布，協助判斷是否有「幾何略有不同」的問題
        Log("樓梯/牆壁 Mesh 頂點/三角形數量分布（相同形狀應該數量完全一致）：");
        var vtxGroups = suspects
            .Where(mf => mf.sharedMesh != null)
            .GroupBy(mf => $"v{mf.sharedMesh.vertexCount}_t{mf.sharedMesh.triangles.Length/3}")
            .OrderByDescending(g => g.Count());
        foreach (var g in vtxGroups)
            Log($"    {g.Count(),4} 個物件  →  {g.Key}");
        Log("");
    }

    // ── 工具 ─────────────────────────────────────────────────
    private MeshFilter[] GetFilters() => _rootObject != null
        ? _rootObject.GetComponentsInChildren<MeshFilter>(true)
        : FindObjectsOfType<MeshFilter>();

    private void Log(string msg)
    {
        _report += msg + "\n";
        Debug.Log(msg);
        Repaint();
    }
}
