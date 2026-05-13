using DG.Tweening;
using TMPro;
using UnityEngine;

namespace VzDev.WebUtils
{
    public class WebSystemSetting : MonoBehaviour
    {
        public TextMeshProUGUI textMeshProUGUI;
        private void Awake()
        {
#if !UNITY_EDITOR && UNITY_WEBGL
        UnityEngine.WebGLInput.captureAllKeyboardInput = false;
#endif
        }
    }
}