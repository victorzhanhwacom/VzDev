using UnityEngine;

namespace VzDev.ColorUtils
{
    [System.Serializable]
    public class ColorThresholdItem
    {
        public int threshold;
        [ColorUsage(true, true)]
        public Color color;
    }
}
