using UnityEngine;

namespace DCIM
{
    [ExecuteAlways]
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class CCTVViewCone : MonoBehaviour
    {
        [Header("View Settings")]
        [SerializeField]
        private float range = 15f;

        [SerializeField]
        [Range(1f, 120f)]
        private float horizontalFOV = 60f;

        [SerializeField]
        [Range(1f, 120f)]
        private float verticalFOV = 40f;


        [Header("Mesh")]
        [SerializeField]
        [Range(4, 64)]
        private int segments = 32;


        [Header("Display")]
        [SerializeField]
        private Material viewConeMaterial;

        [SerializeField]
        private bool visibleOnStart = true;


        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private Mesh mesh;


        // ============================================================
        // Properties
        // ============================================================

        public float Range
        {
            get => range;

            set
            {
                float newValue =
                    Mathf.Max(0.01f, value);

                if (Mathf.Approximately(range, newValue))
                    return;

                range = newValue;

                Rebuild();
            }
        }


        public float HorizontalFOV
        {
            get => horizontalFOV;

            set
            {
                float newValue =
                    Mathf.Clamp(
                        value,
                        1f,
                        120f
                    );

                if (Mathf.Approximately(horizontalFOV, newValue))
                    return;

                horizontalFOV = newValue;

                Rebuild();
            }
        }


        public float VerticalFOV
        {
            get => verticalFOV;

            set
            {
                float newValue =
                    Mathf.Clamp(
                        value,
                        1f,
                        120f
                    );

                if (Mathf.Approximately(verticalFOV, newValue))
                    return;

                verticalFOV = newValue;

                Rebuild();
            }
        }


        public int Segments
        {
            get => segments;

            set
            {
                int newValue =
                    Mathf.Clamp(
                        value,
                        4,
                        64
                    );

                if (segments == newValue)
                    return;

                segments = newValue;

                Rebuild();
            }
        }


        // ============================================================
        // Unity
        // ============================================================

        private void Awake()
        {
            Initialize();
        }


        private void OnEnable()
        {
            Initialize();
        }


        private void OnDestroy()
        {
            DestroyMesh();
        }


        // ============================================================
        // Editor / Inspector
        // ============================================================

        private void OnValidate()
        {
            // Inspector 改值時進來這裡

            range =
                Mathf.Max(
                    0.01f,
                    range
                );


            horizontalFOV =
                Mathf.Clamp(
                    horizontalFOV,
                    1f,
                    120f
                );


            verticalFOV =
                Mathf.Clamp(
                    verticalFOV,
                    1f,
                    120f
                );


            segments =
                Mathf.Clamp(
                    segments,
                    4,
                    64
                );


            // 編輯器還沒初始化時
            // 先抓 Component

            CacheComponents();


            if (meshFilter == null)
                return;


            Rebuild();
        }


        // ============================================================
        // Initialize
        // ============================================================

        private void Initialize()
        {
            CacheComponents();

            if (meshFilter == null)
                return;


            if (viewConeMaterial != null)
            {
                meshRenderer.sharedMaterial =
                    viewConeMaterial;
            }


            Rebuild();


            // Editor 不強制處理顯示狀態
            // 避免 Inspector 操作時一直改 enabled

            if (Application.isPlaying)
            {
                SetVisible(visibleOnStart);
            }
        }


        private void CacheComponents()
        {
            if (meshFilter == null)
            {
                meshFilter =
                    GetComponent<MeshFilter>();
            }


            if (meshRenderer == null)
            {
                meshRenderer =
                    GetComponent<MeshRenderer>();
            }
        }


        // ============================================================
        // Rebuild
        // ============================================================

        public void Rebuild()
        {
            CacheComponents();


            if (meshFilter == null)
                return;


            DestroyMesh();


            mesh =
                CreateViewConeMesh(
                    range,
                    horizontalFOV,
                    verticalFOV,
                    segments
                );


            meshFilter.sharedMesh =
                mesh;
        }


        // ============================================================
        // Destroy Mesh
        // ============================================================

        private void DestroyMesh()
        {
            if (mesh == null)
                return;


            if (Application.isPlaying)
            {
                Destroy(mesh);
            }
            else
            {
                DestroyImmediate(mesh);
            }


            mesh = null;
        }


        // ============================================================
        // Create Mesh
        // ============================================================

        private static Mesh CreateViewConeMesh(
            float range,
            float horizontalFOV,
            float verticalFOV,
            int segments)
        {
            Mesh mesh =
                new Mesh
                {
                    name = "CCTV View Cone"
                };


            int vertexCount =
                segments * 2 + 1;


            Vector3[] vertices =
                new Vector3[vertexCount];


            Vector2[] uv =
                new Vector2[vertexCount];


            int[] triangles =
                new int[segments * 6];


            // --------------------------------------------------------
            // CCTV Origin
            // --------------------------------------------------------

            vertices[0] =
                Vector3.zero;


            uv[0] =
                new Vector2(
                    0.5f,
                    0f
                );


            // --------------------------------------------------------
            // FOV
            // --------------------------------------------------------

            float horizontalRad =
                horizontalFOV *
                Mathf.Deg2Rad;


            float verticalRad =
                verticalFOV *
                Mathf.Deg2Rad;


            float halfWidth =
                Mathf.Tan(
                    horizontalRad * 0.5f
                ) * range;


            float halfHeight =
                Mathf.Tan(
                    verticalRad * 0.5f
                ) * range;


            // --------------------------------------------------------
            // Far Ring
            // --------------------------------------------------------

            for (int i = 0; i < segments; i++)
            {
                float angle =
                    (float)i /
                    segments *
                    Mathf.PI *
                    2f;


                float x =
                    Mathf.Cos(angle) *
                    halfWidth;


                float y =
                    Mathf.Sin(angle) *
                    halfHeight;


                Vector3 position =
                    new Vector3(
                        x,
                        y,
                        range
                    );


                vertices[i + 1] =
                    position;


                vertices[
                    segments + i + 1
                ] =
                    position;


                float normalized =
                    (float)i /
                    segments;


                uv[i + 1] =
                    new Vector2(
                        normalized,
                        1f
                    );


                uv[
                    segments + i + 1
                ] =
                    new Vector2(
                        normalized,
                        1f
                    );
            }


            // --------------------------------------------------------
            // Triangles
            // --------------------------------------------------------

            int index = 0;


            for (int i = 0; i < segments; i++)
            {
                int next =
                    (i + 1) % segments;


                // Front

                triangles[index++] =
                    0;

                triangles[index++] =
                    i + 1;

                triangles[index++] =
                    next + 1;


                // Back

                triangles[index++] =
                    0;

                triangles[index++] =
                    next + 1;

                triangles[index++] =
                    i + 1;
            }


            // --------------------------------------------------------
            // Apply Mesh
            // --------------------------------------------------------

            mesh.vertices =
                vertices;

            mesh.uv =
                uv;

            mesh.triangles =
                triangles;


            mesh.RecalculateNormals();

            mesh.RecalculateBounds();


            return mesh;
        }


        // ============================================================
        // Visibility
        // ============================================================

        public void SetVisible(bool visible)
        {
            CacheComponents();

            if (meshRenderer == null)
                return;


            meshRenderer.enabled =
                visible;
        }


        public void Show()
        {
            SetVisible(true);
        }


        public void Hide()
        {
            SetVisible(false);
        }
    }
}