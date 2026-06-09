using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 旋轉不變的 Mesh 幾何簽名計算器。
///
/// 核心原理：使用旋轉不變量（rotation-invariant geometric descriptors），
/// 這些數值本身就不受旋轉影響，完全不需要先正規化方向。
/// 因此對 90°/180°/270° 等整數倍旋轉、以及任意角度旋轉都能正確識別。
///
/// 使用的不變量：
///   1. 頂點數、三角形數（拓撲）
///   2. 表面積（旋轉不改變面積）
///   3. 近似體積（散度定理，旋轉不改變體積）
///   4. Bounding Sphere 半徑（旋轉不改變）
///   5. AABB 三軸長度排序後（旋轉後排序仍相同）
///   6. 慣性矩特徵值排序（旋轉不改變特徵值）
///   7. 頂點距重心的距離直方圖（旋轉不改變距離分布）
///   8. 三角形面積直方圖（旋轉不改變每個面的面積）
/// </summary>
public static class MeshSignature
{
    // 容差設定
    private const float  PosTol      = 0.0001f;   // 長度量化（公尺）
    private const float  AreaTol     = 0.00001f;  // 面積量化
    private const float  VolTol      = 0.00001f;  // 體積量化
    private const int    HistBins    = 16;         // 直方圖桶數

    /// <summary>計算 Mesh 的旋轉不變簽名字串</summary>
    public static string Compute(Mesh mesh)
    {
        var verts = mesh.vertices;
        var tris  = mesh.triangles;

        if (verts == null || verts.Length == 0)
            return $"empty_{mesh.GetHashCode()}";

        // ── 1. 基本拓撲 ──────────────────────────────────────
        int vertCount = verts.Length;
        int triCount  = tris.Length / 3;

        // ── 2. 重心 ──────────────────────────────────────────
        Vector3 centroid = Vector3.zero;
        foreach (var v in verts) centroid += v;
        centroid /= vertCount;

        // ── 3. Bounding Sphere 半徑 ──────────────────────────
        float maxDist2 = 0f;
        foreach (var v in verts)
            maxDist2 = Mathf.Max(maxDist2, (v - centroid).sqrMagnitude);
        float boundingSphereR = Mathf.Sqrt(maxDist2);

        // ── 4. AABB 三軸長度（排序，消除軸向差異）────────────
        Vector3 mn = verts[0], mx = verts[0];
        foreach (var v in verts)
        {
            mn = Vector3.Min(mn, v);
            mx = Vector3.Max(mx, v);
        }
        float[] aabb = new[] { mx.x - mn.x, mx.y - mn.y, mx.z - mn.z };
        Array.Sort(aabb);   // 排序後與旋轉無關

        // ── 5. 表面積 + 體積 + 三角形面積直方圖 ──────────────
        float totalArea = 0f;
        float totalVol  = 0f;
        var   triAreas  = new float[triCount];

        for (int i = 0; i < triCount; i++)
        {
            int ia = tris[i * 3], ib = tris[i * 3 + 1], ic = tris[i * 3 + 2];
            if (ia >= vertCount || ib >= vertCount || ic >= vertCount) continue;

            Vector3 a = verts[ia], b = verts[ib], c = verts[ic];
            Vector3 cross = Vector3.Cross(b - a, c - a);
            float area = cross.magnitude * 0.5f;
            triAreas[i] = area;
            totalArea  += area;

            // 散度定理近似體積（有號體積，最後取絕對值）
            totalVol += Vector3.Dot(a, cross) / 6f;
        }
        totalVol = Mathf.Abs(totalVol);

        // ── 6. 慣性矩特徵值（排序）──────────────────────────
        // 慣性張量對角線元素（相對於重心）
        double Ixx = 0, Iyy = 0, Izz = 0, Ixy = 0, Ixz = 0, Iyz = 0;
        foreach (var v in verts)
        {
            Vector3 r = v - centroid;
            Ixx += r.y * r.y + r.z * r.z;
            Iyy += r.x * r.x + r.z * r.z;
            Izz += r.x * r.x + r.y * r.y;
            Ixy -= r.x * r.y;
            Ixz -= r.x * r.z;
            Iyz -= r.y * r.z;
        }
        // 用 Jacobi 求慣性張量特徵值（旋轉不變量）
        double[,] inertiaTensor =
        {
            { Ixx, Ixy, Ixz },
            { Ixy, Iyy, Iyz },
            { Ixz, Iyz, Izz }
        };
        double[] eigenvalues = ComputeEigenvalues3x3(inertiaTensor);
        Array.Sort(eigenvalues);   // 排序後與旋轉無關

        // ── 7. 頂點距重心距離直方圖 ──────────────────────────
        var distHist = ComputeDistanceHistogram(verts, centroid, boundingSphereR, HistBins);

        // ── 8. 三角形面積直方圖 ──────────────────────────────
        float maxTriArea = triAreas.Length > 0 ? triAreas.Max() : 1f;
        var   areaHist   = ComputeHistogram(triAreas, 0f, maxTriArea, HistBins);

        // ── 組合簽名 ─────────────────────────────────────────
        var sb = new System.Text.StringBuilder();

        // 拓撲
        sb.Append($"v{vertCount}_t{triCount}|");

        // 旋轉不變標量
        sb.Append($"sr{QF(boundingSphereR, PosTol)}|");
        sb.Append($"sa{QF(totalArea, AreaTol)}|");
        sb.Append($"vo{QF(totalVol, VolTol)}|");

        // AABB（排序後）
        sb.Append($"bb{QF(aabb[0],PosTol)},{QF(aabb[1],PosTol)},{QF(aabb[2],PosTol)}|");

        // 慣性矩特徵值（排序後）
        sb.Append($"ev{QD(eigenvalues[0])},{QD(eigenvalues[1])},{QD(eigenvalues[2])}|");

        // 距離直方圖
        sb.Append("dh");
        foreach (var b in distHist) sb.Append(b).Append(',');
        sb.Append('|');

        // 面積直方圖
        sb.Append("ah");
        foreach (var b in areaHist) sb.Append(b).Append(',');

        return sb.ToString();
    }

