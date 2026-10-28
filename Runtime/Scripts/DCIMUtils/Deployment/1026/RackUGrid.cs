using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;
using VzDev.ApiExtensions;

[RequireComponent(typeof(Grid))]
public class RackUGrid : MonoBehaviour
{
    [SerializeField] int totalU = 42;
    [Tooltip("機櫃前門在 Grid local 的 -Z 方向；若前門在 +Z 請取消勾選")]
    [SerializeField] bool frontIsNegativeZ = true;
    Grid grid;
    [SerializeField, ReadOnly] private List<RackPlacement> placements = new();

    Grid G => grid != null ? grid : (grid = GetComponent<Grid>());
    public int TotalU => totalU;
    public float FrontSign => frontIsNegativeZ ? -1f : 1f;  // 前門方向的 Z 符號

    public bool TryAdd(RackPlacement np)
    {
        if (np.startU < 1 || np.startU + np.sizeU - 1 > totalU) return false;
        if (!IsFree(np)) return false;
        placements.Add(np);
        return true;
    }

    public void Remove(string id) => placements.RemoveAll(p => p.id == id);
    [Button]
    public void Clear()
    {
        placements.Clear();
        transform.RemoveAllChildren();
    }

    bool IsFree(RackPlacement np)
    {
        const float eps = 0.0001f;
        foreach (var p in placements)
        {
            bool uOverlap = np.startU < p.startU + p.sizeU && p.startU < np.startU + np.sizeU;
            bool xOverlap = np.xMin < p.xMax - eps && p.xMin < np.xMax - eps;
            if (uOverlap && xOverlap) return false;
        }
        return true;
    }

    // 回傳 UGrid 的 local 空間中心與尺寸，方便設備直接當子物件擺放，不受機櫃縮放影響
    public void GetLocalPose(RackPlacement p, out Vector3 localCenter, out Vector3 localSize)
    {
        Vector3 cell = G.cellSize;
        Vector3 bottom = G.CellToLocal(new Vector3Int(0, p.startU - 1, 0));

        localSize = new Vector3(cell.x * (p.xMax - p.xMin), cell.y * p.sizeU, cell.z);
        localCenter = bottom + new Vector3(
            cell.x * (p.xMin + p.xMax) * 0.5f,
            localSize.y * 0.5f,
            cell.z * 0.5f);
    }

    // 世界座標 → 第幾U，超出範圍回傳 -1
    public int WorldToU(Vector3 worldPos)
    {
        int u = G.WorldToCell(worldPos).y + 1;
        return (u >= 1 && u <= totalU) ? u : -1;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        var g = GetComponent<Grid>();
        if (g == null) return;

        var style = new GUIStyle(UnityEditor.EditorStyles.label) { fontSize = 10, normal = { textColor = Color.white } };
        Gizmos.matrix = transform.localToWorldMatrix;

        for (int i = 0; i < totalU; i++)
        {
            Vector3 local = g.CellToLocal(new Vector3Int(0, i, 0));
            Gizmos.color = ((i + 1) % 5 == 0) ? Color.blue * 0.6f : Color.yellow * 0.6f;
            Gizmos.DrawWireCube(local + g.cellSize * 0.5f, g.cellSize);
            UnityEditor.Handles.Label(
                transform.TransformPoint(local + new Vector3(-0.03f, g.cellSize.y * 0.5f, 0f)),
                $"U{i + 1}", style);
        }

        // 綠框標示前門那一面，方便確認 frontIsNegativeZ 設定是否正確
        Vector3 origin = g.CellToLocal(Vector3Int.zero);
        float h = g.cellSize.y * totalU;
        float zFront = origin.z + (frontIsNegativeZ ? 0f : g.cellSize.z);
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(
            new Vector3(origin.x + g.cellSize.x * 0.5f, origin.y + h * 0.5f, zFront),
            new Vector3(g.cellSize.x, h, 0f));
    }
#endif
}