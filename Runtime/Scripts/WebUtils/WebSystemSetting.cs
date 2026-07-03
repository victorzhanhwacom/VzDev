using UnityEngine;

namespace VzDev.WebUtils
{
    public class WebSystemSetting : MonoBehaviour
    {
        private void Start()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            WebGLInput.captureAllKeyboardInput = false;
#endif
        }
    }
}