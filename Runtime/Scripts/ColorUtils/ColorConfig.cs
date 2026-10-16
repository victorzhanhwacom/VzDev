using NaughtyAttributes;
using UnityEngine;
using VzDev.Frameworks;

namespace VzDev.ColorUtils
{
    public class ColorConfig : SingletonMonoBehaviour<ColorConfig>
    {
        [Foldout("[AlertLevels]"), SerializeField] private Color32 normalColor = new Color32(57, 255, 57, 255);
        [Foldout("[AlertLevels]"), SerializeField] private Color32 warningColor = new Color32(255, 204, 58, 255);
        [Foldout("[AlertLevels]"), SerializeField] private Color32 alertColor = new Color32(255, 102, 90, 255);
        [Foldout("[OtherColors]"), SerializeField] private Color32 cyanColor = new Color32(100, 255, 255, 255);

        public static Color32 NormalColor => Instance?.normalColor ?? new Color32(57, 255, 57, 255);
        public static Color32 WarningColor => Instance?.warningColor ?? new Color32(255, 204, 58, 255);
        public static Color32 AlertColor => Instance?.alertColor ?? new Color32(255, 102, 90, 255);
        public static Color32 CyanColor => Instance?.cyanColor ?? new Color32(100, 255, 255, 255);
    }
}
