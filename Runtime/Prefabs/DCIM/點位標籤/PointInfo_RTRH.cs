using System;
using NaughtyAttributes;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace VzDev.DCIMUtils
{
    public class PointInfo_RTRH : MonoBehaviour
    {
        [SerializeField, ReadOnly] private bool _isRTMode = true;
        [Foldout("[Events]")] public UnityEvent<int> onSwitchToMode;
        [Foldout("[Settings]"), SerializeField] private Sprite rtIcon, rhIcon;
        [Foldout("[Components]"), SerializeField] private Image icon;
        [Foldout("[Components]"), SerializeField] private TextMeshProUGUI label;

        private float _lastRTValue = -1f, _lastRHValue = -1f;

        public void SwitchMode(bool isRTMode)
        {
            if(_isRTMode == isRTMode) return;
            _isRTMode = isRTMode;
            icon.sprite = _isRTMode ? rtIcon : rhIcon;
            onSwitchToMode?.Invoke(_isRTMode ? 0 : 1);
            UpdateLabel();
        }

        private void UpdateLabel()
        {
            label.text = _isRTMode ? $"{_lastRTValue} °C" : $"{_lastRHValue} %";
        }

        public void SetRTValue(float value)
        {
            if (value != _lastRTValue)
            {
                _lastRTValue = value;
                UpdateLabel();
            }
        }

        public void SetRHValue(float value)
        {
            if (value != _lastRHValue)
            {
                _lastRHValue = value;
                UpdateLabel();
            }
        }

        [Button, HideIf(nameof(_isRTMode))]
        private void SwitchToRTMode() => SwitchMode(true);
        [Button, ShowIf(nameof(_isRTMode))]
        private void SwitchToRHMode() => SwitchMode(false);
    }
}
