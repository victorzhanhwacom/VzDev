using DG.Tweening;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;

namespace VzDev.DotweenUtils
{
    public class DOTweenFader : MonoBehaviour
    {
        [Foldout("[Events]")] public UnityEvent onComplete, onStart;
        [Foldout("[Settings]"), SerializeField, Label("OnEnabled/OnDisabled時自動播放動畫")] private bool isAutoPlayOnEnable = true;
        [Foldout("[Settings]"), SerializeField] private bool isHideOnAwake = true;

        [Foldout("[Settings]"), SerializeField] private DOTweenData tweenData;
        [Foldout("[Components]"), SerializeField] private CanvasGroup canvasGroup;

        private Tween _tween;
        private bool IsEditPlaying => Application.isPlaying && Application.isEditor;
        private void Awake()
        {
            if (!TryGetComponent(out canvasGroup))
            {
                Debug.LogWarning($"[DOTweenFader] The target `{gameObject.name}` doesn't have a CanvasGroup component.");
            }
            if (canvasGroup != null && isHideOnAwake && !gameObject.activeSelf) gameObject.SetActive(false);
        }


        [Button, ShowIf(nameof(IsEditPlaying))]
        public void Play()
        {
            if (canvasGroup == null) return;
            Stop();

            _tween = canvasGroup.DOFade(1, tweenData.duration)
                .SetEase(tweenData.ease)
                .SetDelay(tweenData.delay)
                .OnStart(() => onStart?.Invoke())
                .OnComplete(() => onComplete?.Invoke());
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
            float calculatedDuration = tweenData.duration;
            if (_tween != null && _tween.IsActive())
            {
                calculatedDuration = _tween.ElapsedPercentage() * tweenData.duration;
            }

            Stop();
            _tween = canvasGroup.DOFade(0, calculatedDuration).SetEase(tweenData.ease);
        }

        private void OnValidate()
        {
            if (canvasGroup == null) TryGetComponent(out canvasGroup);
        }

        private void OnEnable()
        {
            if (isAutoPlayOnEnable) Play();
        }
        private void OnDisable()
        {
            if (isAutoPlayOnEnable) PlayBackwards();
        }
    }
}