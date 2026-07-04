using NaughtyAttributes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VzDev.ObjectUtils;

namespace VzDev.ToolUtils
{
    public class PointTag : MonoBehaviour
    {
        #region Fields
        [Foldout("[Components]"), SerializeField] private UIAnchorFollower uiAnchorFollower;
        [Foldout("[Components]"), SerializeField] private Toggle toggle, labelToggle;
        [Foldout("[Components]"), SerializeField] private TextMeshProUGUI label;
        public Toggle ToggleItem => toggle;
        public Transform FollowerTarget => uiAnchorFollower != null ? uiAnchorFollower.Target3DObject : null;
        #endregion

        /// <summary>
        /// 設置UI Anchor Follower的目標物件，讓Tag跟隨該物件的位置。
        /// </summary>
        public void SetFollowerTarget(Transform target)
        {
            if (uiAnchorFollower != null)
                uiAnchorFollower.SetTargetObject(target);
        }

        public void SetLabelAlwaysVisible(bool alwaysVisible)
        {
            if (labelToggle != null)
                labelToggle.isOn = alwaysVisible;
        }

        public void SetLabel(string text)
        {
            if (label != null)
                label.text = text;
        }

        private void OnValidate()
        {
            if (uiAnchorFollower == null)
                uiAnchorFollower = GetComponent<UIAnchorFollower>();
            if (toggle == null)
                toggle = GetComponentsInChildren<Toggle>(true)[0];
            if (labelToggle == null)
                labelToggle = GetComponentsInChildren<Toggle>(true)[1];
            if (label == null)
                label = GetComponentInChildren<TextMeshProUGUI>(true);
        }
    }
}
