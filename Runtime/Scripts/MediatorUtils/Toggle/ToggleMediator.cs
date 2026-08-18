using System;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace VzDev.MediatorUtils
{
    [RequireComponent(typeof(Toggle))]
    public class ToggleMediator : MonoBehaviour
    {
        [Foldout("[Events]")] public UnityEvent onTrueEvent, onFalseEvent;
        [Foldout("[Events]")] public UnityEvent<bool> onReverseEvent;
        [Foldout("[Components]"), SerializeField] private Toggle toggle;

        private void Awake() => GetToggle();

        public void NotifiyEvent() => SetToggleAndNotify(toggle.isOn);

        public void SetToggleAndNotify(bool value)
        {
            GetToggle();
            toggle.SetIsOnWithoutNotify(value);
            toggle.onValueChanged.Invoke(value);
        }

        private void SetIsOn(bool value)
        {
            (value? onTrueEvent : onFalseEvent)?.Invoke();
            onReverseEvent?.Invoke(!value);
        }

        private void GetToggle()
        {
            if (toggle == null)
            {
                toggle = GetComponent<Toggle>();
            }
        }

        private void OnEnable()
        {
            GetToggle();
            toggle.onValueChanged.AddListener(SetIsOn);
        }

        private void OnDisable()
        {
            GetToggle();
            toggle.onValueChanged.RemoveListener(SetIsOn);
        }
        private void OnValidate() => GetToggle();
    }
}