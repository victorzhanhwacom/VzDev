using NaughtyAttributes;
using TMPro;
using UnityEngine;

namespace VzDev.DOTweenUtils
{
    /// <summary>
    /// 文字特效
    /// </summary>
    public class DOTweenText : DOTweenBase
    {
        #region Variables
        [Foldout("[Components]"), SerializeField] private TextMeshProUGUI txt;
        #endregion

        public void SetText(string str)
        {
            str = str.Trim();
            if (Application.isPlaying && Duration > 0) tween = SetTweenParams(DOTweenHelper.ToBlink(txt, str, Duration), true);
            else txt.text = str;
        }

        public string text
        {
            set => SetText(value);
        }

        private void Awake() => OnValidate();
        private void OnValidate() => txt ??= GetComponent<TextMeshProUGUI>();

        public override void SetVisible(bool isVisible)
        {
            StopTween();
            tween = SetTweenParams(DOTweenHelper.ToBlink(txt, txt.text, Duration), true);
            gameObject.SetActive(isVisible);
        }
    }
}