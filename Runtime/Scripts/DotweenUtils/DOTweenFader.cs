using System;
using DG.Tweening;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;

namespace VzDev.DOTweenUtils
{
    [RequireComponent(typeof(CanvasGroup))]
    public class DoTweenFader : MonoBehaviour
    {
        #region Variables

        [Foldout("[Events]")] public UnityEvent onComplete;

        [Foldout("[Settings]"), SerializeField]
        private bool isHideInStart = true;
        
        [Foldout("[Settings]"), SerializeField]
        private float duration = 0.3f;

        [Foldout("[Settings]"), SerializeField]
        private float delay = 0f;

        [Foldout("[Settings]"), SerializeField]
        private Ease ease = Ease.Linear;
        
        [Foldout("[Settings]"), SerializeField]
        private CanvasGroup canvasGroup;

        private Tween _tween;

        #endregion

        /// Fade淡入
        public void Show() => ToFade(1f);

        /// Fade淡出
        public void Hide() => ToFade(0f);

        public void ToFade(bool isShown)
        {
            if (isShown) Show();
            else Hide();
        }
        public void ToFade(float toValue)
        {
            if (_tween != null)
            {
                _tween.Kill();
                _tween = null;
            }

            if (canvasGroup == null) return;

            // Zero GC allocation, fluent syntax
            _tween = canvasGroup.DOFade(toValue, duration).SetDelay(delay).SetEase(ease)
                .OnComplete(() => onComplete?.Invoke());
        }

        private void Start()
        {
            OnValidate();
            canvasGroup.alpha = isHideInStart? 0f : 1f;
        }

        private void OnValidate()
        {
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }
        }

        private void OnDestroy() => _tween?.Kill();
    }
}