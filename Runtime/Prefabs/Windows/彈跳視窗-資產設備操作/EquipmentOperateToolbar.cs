using System;
using NaughtyAttributes;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using VzDev.CameraUtils;
using VzDev.DCIMUtils.DataUtils;
using VzDev.DOTweenUtils;
using VzDev.InteractiveUtils.ModelMouseEvent;

namespace VzDev.DCIMUtils.DeploymentUtils
{
    public class EquipmentOperateToolbar : MonoBehaviour
    {
        #region Field
        [SerializeField, ReadOnly] private EquipmentAsset equipmentAsset;
        [Foldout("[Components]"), SerializeField] private RectTransform rectTransform;
        [Foldout("[Components]"), SerializeField] private GameObject rootView;
        [Foldout("[Components]"), SerializeField] private TextMeshProUGUI txtDeviceName, txtCategory;
        [Foldout("[Components]"), SerializeField] private Image imgDevicePhoto;
        [Foldout("[Components]"), SerializeField] private Button btnLocation, btnRemove, btnMove, btnClose;
        [Foldout("[Components]"), SerializeField] private DOTweenFader tweenFader;
        #endregion

        private void Awake()
        {
            OnValidate();
            rootView.SetActive(false);
        }
        private void OnValidate()
        {
            if (rectTransform == null)
                rectTransform = GetComponent<RectTransform>();
            if (tweenFader == null)
                tweenFader = GetComponentInChildren<DOTweenFader>();
        }

        private void SetEquipmentAsset(EquipmentAsset equipmentAsset)
        {
            this.equipmentAsset = equipmentAsset;
            txtDeviceName.text = equipmentAsset.deviceName;
            txtCategory.text = equipmentAsset.category.ToString();

            bool isHavePhoto = equipmentAsset.assetPhotoSprite != null;
            imgDevicePhoto.gameObject.SetActive(isHavePhoto);
            imgDevicePhoto.sprite = equipmentAsset.assetPhotoSprite ?? null;

            btnRemove.gameObject.SetActive(equipmentAsset.system != DCIM_System.DCR);
            btnMove.gameObject.SetActive(equipmentAsset.system != DCIM_System.DCR);

            equipmentModelEvent?.Invoke(equipmentAsset.modelInfo.modelTarget);
        }

        #region Event Listener OnEnable / OnDisable
        private void OnEnable()
        {
            ColliderInteractionSystem.OnMouseClick += OnMouseClickHandler;
            ColliderInteractionSystem.OnMouseClickEmpty += OnMouseClickEmptyHandler;
            tweenFader.onComplete.AddListener(OnTweenComplete);
        }

        private void OnMouseClickHandler(GameObject target)
        {
            if (target.TryGetComponent(out DataModelBinder_Equipment binder))
            {
                SetEquipmentAsset(binder.EquipmentAsset);
                Show();
            }
        }

        private void OnMouseClickEmptyHandler() => Hide();

        private void OnDisable()
        {
            ColliderInteractionSystem.OnMouseClick -= OnMouseClickHandler;
            ColliderInteractionSystem.OnMouseClickEmpty -= OnMouseClickEmptyHandler;
            tweenFader.onComplete.RemoveListener(OnTweenComplete);
        }
        #endregion

        #region Event Listener Show / Hide
        private void Show()
        {
            btnLocation.onClick.AddListener(OnClickLocation);
            btnRemove.onClick.AddListener(OnClickRemove);
            btnMove.onClick.AddListener(OnClickMove);
            btnClose.onClick.AddListener(OnClickClose);
            tweenFader.gameObject.SetActive(true);
        }

        private void OnTweenComplete(bool isEaseOut) => rootView.SetActive(isEaseOut);

        private void Hide()
        {
            tweenFader.Hide();
            btnLocation.onClick.RemoveListener(OnClickLocation);
            btnRemove.onClick.RemoveListener(OnClickRemove);
            btnMove.onClick.RemoveListener(OnClickMove);
            btnClose.onClick.RemoveListener(OnClickClose);
        }
        private void OnClickLocation() => RTSCameraController.CameraToPosition(equipmentAsset.modelInfo.modelTarget.transform);
        private void OnClickRemove() => OnRemoveEquipmentAssetAction?.Invoke(equipmentAsset);
        private void OnClickMove() => OnMoveEquipmentAssetAction?.Invoke(equipmentAsset);
        private void OnClickClose() => ColliderInteractionSystem.SimulateClickEmpty();
        #endregion

        public static Action<EquipmentAsset> OnRemoveEquipmentAssetAction, OnMoveEquipmentAssetAction;

        public UnityEvent<Transform> equipmentModelEvent;
    }
}
