using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Runtime Mesh Deduplicator（幾何矩版）
///
/// 掛在場景根物件上，場景載入後自動執行。
/// 使用旋轉不變的幾何矩簽名，正確識別並合併
/// 形狀相同但旋轉不同（含 90°/180°/270°）的 Mesh。
///
/// WebGL URP 注意：
///   - URP Asset 需啟用 SRP Batcher（預設已開）
///   - Material 需使用相同 Shader + 相同屬性才能合批
/// </summary>
public class RuntimeMeshDeduplicator : MonoBehaviour
{
    [Header("設定")]
    [Tooltip("留空 = 掃描整個場景")]
    public GameObject rootObject;

    [Tooltip("每幀處理幾個 MeshFilter（避免 WebGL 單幀卡頓，建議 30~100）")]
    [Range(10, 200)]
    public int batchPerFrame = 50;

    [Header("Debug")]
    public bool showLog = true;

    // ── 狀態 ─────────────────────────────────────────────────
    private bool _done;
    public  bool IsDone => _done;

    // 外部可訂閱的完成事件
    public System.Action<int, int> OnComplete; // (total, replaced)

    // ─────────────────────────────────────────────────────────
    private void Start()
    {
        StartCoroutine(DeduplicateAsync());
    }

    private IEnumerator DeduplicateAsync()
    {
        MeshFilter[] filters = rootObject != null
            ? rootObject.GetComponentsInChildren<MeshFilter>(true)
            : FindObjectsOfType<MeshFilter>();

        int total = filters.Length;
        if (showLog)
            Debug.Log($"[RuntimeMeshDedup] 開始掃描 {total} 個 MeshFilter...");

        // signature → canonical mesh
        // 同一個 mesh instance 只算一次 signature
        var sigCache    = new Dictionary<int, string>();      // instanceID → sig
        var canonicalMap = new Dictionary<string, Mesh>();    // sig → canonical mesh

        int replaced  = 0;
        int processed = 0;

        foreach (var mf in filters)
        {
            var mesh = mf.sharedMesh;
            if (mesh == null) { processed++; continue; }

            // 從快取取 signature，避免重複計算同一個 mesh asset
            int meshId = mesh.GetInstanceID();
            if (!sigCache.TryGetValue(meshId, out string sig))
            {
                sig = MeshSignature.Compute(mesh);
                sigCache[meshId] = sig;
            }

            if (!canonicalMap.TryGetValue(sig, out var canonical))
            {
                // 第一次見到這個幾何形狀，登記為 canonical
                canonicalMap[sig] = mesh;
            }
            else if (!ReferenceEquals(mesh, canonical))
            {
                // 相同幾何、不同 mesh instance → 替換
                mf.sharedMesh = canonical;
                replaced++;

                if (showLog)
                    Debug.Log($"[RuntimeMeshDedup] 替換：{mf.name} [{mesh.name}] → [{canonical.name}]");
            }

            processed++;
            if (processed % batchPerFrame == 0)
                yield return null;
        }

        _done = true;

        if (showLog)
            Debug.Log($"[RuntimeMeshDedup] 完成！" +
                      $"掃描 {total} 個，唯一形狀 {canonicalMap.Count} 種，" +
                      $"替換 {replaced} 個重複參照");

        OnComplete?.Invoke(total, replaced);
    }
}
