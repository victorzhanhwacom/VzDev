using NaughtyAttributes;
using UnityEngine;

namespace VzDev.RenderingUtils.Staging
{
    /// <summary>
    /// 掛在待上架設備模型上，新增兩個子物件分別疊加「半透明色調」與「三角網格線」，
    /// 完全不修改原始模型的 MeshRenderer/Material。
    /// 拆成兩個獨立 Renderer/Material 的原因：URP 對單一材質內多個未標記 LightMode
    /// 的自訂 Pass，實務上只會執行第一個 Pass，無法用一個材質做出 Fill+Line 疊加效果，
    /// 因此改用兩個各自只有一個 Pass 的材質，分別掛在兩個子物件上。
    /// </summary>
    [RequireComponent(typeof(MeshFilter))]
    public class StagingWireframeApplier : MonoBehaviour
    {
        #region Fields
        [SerializeField] private Shader fillShader;
        [SerializeField] private Shader lineShader;
        [SerializeField, ColorUsage(true, true)] private Color tintColor = new Color(1f, 0.4f, 0.1f, 0.15f);
        [SerializeField, ColorUsage(true, true)] private Color edgeColor = new Color(1f, 0.55f, 0.15f, 1f);
        [SerializeField, Range(0.5f, 4f)] private float edgeThickness = 1.5f;

        private const string FillChildName = "__StagingWireframeFill";
        private const string LineChildName = "__StagingWireframeLines";

        private GameObject fillObject;
        private GameObject lineObject;
        private Mesh bakedMesh;
        private Material fillMaterial;
        private Material lineMaterial;

        private bool isApplied => fillObject != null || lineObject != null;
        #endregion

        [Button("Apply Staging Effect"), ShowIf(nameof(CanApply))]
        public void Apply()
        {
            if (isApplied) return;

            var meshFilter = GetComponent<MeshFilter>();
            if (fillShader == null || lineShader == null)
            {
                Debug.LogError($"{nameof(StagingWireframeApplier)}: fillShader 或 lineShader 未指定，無法套用效果。", this);
                return;
            }
            if (meshFilter.sharedMesh == null)
            {
                Debug.LogWarning($"{nameof(StagingWireframeApplier)}: 找不到 sharedMesh，略過。", this);
                return;
            }

            bakedMesh = BarycentricMeshBaker.Bake(meshFilter.sharedMesh);
            if (bakedMesh == null)
            {
                Debug.LogError($"{nameof(StagingWireframeApplier)}: 烘焙失敗，中止套用。", this);
                return;
            }

            fillObject = CreateChild(FillChildName);
            fillObject.AddComponent<MeshFilter>().sharedMesh = bakedMesh;
            fillMaterial = new Material(fillShader) { name = "StagingWireframeFill_Runtime" };
            fillObject.AddComponent<MeshRenderer>().sharedMaterial = fillMaterial;

            lineObject = CreateChild(LineChildName);
            lineObject.AddComponent<MeshFilter>().sharedMesh = bakedMesh;
            lineMaterial = new Material(lineShader) { name = "StagingWireframeLines_Runtime" };
            lineObject.AddComponent<MeshRenderer>().sharedMaterial = lineMaterial;

            ApplyColorSettings();
        }

        private GameObject CreateChild(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, worldPositionStays: false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            return go;
        }

        [Button("Revert To Original"), ShowIf(nameof(isApplied))]
        public void Revert()
        {
            DestroyChild(ref fillObject);
            DestroyChild(ref lineObject);
            if (bakedMesh != null) Object.Destroy(bakedMesh);
            bakedMesh = null;
            fillMaterial = null;
            lineMaterial = null;
        }

        private void DestroyChild(ref GameObject go)
        {
            if (go == null) return;
            if (Application.isPlaying) Object.Destroy(go);
            else Object.DestroyImmediate(go);
            go = null;
        }

        private bool CanApply() => !isApplied;

        private void OnValidate()
        {
            if (isApplied) ApplyColorSettings();
        }

        private void ApplyColorSettings()
        {
            if (fillMaterial != null) fillMaterial.SetColor("_TintColor", tintColor);
            if (lineMaterial != null)
            {
                lineMaterial.SetColor("_EdgeColor", edgeColor);
                lineMaterial.SetFloat("_EdgeThickness", edgeThickness);
            }
        }

        private void OnDestroy()
        {
            if (bakedMesh != null) Object.Destroy(bakedMesh);
        }
    }
}