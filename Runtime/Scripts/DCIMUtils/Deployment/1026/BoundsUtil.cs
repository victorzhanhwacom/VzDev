using UnityEngine;

public static class BoundsUtil
{
    // 回傳 root 本地空間下，所有 MeshFilter 合併後的包圍盒
    public static bool TryGetLocalBounds(Transform root, out Bounds result)
    {
        result = default;
        bool has = false;

        foreach (var mf in root.GetComponentsInChildren<MeshFilter>())
        {
            if (mf.sharedMesh == null) continue;

            Matrix4x4 m = root.worldToLocalMatrix * mf.transform.localToWorldMatrix;
            Bounds mb = mf.sharedMesh.bounds;
            Vector3 c = mb.center, e = mb.extents;

            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = c + new Vector3(
                    (i & 1) == 0 ? -e.x : e.x,
                    (i & 2) == 0 ? -e.y : e.y,
                    (i & 4) == 0 ? -e.z : e.z);
                Vector3 p = m.MultiplyPoint3x4(corner);

                if (!has) { result = new Bounds(p, Vector3.zero); has = true; }
                else result.Encapsulate(p);
            }
        }
        return has;
    }
}