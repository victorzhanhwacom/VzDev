using UnityEngine;

namespace VzDev.DCIMUtils
{
    /// <summary>
    /// 以「子物件」作為控制點（依 Hierarchy 順序），沿 Catmull-Rom 曲線生成空調氣流管狀 Mesh。
    /// Mesh 只在建立/修改時計算一次，執行期零 CPU 成本；搭配 VzDev/DCIM/Airflow Shader 使用。
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class AirflowTube : MonoBehaviour
    {
        [Header("Shape")]
        [Range(8, 128)] public int segments = 40;
        [Range(4, 16)]  public int sides = 8;
        [Min(0.01f)] public float radiusStart = 0.12f;   // 出風口半徑
        [Min(0.01f)] public float radiusEnd = 0.4f;      // 末端半徑（氣流擴散）

        [Header("Appearance")]
        public Color tint = Color.white;
        [Range(0f, 2f)] public float intensity = 1f;     // 風量（寫入頂點色 alpha）
        [Tooltip("包圍盒外擴（公尺），需大於 Shader 的 Sway Amplitude，避免擺動時被裁切")]
        [Min(0f)] public float boundsPadding = 0.5f;

        static readonly int AirflowTimeId = Shader.PropertyToID("_AirflowTime");
        static int s_lastFrame = -1;

        Mesh _mesh;
        Vector3[] _cp;
        Color[] _colors;
        bool _dirty = true;

        void OnEnable()   => _dirty = true;
        void OnValidate() => _dirty = true;

        void OnDestroy()
        {
            if (_mesh == null) return;
            if (Application.isPlaying) Destroy(_mesh);
            else DestroyImmediate(_mesh);
        }

        void Update()
        {
            // 全場景所有氣流共用一個全域時間，每幀只設定一次
            if (s_lastFrame != Time.frameCount)
            {
                s_lastFrame = Time.frameCount;
                double t = Application.isPlaying ? Time.timeAsDouble : Time.realtimeSinceStartupAsDouble;
                Shader.SetGlobalFloat(AirflowTimeId, (float)(t % 3600.0));
            }

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                for (int i = 0; i < transform.childCount; i++)
                {
                    var c = transform.GetChild(i);
                    if (c.hasChanged) { c.hasChanged = false; _dirty = true; }
                }
            }
#endif
            if (_dirty) Build();
        }

        /// <summary>即時調整風量（只更新頂點色，不重建幾何）。關機請直接停用 Renderer。</summary>
        public void SetIntensity(float value)
        {
            intensity = value;
            if (_mesh == null || _colors == null) return;
            var c = tint; c.a = intensity;
            for (int i = 0; i < _colors.Length; i++) _colors[i] = c;
            _mesh.colors = _colors;
        }

        public void Build()
        {
            _dirty = false;
            int n = transform.childCount;
            if (n < 2) return;

            if (_cp == null || _cp.Length != n) _cp = new Vector3[n];
            for (int i = 0; i < n; i++) _cp[i] = transform.GetChild(i).localPosition;

            int rings = segments + 1;
            int ring = sides + 1;   // 多一個頂點處理 UV 接縫

            // 取樣路徑與累積弧長
            var pos = new Vector3[rings];
            var arc = new float[rings];
            for (int i = 0; i < rings; i++)
            {
                pos[i] = Sample(i / (float)segments);
                arc[i] = i == 0 ? 0f : arc[i - 1] + Vector3.Distance(pos[i], pos[i - 1]);
            }
            float total = Mathf.Max(arc[rings - 1], 1e-4f);

            int vCount = rings * ring;
            var verts = new Vector3[vCount];
            var normals = new Vector3[vCount];
            var uv0 = new Vector2[vCount];
            var uv1 = new Vector2[vCount];
            var tangents = new Vector4[vCount];                       // 截面側向（擺動用）
            var frameUp = new System.Collections.Generic.List<Vector3>(vCount); // 截面上向（擺動用）
            _colors = new Color[vCount];
            var tris = new int[segments * sides * 6];

            var col = tint; col.a = intensity;
            Vector3 normal = Vector3.zero;

            for (int i = 0; i < rings; i++)
            {
                Vector3 tan = (pos[Mathf.Min(i + 1, rings - 1)] - pos[Mathf.Max(i - 1, 0)]).normalized;

                // Parallel transport：避免管子扭轉
                if (i == 0)
                {
                    Vector3 up = Mathf.Abs(Vector3.Dot(tan, Vector3.up)) < 0.99f ? Vector3.up : Vector3.right;
                    normal = Vector3.Cross(tan, up).normalized;
                }
                else
                {
                    normal = Vector3.ProjectOnPlane(normal, tan).normalized;
                }
                Vector3 binormal = Vector3.Cross(tan, normal);

                float t = arc[i] / total;
                float r = Mathf.Lerp(radiusStart, radiusEnd, t);

                for (int j = 0; j <= sides; j++)
                {
                    float a = j / (float)sides * Mathf.PI * 2f;
                    Vector3 d = normal * Mathf.Cos(a) + binormal * Mathf.Sin(a);
                    int k = i * ring + j;
                    verts[k] = pos[i] + d * r;
                    normals[k] = d;
                    uv0[k] = new Vector2(t, j / (float)sides);
                    uv1[k] = new Vector2(arc[i], 0f);
                    tangents[k] = new Vector4(normal.x, normal.y, normal.z, 1f);
                    frameUp.Add(binormal);
                    _colors[k] = col;
                }
            }

            int ti = 0;
            for (int i = 0; i < segments; i++)
            {
                for (int j = 0; j < sides; j++)
                {
                    int a = i * ring + j;
                    int b = a + ring;
                    tris[ti++] = a;     tris[ti++] = a + 1; tris[ti++] = b;
                    tris[ti++] = a + 1; tris[ti++] = b + 1; tris[ti++] = b;
                }
            }

            if (_mesh == null)
            {
                _mesh = new Mesh { name = "AirflowTube", hideFlags = HideFlags.DontSave };
                GetComponent<MeshFilter>().sharedMesh = _mesh;
            }
            _mesh.Clear();
            _mesh.vertices = verts;
            _mesh.normals = normals;
            _mesh.uv = uv0;
            _mesh.SetUVs(1, uv1);
            _mesh.SetUVs(2, frameUp);
            _mesh.tangents = tangents;
            _mesh.colors = _colors;
            _mesh.triangles = tris;
            _mesh.RecalculateBounds();
            var bounds = _mesh.bounds;
            bounds.Expand(boundsPadding * 2f);
            _mesh.bounds = bounds;
        }

        Vector3 Sample(float t)
        {
            int last = _cp.Length - 1;
            float f = Mathf.Clamp01(t) * last;
            int i = Mathf.Min((int)f, last - 1);
            float u = f - i;

            Vector3 p0 = _cp[Mathf.Max(i - 1, 0)];
            Vector3 p1 = _cp[i];
            Vector3 p2 = _cp[i + 1];
            Vector3 p3 = _cp[Mathf.Min(i + 2, last)];

            return 0.5f * (2f * p1
                         + (-p0 + p2) * u
                         + (2f * p0 - 5f * p1 + 4f * p2 - p3) * u * u
                         + (-p0 + 3f * p1 - 3f * p2 + p3) * u * u * u);
        }

        void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.3f, 0.85f, 1f, 0.6f);
            for (int i = 0; i < transform.childCount - 1; i++)
                Gizmos.DrawLine(transform.GetChild(i).position, transform.GetChild(i + 1).position);
        }
    }
}
