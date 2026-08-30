using NaughtyAttributes;
using UnityEngine;

namespace VzDev.ColorUtils.Staging
{
    /// <summary>
    /// 掛在待上架設備模型上，依模型 Bounding Box 生成一個外框子物件，
    /// 套用 Fresnel 玻璃感材質，模擬類似建造模式下的擺設預覽效果。
    /// 完全不修改原始模型的 MeshRenderer/Material。
    /// </summary>
    [RequireComponent(typeof(MeshFilter))]
    public class StagingBoundsApplier : MonoBehaviour
    {
        #region Fields
        [SerializeField] private Shader boundingBoxShader;
        [SerializeField, ColorUsage(true, true)] private Color fillColor = new Color(0.4f, 0.9f, 1f, 1f);
        [SerializeField, ColorUsage(true, true)] private Color edgeColor = new Color(0.6f, 1f, 1f, 1f);
        [SerializeField, Range(0.001f, 0.1f)] private float edgeThickness = 0.015f;
        [SerializeField, Range(0.5f, 8f)] private float fresnelPower = 3f;
        [SerializeField, Range(0f, 1f)] private float fillMinAlpha = 0.03f;
        [SerializeField, Range(0f, 1f)] private float fillMaxAlpha = 0.6f;
        [SerializeField, Range(0f, 0.2f), Tooltip("外框相對模型外擴的比例，0 為完全貼合")]
        private float boundsPadding = 0.03f;

        private const string ChildName = "__StagingBoundingBox";

        private GameObject boxObject;
        private Mesh boxMesh;
        private Material boxMaterial;

        private bool isApplied => boxObject != null;
        #endregion

        [Button("Apply Bounding Box Effect"), ShowIf(nameof(CanApply))]
        public void Apply()
        {
            if (isApplied) return;

            var meshFilter = GetComponent<MeshFilter>();
            if (boundingBoxShader == null)
            {
                Debug.LogError($"{nameof(StagingBoundsApplier)}: boundingBoxShader 未指定，無法套用效果。", this);
                return;
            }
            if (meshFilter.sharedMesh == null)
            {
                Debug.LogWarning($"{nameof(StagingBoundsApplier)}: 找不到 sharedMesh，略過。", this);
                return;
            }

            boxMesh = BoundingBoxMeshBuilder.Build(meshFilter.sharedMesh.bounds, boundsPadding);

            boxObject = new GameObject(ChildName);
            boxObject.transform.SetParent(transform, worldPositionStays: false);
            boxObject.transform.localPosition = Vector3.zero;
            boxObject.transform.localRotation = Quaternion.identity;
            boxObject.transform.localScale = Vector3.one;

            boxObject.AddComponent<MeshFilter>().sharedMesh = boxMesh;
            boxMaterial = new Material(boundingBoxShader) { name = "StagingBoundingBox_Runtime" };
            boxObject.AddComponent<MeshRenderer>().sharedMaterial = boxMaterial;

            ApplySettings();
        }

        [Button("Revert To Original"), ShowIf(nameof(isApplied))]
        public void Revert()
        {
            if (boxObject != null)
            {
                if (Application.isPlaying) Object.Destroy(boxObject);
                else Object.DestroyImmediate(boxObject);
                boxObject = null;
            }
            if (boxMesh != null) Object.Destroy(boxMesh);
            boxMesh = null;
            boxMaterial = null;
        }

        private bool CanApply() => !isApplied;

        private void OnValidate()
        {
            if (isApplied) ApplySettings();
        }

        private void ApplySettings()
        {
            if (boxMaterial == null) return;
            boxMaterial.SetColor("_FillColor", fillColor);
            boxMaterial.SetColor("_EdgeColor", edgeColor);
            boxMaterial.SetFloat("_EdgeThickness", edgeThickness);
            boxMaterial.SetFloat("_FresnelPower", fresnelPower);
            boxMaterial.SetFloat("_FillMinAlpha", fillMinAlpha);
            boxMaterial.SetFloat("_FillMaxAlpha", fillMaxAlpha);
        }

        private void OnDestroy()
        {
            if (boxMesh != null) Object.Destroy(boxMesh);
        }
    }
}