using UnityEngine;

namespace VzDev.RenderingUtils.Staging
{
    /// <summary>
    /// 依 Mesh 的本地空間 Bounding Box，生成一個六面體外框 Mesh。
    /// 每個面獨立配置 4 個頂點（不共用頂點），UV 各自是 0~1，
    /// 讓 StagingBoundingBox.shader 能用 UV 邊界距離畫出乾淨的四邊形邊框，
    /// 不需要像三角網格線那樣烘焙重心座標。
    /// </summary>
    public static class BoundingBoxMeshBuilder
    {
        /// <param name="padding">外框相對模型 Bounding Box 的外擴比例，0 為完全貼合，0.05 代表各方向外擴 5%</param>
        public static Mesh Build(Bounds localBounds, float padding = 0.03f)
        {
            Vector3 center = localBounds.center;
            Vector3 extents = localBounds.extents * (1f + padding);

            Vector3 min = center - extents;
            Vector3 max = center + extents;

            // 8 個角點
            Vector3 p0 = new Vector3(min.x, min.y, min.z);
            Vector3 p1 = new Vector3(max.x, min.y, min.z);
            Vector3 p2 = new Vector3(max.x, min.y, max.z);
            Vector3 p3 = new Vector3(min.x, min.y, max.z);
            Vector3 p4 = new Vector3(min.x, max.y, min.z);
            Vector3 p5 = new Vector3(max.x, max.y, min.z);
            Vector3 p6 = new Vector3(max.x, max.y, max.z);
            Vector3 p7 = new Vector3(min.x, max.y, max.z);

            var verts = new System.Collections.Generic.List<Vector3>();
            var normals = new System.Collections.Generic.List<Vector3>();
            var uvs = new System.Collections.Generic.List<Vector2>();
            var tris = new System.Collections.Generic.List<int>();

            void AddFace(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal)
            {
                int baseIndex = verts.Count;
                verts.Add(a); verts.Add(b); verts.Add(c); verts.Add(d);
                normals.Add(normal); normals.Add(normal); normals.Add(normal); normals.Add(normal);
                uvs.Add(new Vector2(0, 0));
                uvs.Add(new Vector2(1, 0));
                uvs.Add(new Vector2(1, 1));
                uvs.Add(new Vector2(0, 1));
                tris.Add(baseIndex + 0); tris.Add(baseIndex + 2); tris.Add(baseIndex + 1);
                tris.Add(baseIndex + 0); tris.Add(baseIndex + 3); tris.Add(baseIndex + 2);
            }

            AddFace(p0, p1, p2, p3, Vector3.down);   // 底面
            AddFace(p4, p7, p6, p5, Vector3.up);     // 頂面
            AddFace(p0, p4, p5, p1, Vector3.back);   // 前面 (-Z)
            AddFace(p3, p2, p6, p7, Vector3.forward);// 後面 (+Z)
            AddFace(p0, p3, p7, p4, Vector3.left);   // 左面
            AddFace(p1, p5, p6, p2, Vector3.right);  // 右面

            var mesh = new Mesh { name = "StagingBoundingBox" };
            mesh.SetVertices(verts);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();

            return mesh;
        }
    }
}