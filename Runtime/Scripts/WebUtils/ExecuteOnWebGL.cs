using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;

namespace VzDev.WebGLUtils
{
    public class ExecuteOnWebGL : MonoBehaviour
    {
        [SerializeField] private UnityEvent onWebGLAwake;

        void Awake()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            InvokeEvent();
#endif
        }

        [Button("Simulate Event")]
        private void InvokeEvent() => onWebGLAwake?.Invoke();
    }
}