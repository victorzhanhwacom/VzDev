using NaughtyAttributes;
using UnityEngine;

namespace VzDev.ObjectUtils
{
    /// <summary>
    /// UI跟隨模型位置
    /// </summary>
    public class UI_ModelFollower : MonoBehaviour
    {
        #region Field
        [SerializeField] private Vector3 offset;
        [Foldout("[Components]"), SerializeField] private RectTransform rectTransform;
        [Foldout("[Components]"), SerializeField] private Camera mainCamera;
        private Transform targetModel;
        private Vector3 worldPosition, screenPosition;
        #endregion

        private void Awake()
        {
            if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
            if (mainCamera == null) mainCamera = Camera.main;
        }
        private void OnValidate() => Awake();

        private void Update()
        {
            if (targetModel == null)
            {
                enabled = false;
                return;
            }
            worldPosition = targetModel.position + offset;
            screenPosition = mainCamera.WorldToScreenPoint(worldPosition);
            transform.position = screenPosition;
        }

        public void FollowModelPosition(Transform modelTarget) => targetModel = modelTarget;
    }
}
