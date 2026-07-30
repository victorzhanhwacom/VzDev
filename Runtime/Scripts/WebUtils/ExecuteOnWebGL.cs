using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;

namespace VzDev.WebGLUtils
{
    public class ExecuteOnWebGL : MonoBehaviour
    {
        [Foldout("[Event]"), SerializeField] private UnityEvent onWebGLAwake;
        [SerializeField] private GameObject[] objectsToHide;

        void Awake()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            foreach (var obj in objectsToHide)
            {
                if (obj != null)
                    obj.SetActive(false);
            }
            onWebGLAwake?.Invoke();
#endif
        }
    }
}