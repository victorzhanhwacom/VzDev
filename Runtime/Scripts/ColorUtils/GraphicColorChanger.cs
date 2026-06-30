using System;
using System.Collections.Generic;
using DG.Tweening;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.UI;

namespace VzDev.ColorUtils
{
    /// <summary>
    /// Graphic顏色切換器 - 根據布林值或索引切換一組Graphic的顏色，支持DOTween動畫過渡。
    /// </summary>
    public class GraphicColorChanger : MonoBehaviour
    {
        #region Variables
        [ReadOnly, SerializeField] private int currentIndex = -1;
        [SerializeField] private ColorIndex[] colorIndices;

        [Foldout("[Settings]"), SerializeField] private float duration = 0.3f, delay;
        [Foldout("[Settings]"), SerializeField] private Ease ease = Ease.OutQuad;
        private bool IsOn => currentIndex == 1;
        #endregion

        [Button, ShowIf(nameof(IsOn))]
        public void SetColorToggleOff() => ChangeColor(false);
        [Button, HideIf(nameof(IsOn))]
        public void SetColorToggleOn() => ChangeColor(true);

        /// <summary>
        /// 根據布林值切換顏色，預設使用colorIndices陣列的前兩個元素作為切換對象。
        /// </summary>
        public void ChangeColor(bool isOn)
        {
            if (colorIndices?.Length < 2)
                Debug.LogWarning($"ColorIndices count is less than 2!");
            else
                ChangeColor(isOn ? 1 : 0);
        }

        public void ChangeColor(Single index) => ChangeColor((int)index);
        public void ChangeColor(int index)
        {
            currentIndex = index;
            if (index < 0 || index >= colorIndices.Length)
            {
                Debug.LogWarning($"Color index {index} is out of range!");
                return;
            }
            ColorItem[] colorItems = colorIndices[index].colorItems;
            if (colorItems == null || colorItems.Length == 0)
            {
                Debug.LogWarning($"ColorItems for index {index} is null or empty!");
                return;
            }
            for (int i = 0; i < colorItems.Length; i++)
            {
                ColorItem item = colorItems[i];
                if (item.graphicTargets == null || item.graphicTargets.Length == 0)
                {
                    Debug.LogWarning($"Targets for ColorItem {i} in index {index} is null or empty!");
                    continue;
                }
                for (int j = 0; j < item.graphicTargets.Length; j++)
                {
                    Graphic target = item.graphicTargets[j];
                    if (target == null)
                    {
                        Debug.LogWarning($"A target in ColorItem {i} of index {index} is null!");
                        continue;
                    }
#if UNITY_EDITOR
                    if (!Application.isPlaying) { target.color = item.color; continue; }
#endif
                    target.DOKill(this);
                    target.DOColor(item.color, duration).SetEase(ease).SetDelay(delay);
                }
            }
        }
    }

    [Serializable]
    public class ColorIndex
    {
        public int index;
        public ColorItem[] colorItems;
    }
    [Serializable]
    public class ColorItem
    {
        public Color color;
        public Graphic[] graphicTargets;
    }
}