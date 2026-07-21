using NaughtyAttributes;
using UnityEngine;

namespace VzDev.ConfigUtils
{
    public class SystemConfig : MonoBehaviour
    {
        [Foldout("[Settings]"), SerializeField, Tooltip("-1為不限制")] private int targetFrameRate = -1;

        private void Awake()
        {
            Application.targetFrameRate = targetFrameRate;
            QualitySettings.vSyncCount = 0;
        }
    }
}
