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
}