using System;
using NaughtyAttributes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VzDev.InteractiveUtils.ModelMouseEvent;
using VzDev.ObjectUtils;

namespace VzDev.ToolUtils
{
    public class PointTag : MonoBehaviour
    {
        #region Fields
        [Foldout("[Components]"), SerializeField] private UIAnchorFollower uiAnchorFollower;
        [Foldout("[Components]"), SerializeField] private Toggle toggle, labelToggle;
        [Foldout("[Components]"), SerializeField] private TextMeshProUGUI label, label2;
      
        public Action<bool, PointTag> OnToggleChangedAction; // 外部订阅者可以监听Toggle状态变化]

        public void SetToggleGroup(ToggleGroup group)
        {
            if (toggle != null)
                toggle.group = group;
        }

        public void SetToggle(bool isOn)
        {
            if (toggle != null)
                toggle.isOn = isOn;
        }

        public Transform FollowerTarget => uiAnchorFollower != null ? uiAnchorFollower.Target3DObject : null;

        public bool LabelVisible => labelToggle != null ? labelToggle.isOn : false;
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
            if (label2 != null)
                label2.text = text;
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

        private void OnEnable()
        {
            toggle.onValueChanged.AddListener(OnToggleValueChanged);
            ColliderInteractionSystem.OnMouseClick += OnModelClicked;
            ColliderInteractionSystem.OnMouseClickEmpty += OnMouseClickEmpty;
        }

        private void OnMouseClickEmpty() => toggle.isOn = false;

        private void OnModelClicked(GameObject target)
        {
            if (target.name == uiAnchorFollower.Target3DObject.name)
            {
                toggle.isOn = true;
            }
        }

        private void OnDisable() => toggle.onValueChanged.RemoveListener(OnToggleValueChanged);
        private void OnToggleValueChanged(bool isOn)
        {
            if (isOn)
            {
                ColliderInteractionSystem.SimulateClick(uiAnchorFollower.Target3DObject.gameObject);
            }
            else
            {
                if(toggle.group != null && toggle.group.AnyTogglesOn() == false)
                {
                    ColliderInteractionSystem.SimulateClickEmpty();
                }
            }
        }
    }
}
