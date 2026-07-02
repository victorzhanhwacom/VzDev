using NaughtyAttributes;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace VzDev.DCIMUtils
{
    public class PointInfo_RTRH : MonoBehaviour
    {
        #region Fields
        [SerializeField, ReadOnly] private bool _isRTMode = true;
        [Foldout("[Events - Mode]")] public UnityEvent onSelectRtMode, onSelectRhMode;
        [Foldout("[Events - Value]")] public UnityEvent<float> onRtChanged;
        [Foldout("[Events - Value]")] public UnityEvent<int> onRhChanged;
       // [Foldout("[Events]"), Tooltip("0:正常, 1:告警, 2:異常")] public UnityEvent<int> onStatusChanged;
        [Foldout("[Settings]"), SerializeField] private Sprite rtIcon, rhIcon;
        [Foldout("[Components]"), SerializeField] private Image icon;
        [Foldout("[Components]"), SerializeField] private TextMeshProUGUI label;

        private float _lastRTValue = -1f;
        private int _lastRHValue = -1;
        private readonly int txtUnitSize = 8;
        #endregion

        private void Awake()
        {
            icon.sprite = _isRTMode ? rtIcon : rhIcon;
            UpdateLabel();
        }

        public void SwitchMode(bool isRTMode)
        {
            if (_isRTMode == isRTMode) return;
            _isRTMode = isRTMode;
            icon.sprite = _isRTMode ? rtIcon : rhIcon;
            if (_isRTMode) onSelectRtMode?.Invoke();
            else onSelectRhMode?.Invoke();
            UpdateLabel();
        }

        private void UpdateLabel()
        {
            if (label == null) return;
            label.text = _isRTMode
                ? $"{_lastRTValue:0.#}<size={txtUnitSize}>°C</size>"
                : $"{_lastRHValue:0.#}<size={txtUnitSize}>%</size>";
            if (_isRTMode) onRtChanged?.Invoke(_lastRTValue);
            else onRhChanged?.Invoke(_lastRHValue);
        }

        public void SetRTValue(float value)
        {
            if (Mathf.Approximately(value, _lastRTValue)) return;
            _lastRTValue = value;
            UpdateLabel();
        }

        public void SetRHValue(int value)
        {
            if (Mathf.Approximately(value, _lastRHValue)) return;
            _lastRHValue = value;
            UpdateLabel();
        }

        [Button, HideIf(nameof(_isRTMode))]
        private void SwitchToRTMode() => SwitchMode(true);
        [Button, ShowIf(nameof(_isRTMode))]
        private void SwitchToRHMode() => SwitchMode(false);
    }
}
