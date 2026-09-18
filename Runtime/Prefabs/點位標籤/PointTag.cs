using NaughtyAttributes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VzDev.InteractiveUtils.ModelMouseEvent;
using VzDev.ObjectUtils;

namespace VzDev.LandmarkUtils
{
    public class PointTag : MonoBehaviour
    {
        #region Fields
        [Foldout("[Components]"), SerializeField] private UIAnchorFollower uiAnchorFollower;
        [Foldout("[Components]"), SerializeField] private Toggle toggleSelf, toggleViewMode, toggleDotMode;
        [Foldout("[Components]"), SerializeField] private TextMeshProUGUI txtModelName;
        [Foldout("[Components]"), SerializeField] private Button btnClose;
        public Transform FollowerTarget => uiAnchorFollower != null ? uiAnchorFollower.Target3DObject : null;
        private bool IsDotMode => toggleSelf.isOn;
        private bool isClickByModel = false;
        #endregion

        #region 初始設置Toggle相關
        /// <summary>
        /// 設置UI Anchor Follower的目標物件，讓Tag跟隨該物件的位置。
        /// </summary>
        public void SetFollowerTarget(Transform target)
        {
            if (uiAnchorFollower != null) uiAnchorFollower.SetTargetObject(target);
        }

        /// <summary>
        /// 設定View標籤模式的ToggleGroup
        /// </summary>
        public void SetToggleViewModeGroup(ToggleGroup group)
        {
            if (toggleViewMode != null) toggleViewMode.group = group;
        }

        /// <summary>
        /// 設定Dot精簡模式的ToggleGroup
        /// </summary>
        public void SetToggleDotModeGroup(ToggleGroup group)
        {
            if (toggleDotMode != null) toggleDotMode.group = group;
        }

        /// <summary>
        /// 設定是否為Dot精簡模式
        /// </summary>
        public void SetIsDotMode(bool isOn)
        {
            if (toggleSelf != null) toggleSelf.isOn = isOn;
        }

        /// <summary>
        /// 設定標籤文字，如果有的話
        /// </summary>
        public void SetModelName(string modelName)
        {
            if (txtModelName != null) txtModelName.text = modelName;
        }
        #endregion

        private void OnValidate()
        {
            if (uiAnchorFollower == null)
                uiAnchorFollower = GetComponent<UIAnchorFollower>();
            toggleSelf ??= GetComponent<Toggle>();
        }

        #region Event Listener
        private void OnDisable()
        {
            btnClose.onClick.RemoveListener(OnClickCloseButton);
            toggleDotMode.onValueChanged.RemoveListener(OnToggleViewModeValueChanged);
            toggleViewMode.onValueChanged.RemoveListener(OnToggleViewModeValueChanged);
            ColliderInteractionSystem.OnMouseClick -= OnModelClicked;
            ColliderInteractionSystem.OnMouseClickEmpty -= OnMouseClickEmpty;
        }
        private void OnEnable()
        {
            btnClose.onClick.AddListener(OnClickCloseButton);
            toggleDotMode.onValueChanged.AddListener(OnToggleViewModeValueChanged);
            toggleViewMode.onValueChanged.AddListener(OnToggleViewModeValueChanged);
            ColliderInteractionSystem.OnMouseClick += OnModelClicked;
            ColliderInteractionSystem.OnMouseClickEmpty += OnMouseClickEmpty;
        }
        #endregion

        #region 點擊Toggle / 模型事件
        private void OnClickCloseButton() => ColliderInteractionSystem.SimulateClickEmpty();
        private void OnToggleViewModeValueChanged(bool isOn)
        {
            if (isClickByModel) return;
            if (isOn)
                ColliderInteractionSystem.SimulateClick(uiAnchorFollower.Target3DObject.gameObject);
            else
                ColliderInteractionSystem.SimulateClickEmpty();
            isClickByModel = false;
        }

        private void OnModelClicked(GameObject target)
        {
            if (uiAnchorFollower == null || uiAnchorFollower.Target3DObject == null)
            {
                Debug.LogWarning($"{gameObject.name}: UIAnchorFollower或其目標物件為空，無法處理模型點擊事件。", this);
                return;
            }

            bool isTargetModelClicked = target == uiAnchorFollower.Target3DObject.gameObject;

            if (isTargetModelClicked)
            {
                var targetToggle = IsDotMode ? toggleDotMode : toggleViewMode;
                if (targetToggle.isOn)
                {
                    ColliderInteractionSystem.SimulateClickEmpty();
                }
                else
                {
                    targetToggle.isOn = true;
                }
            }
        }

        private void OnMouseClickEmpty()
        {
            isClickByModel = true;
            var targetToggle = IsDotMode ? toggleDotMode : toggleViewMode;
            targetToggle.isOn = false;
        }

        #endregion
    }
}
