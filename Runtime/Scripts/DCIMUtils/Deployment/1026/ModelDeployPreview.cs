using NaughtyAttributes;
using UnityEngine;
using VzDev.DebugUtils;
using VzDev.ToolUtils;

namespace VzDev.BuilderUtils
{
    /// <summary>
    /// 模型部署預覽，提供旋轉功能
    /// </summary>
    public class ModelDeployPreview : MonoBehaviour
    {
        #region Fields
        [SerializeField] Transform previewModel;
        [Foldout("[Settings]"), SerializeField] private float rotatePerAngle = 90f;
        [Foldout("[Settings]"), SerializeField] private LayerMask hitLayerMask = ~0;
        [Foldout("[Style]"), SerializeField] private Material deployFollowMaterial, deployAllowMaterial, deployForbiddenMaterial;
        private Camera _mainCamera;
        private Renderer modelRenderer;
        private Material[] originalMaterial;
        private Ray ray;
        private RaycastHit hit;

        private bool isDeployed = false;
        private int currentFrameCount = 0;
        #endregion

        private void Awake()
        {
            _mainCamera = Camera.main;
        }

        public Transform Model => previewModel;

        [Button]
        private void BuildModel() => DeployModel(previewModel);

        /// <summary>
        /// 設置預覽模型並建立，並將其旋轉重置為0
        /// </summary>
        public void DeployModel(Transform model)
        {
            if (model == null) return;
            previewModel = Instantiate(model, transform);
            previewModel.localRotation = Quaternion.identity;  //將模型的旋轉重置為0
            if (previewModel.TryGetComponent(out modelRenderer))
            {
                originalMaterial = modelRenderer.materials;
            }
        }

        private void Update()
        {
            if (previewModel == null || isDeployed || EventHelper.IsPointerOverUI()) return;

            ModelFollowCourser();
        }

        /// <summary>
        /// 模型跟隨鼠標位置
        /// </summary>
        private void ModelFollowCourser()
        {
            ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out hit, Mathf.Infinity, hitLayerMask))
            {
                previewModel.position = hit.point;
                previewModel.rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
              
            }
        }



        private void DeployModelOnClick()
        {
            if (Input.GetMouseButtonDown(0))
            {
                isDeployed = true;
                //在這裡可以添加部署模型的邏輯
            }
        }


        #region Change Material of Model 
        private void RestoreMaterial() => SwitchMaterial(originalMaterial);
        // private void SwitchMaterialOnHit() => SwitchMaterial(OnHitMaterial);
        // private void SwitchMaterialNonHit() => SwitchMaterial(NonHitMaterial);
        private void SwitchMaterial(Material[] materials)
        {
            if (modelRenderer != null && materials != null)
            {
                modelRenderer.materials = materials;
            }
        }
        private void SwitchMaterial(Material material)
        {
            if (material == null) return;
            SwitchMaterial(new Material[] { material });
        }
        #endregion

        #region Rotate
        [Button]
        public void RotateLeft() => Rotate(-rotatePerAngle);
        [Button]
        public void RotateRight() => Rotate(rotatePerAngle);
        private void Rotate(float angle)
        {
            if (previewModel == null) return;
            previewModel.Rotate(0f, angle, 0f, Space.Self);
        }
        #endregion
    }
}
