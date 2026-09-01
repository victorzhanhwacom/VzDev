using System;
using NaughtyAttributes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VzDev.DCIMUtils.DataUtils;
using VzDev.InteractiveUtils.ModelMouseEvent;

namespace VzDev.DCIMUtils.DeploymentUtils
{
    /// <summary>
    /// 點擊設備模型時，顯示工具列，並且工具列跟隨設備模型位置。
    /// </summary>
    public class ToolBarOfEquipment : MonoBehaviour
    {
        #region Fields
        [Foldout("[Settings]"), SerializeField] private Vector3 offset = new Vector3(0, 0, 0);
        [Foldout("[Components]"), SerializeField] private Button btnDetail, btnRemove, btnMove;
        [Foldout("[Components]"), SerializeField] private TextMeshProUGUI txtDeviceName, txtSystem;
        [Foldout("[Components]"), SerializeField] private GameObject container;
        [Foldout("[Components]"), SerializeField] private Camera mainCamera;

        private EquipmentAsset equipmentAsset;
        private Vector3 worldPosition, screenPosition, modelCenterPosition;

        #endregion

        private void Awake()
        {
            container.SetActive(false);
            if (mainCamera == null) mainCamera = Camera.main;
        }

        #region 更新UI顯示資訊
        private void UpdateView()
        {
            txtDeviceName.text = equipmentAsset.companyPropertyInfo.propertyName;
            txtSystem.text = equipmentAsset.system.ToString();
        }
        #endregion

        #region 點選按鈕 & 選取目標設備模型
        private void OnClickDetailBtn() => onClickDetailBtn?.Invoke();
        private void OnClickRemoveBtn() => onClickRemoveBtn?.Invoke();
        private void OnClickMoveBtn() => onClickMoveBtn?.Invoke();

        private void OnClickEquipmentModel()
        {
            container.SetActive(true);
            btnDetail.onClick.AddListener(OnClickDetailBtn);
            btnRemove.onClick.AddListener(OnClickRemoveBtn);
            btnMove.onClick.AddListener(OnClickMoveBtn);
        }
        private void OnMouseClickEmpty()
        {
            container.SetActive(false);
            btnDetail.onClick.RemoveListener(OnClickDetailBtn);
            btnRemove.onClick.RemoveListener(OnClickRemoveBtn);
            btnMove.onClick.RemoveListener(OnClickMoveBtn);
        }
        #endregion

        #region UI座標跟隨目標設備模型 (Update)
        private void Update() => FollowSelectedEquipmentModel();

        /// <summary>
        /// UI座標跟隨目標設備模型
        /// </summary>
        private void FollowSelectedEquipmentModel()
        {
            if (equipmentAsset == null || !container.activeSelf) return;
            modelCenterPosition = equipmentAsset.modelInfo.modelTarget.GetComponent<Renderer>().bounds.center;
            worldPosition = modelCenterPosition + offset;
            screenPosition = mainCamera.WorldToScreenPoint(worldPosition);
            transform.position = screenPosition;
        }
        #endregion

        #region Event Listener
        private void OnEnable()
        {
            ColliderInteractionSystem.OnMouseClick += OnMouseClickModel;
            ColliderInteractionSystem.OnMouseClickEmpty += OnMouseClickEmpty;
        }
        private void OnDisable()
        {
            ColliderInteractionSystem.OnMouseClick -= OnMouseClickModel;
            ColliderInteractionSystem.OnMouseClickEmpty -= OnMouseClickEmpty;
        }
        private void OnMouseClickModel(GameObject target)
        {
            if (target.TryGetComponent(out DataModelBinder_Equipment dataModelBinder))
            {
                equipmentAsset = dataModelBinder.EquipmentAsset;
                OnClickEquipmentModel();
                UpdateView();
            }
        }
        #endregion

        #region Static Events
        public static Action onClickDetailBtn, onClickRemoveBtn, onClickMoveBtn;
        #endregion
    }
}
