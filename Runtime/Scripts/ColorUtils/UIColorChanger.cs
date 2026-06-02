using System.Collections.Generic;
using DG.Tweening;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.UI;

namespace VzDev.ColorUtils
{
    /// <summary>
    /// 簡單的UI顏色切換器，支援Image和TextMeshProUGUI等Graphic元件。
    /// </summary>
    public class UIColorChanger : MonoBehaviour
    {
        #region Variables

        [SerializeField] private Color[] colors;

        [Label("[Target:Image/TextmeshProUGUI]")] [SerializeField]
        private List<Graphic> targets;

        [Foldout("[Settings]"), SerializeField] private float duration = 0.2f, delay;
        [Foldout("[Settings]"), SerializeField] private Ease ease = Ease.OutQuad;

        [Foldout("[Settings]"), Label("[可選] - 自動綁定Toggle.isOn判斷"), SerializeField]
        private Toggle toggleTarget;

        #endregion

        private void OnEnable() => toggleTarget?.onValueChanged.AddListener(ChangeColor);
        private void OnDisable() => toggleTarget?.onValueChanged.RemoveListener(ChangeColor);

        /// 設置顏色Index (true:0/false:1)
        public void ChangeColor(bool isOn)
        {
            if (colors.Length < 2)
                Debug.LogWarning($"Colors count is less than 2!", this);
            else
                ChangeColor(isOn ? 1 : 0);
        }

        public void ChangeColor(int index)
        {
            if (toggleTarget != null && toggleTarget.isOn) return;
            targets.ForEach(target =>
            {
                if (Application.isPlaying)
                    target.DOColor(colors[index], duration).SetEase(ease).SetDelay(delay);
                else
                    target.color = colors[index];
            });
        }
    }
}