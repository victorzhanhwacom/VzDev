using System;
using NaughtyAttributes;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace VzDev
{
    public class TMP_InputFieldMediator : MonoBehaviour
    {
        [Foldout("[Events]")] public UnityEvent<string> OnSubmitEvent;
        [Foldout("[Components]"), SerializeField] private TMP_InputField _inputField;
        [Foldout("[Settings]"), SerializeField] private bool isAutoClearOnSubmit = true;

        private void Awake()
        {
            if (_inputField == null)
                _inputField = GetComponent<TMP_InputField>();
        }

        private void OnEnable() => _inputField?.onSubmit.AddListener(OnSubmit);
        private void OnDisable() => _inputField?.onSubmit.RemoveListener(OnSubmit);

        private void OnSubmit(string text)
        {
            OnSubmitEvent?.Invoke(text);
            if (isAutoClearOnSubmit)
            {
                _inputField.text = string.Empty;
                _inputField.Select();
                _inputField.ActivateInputField();
            }
        }

        private void OnValidate() => _inputField ??= GetComponent<TMP_InputField>();
    }
}
