using NaughtyAttributes;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using VzDev.Extensions;

namespace VzDev.StringUtils
{
    public class StringMediator : MonoBehaviour
    {
        [Foldout("[Events]")] public UnityEvent<bool> isValueExistEvent;
        [Foldout("[Events]")] public UnityEvent onValueExistEvent, onValueNotExistEvent;

        public void CheckValueExist(TextMeshProUGUI component)
        {
            if (ReferenceEquals(component, null)) return;
            CheckValueExist(component.text);
        }

        public void CheckValueExist(TMP_InputField component)
        {
            if (ReferenceEquals(component, null)) return;
            CheckValueExist(component.text);
        }

        public void CheckValueExist(string txt) => InvokeEvent(txt.IsValueExist());

        public void InvokeEvent(bool isValueExist)
        {
            isValueExistEvent.Invoke(isValueExist);
            if (isValueExist) onValueExistEvent?.Invoke();
            else onValueNotExistEvent?.Invoke();
        }
        
    }
}
