using DG.Tweening;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;

namespace VzDev.DotweenUtils
{
    public class DOTweenMover : MonoBehaviour
    {
        [Foldout("[Events]")] public UnityEvent onComplete, onStart;
        [Foldout("[Settings]"), SerializeField, Label("OnEnabled/OnDisabled時自動播放動畫")] private bool isAutoPlayOnEnable = true;
        [Foldout("[Settings]"), SerializeField] private bool setFromValue = true;
        [Foldout("[Settings]"), SerializeField] private bool setToValue;
        [Foldout("[Settings]"), SerializeField, ShowIf(nameof(setFromValue))] private Vector2 fromValue;

        [Foldout("[Settings]"), SerializeField, ShowIf(nameof(setToValue))] private Vector2 toValue;
        [Foldout("[Settings]"), SerializeField] private DOTweenData tweenData;

        [Foldout("[Components]"), SerializeField] private RectTransform rectTarget;

        private Vector2 _originalPos;
        private Tween _tween;
        private bool IsEditPlaying => Application.isPlaying && Application.isEditor;

        private void Awake()
        {
            if (rectTarget == null) TryGetComponent(out rectTarget);
            if (rectTarget != null)
            {

                _originalPos = rectTarget.anchoredPosition;
                if (setFromValue) rectTarget.anchoredPosition = fromValue;
            }
            else
            {
                Debug.LogWarning($"[DOTweenMoverData] The target `{name}` doesn't have a RectTransform component.");
            }
        }
        [Button, ShowIf(nameof(IsEditPlaying))]
        public void Play()
        {
            if (rectTarget == null) return;
            Stop();


            Vector2 toPos = setToValue ? toValue : _originalPos;
            _tween = rectTarget.DOAnchorPos(toPos, tweenData.duration)
                .SetEase(tweenData.ease).SetDelay(tweenData.delay)
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
            if (rectTarget == null) return;
            // 1. 決定目標：如果沒勾 setFromValue，就安全退回到初始位置
            Vector2 targetPos = setFromValue ? fromValue : _originalPos;

            // 2. 決定動態時間 (進階優化，自由選擇是否保留)
            float calculatedDuration = tweenData.duration;
            if (_tween != null && _tween.IsActive())
            {
                // 如果原本的動畫播到一半被攔截，我們按比例縮短回去的時間，避免移動速度突然變慢
                calculatedDuration = _tween.ElapsedPercentage() * tweenData.duration;
            }

            Stop();
            _tween = rectTarget.DOAnchorPos(targetPos, calculatedDuration).SetEase(tweenData.ease);
        }

        private void OnValidate()
        {
            if (rectTarget == null) TryGetComponent(out rectTarget);
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