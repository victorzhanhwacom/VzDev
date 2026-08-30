using UnityEngine;

namespace VzDev.ColorUtils.Staging
{
    /// <summary>
    /// 將任意 Mesh 轉換成「每三角面獨立頂點」版本，並把重心座標 (1,0,0)/(0,1,0)/(0,0,1)
    /// 烘進 UV2，供 StagingWireframeLines.shader 在 Fragment Stage 用 fwidth 畫三角面邊線。
    /// 不依賴 Geometry Shader，WebGL/所有平台皆可用。
    /// 明確逐個 Sub-mesh 讀取三角面索引，涵蓋多材質槽模型的完整幾何。
    /// </summary>
    public static class BarycentricMeshBaker
    {
        public static Mesh Bake(Mesh source)
        {
            if (source == null) return null;

            var srcVerts = source.vertices;
            var srcNormals = source.normals;
            var srcUV0 = source.uv;

            int totalTriCount = 0;
            var subMeshTriangles = new int[source.subMeshCount][];

            for (int s = 0; s < source.subMeshCount; s++)
            {
                var topology = source.GetTopology(s);
                if (topology != MeshTopology.Triangles)
                {
                    Debug.LogWarning($"[BarycentricMeshBaker] Sub-mesh {s} 拓樸是 {topology}（非 Triangles），已跳過。");
                    subMeshTriangles[s] = System.Array.Empty<int>();
                    continue;
                }

                subMeshTriangles[s] = source.GetTriangles(s);
                totalTriCount += subMeshTriangles[s].Length / 3;
            }

            if (totalTriCount == 0)
            {
                Debug.LogError($"[BarycentricMeshBaker] '{source.name}' 沒有任何可用的三角面資料，回傳 null。");
                return null;
            }

            var newVerts = new Vector3[totalTriCount * 3];
            var newNormals = new Vector3[totalTriCount * 3];
            var newUV0 = new Vector2[totalTriCount * 3];
            var newUV2 = new Vector2[totalTriCount * 3];
            var newTriangles = new int[totalTriCount * 3];

            int writeTriIndex = 0;
            for (int s = 0; s < source.subMeshCount; s++)
            {
                var triangles = subMeshTriangles[s];
                if (triangles == null || triangles.Length == 0) continue;

                int triCountInSub = triangles.Length / 3;

                for (int t = 0; t < triCountInSub; t++)
                {
                    for (int v = 0; v < 3; v++)
                    {
                        int srcIndex = triangles[t * 3 + v];
                        int dstIndex = writeTriIndex * 3 + v;

                        newVerts[dstIndex] = srcVerts[srcIndex];
                        newNormals[dstIndex] = (srcNormals != null && srcNormals.Length > srcIndex)
                            ? srcNormals[srcIndex] : Vector3.up;
                        newUV0[dstIndex] = (srcUV0 != null && srcUV0.Length > srcIndex)
                            ? srcUV0[srcIndex] : Vector2.zero;
                        newTriangles[dstIndex] = dstIndex;

                        newUV2[dstIndex] = v == 0 ? new Vector2(1, 0)
                                          : v == 1 ? new Vector2(0, 1)
                                                    : new Vector2(0, 0);
                    }
                    writeTriIndex++;
                }
            }

            var result = new Mesh
            {
                name = source.name + "_Barycentric",
                indexFormat = newVerts.Length > 65000
                    ? UnityEngine.Rendering.IndexFormat.UInt32
                    : UnityEngine.Rendering.IndexFormat.UInt16
            };
            result.vertices = newVerts;
            result.normals = newNormals;
            result.uv = newUV0;
            result.uv2 = newUV2;
            result.triangles = newTriangles;
            result.RecalculateBounds();

            return result;
        }
    }
}