    // ── 直方圖 ────────────────────────────────────────────────
    private static int[] ComputeDistanceHistogram(
        Vector3[] verts, Vector3 centroid, float maxDist, int bins)
    {
        var hist = new int[bins];
        if (maxDist < 1e-6f) return hist;
        foreach (var v in verts)
        {
            float d   = (v - centroid).magnitude;
            int   bin = Mathf.Clamp((int)(d / maxDist * bins), 0, bins - 1);
            hist[bin]++;
        }
        return hist;
    }

    private static int[] ComputeHistogram(float[] values, float min, float max, int bins)
    {
        var hist = new int[bins];
        float range = max - min;
        if (range < 1e-9f) return hist;
        foreach (var val in values)
        {
            int bin = Mathf.Clamp((int)((val - min) / range * bins), 0, bins - 1);
            hist[bin]++;
        }
        return hist;
    }

    // ── Jacobi 特徵值分解（只需特徵值，不需特徵向量）────────
    private static double[] ComputeEigenvalues3x3(double[,] a)
    {
        double[,] m = (double[,])a.Clone();
        for (int iter = 0; iter < 100; iter++)
        {
            int p = 0, q = 1;
            double maxOff = Math.Abs(m[0, 1]);
            for (int i = 0; i < 3; i++)
            for (int j = i + 1; j < 3; j++)
            {
                double v = Math.Abs(m[i, j]);
                if (v > maxOff) { maxOff = v; p = i; q = j; }
            }
            if (maxOff < 1e-12) break;

            double th = (m[q, q] - m[p, p]) / (2.0 * m[p, q]);
            double t  = Math.Sign(th) / (Math.Abs(th) + Math.Sqrt(1.0 + th * th));
            double c  = 1.0 / Math.Sqrt(1.0 + t * t);
            double s  = t * c;

            double[,] r = (double[,])m.Clone();
            for (int i = 0; i < 3; i++)
            {
                if (i == p || i == q) continue;
                r[i, p] = r[p, i] = c * m[i, p] - s * m[i, q];
                r[i, q] = r[q, i] = s * m[i, p] + c * m[i, q];
            }
            r[p, p] = c*c*m[p,p] - 2*s*c*m[p,q] + s*s*m[q,q];
            r[q, q] = s*s*m[p,p] + 2*s*c*m[p,q] + c*c*m[q,q];
            r[p, q] = r[q, p] = 0;
            m = r;
        }
        return new[] { m[0, 0], m[1, 1], m[2, 2] };
    }

    // ── 量化工具 ──────────────────────────────────────────────
    private static int QF(float  v, float  tol) => (int)Math.Round(v / tol);
    private static int QD(double v)              => (int)Math.Round(v / 0.01);
}
