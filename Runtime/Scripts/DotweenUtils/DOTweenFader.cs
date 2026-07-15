using DG.Tweening;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;

namespace VzDev.DotweenUtils
{
    public class DOTweenFader : MonoBehaviour
    {
        [Foldout("[Events]")] public UnityEvent onComplete;
        [SerializeField] private DOTweenSetting tweenSetting;
        [SerializeField] private DOTweenSetting tweenEvent;

        [Foldout("[Components]"), SerializeField] private CanvasGroup canvasGroup;

        private Tween _tween;
        private bool IsEditPlaying => Application.isPlaying && Application.isEditor;
        private void Awake()
        {
            if (!TryGetComponent(out canvasGroup))
            {
                Debug.LogWarning($"[DOTweenFader] The target `{gameObject.name}` doesn't have a CanvasGroup component.");
            }
        }


        [Button, ShowIf(nameof(IsEditPlaying))]
        public void Play()
        {
            if (canvasGroup == null) return;
            Stop();

            _tween = canvasGroup.DOFade(1, tweenSetting.duration);
            _tween = tweenSetting.SetupTween(_tween);
            _tween = tweenEvent.SetupTween(_tween);


/* 
            _tween = canvasGroup.DOFade(1, tweenSetting.duration)
                .SetEase(tweenSetting.easeOut)
                .SetDelay(tweenSetting.delay)
                .OnStart(() => onStart?.Invoke())
                .OnComplete(() => onComplete?.Invoke()); */
        }

        [Button, ShowIf(nameof(IsEditPlaying))]
        public void Stop()
        {
            if (_tween != null && _tween.IsActive()) _tween.Kill();
            _tween = null;
        }

        public void PlayBackwards()
        {
            if (canvasGroup == null) return;

            // 2. 同樣計算動態時間，避免淡入到一半被攔截時，突然變太慢
            float calculatedDuration = tweenSetting.duration;
            if (_tween != null && _tween.IsActive())
            {
                calculatedDuration = _tween.ElapsedPercentage() * tweenSetting.duration;
            }

            Stop();
            _tween = canvasGroup.DOFade(0, calculatedDuration).SetEase(tweenSetting.ease);
        }

        private void OnValidate()
        {
            if (canvasGroup == null) TryGetComponent(out canvasGroup);
        }
    }
}