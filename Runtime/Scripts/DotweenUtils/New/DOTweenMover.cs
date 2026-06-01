using DG.Tweening;
using NaughtyAttributes;
using UnityEngine;

namespace VzDev.DotweenUtils1
{
    ///DOTween移動效果
    public class DOTweenMover : MonoBehaviour
    {
        #region Variables

        [Label("[Events]"), ShowIf(nameof(IsHaveTweenData))] public DOTweenEvents events;
        [Foldout("[Settings]"), SerializeField, Expandable] private DOTweenBaseData tweenData;
        [Foldout("[Settings]"), SerializeField] private bool setFromValue, setToValue = true;
        [Foldout("[Settings]"), SerializeField, ShowIf(nameof(setFromValue))] private Vector2 fromValue;
        [Foldout("[Settings]"), SerializeField, ShowIf(nameof(setToValue))] private Vector2 toValue;
        [Foldout("[Comopnents]"), SerializeField] private RectTransform rectTarget;

        private Tween _tween;
        private Vector2 _originalPos;

        private bool IsHaveTweenData => tweenData != null;
        private bool IsHaveTweenDataAndPlaying => IsHaveTweenData && Application.isEditor && Application.isPlaying;
        #endregion

        private void OnValidate()
        {
            if (rectTarget == null) TryGetComponent(out rectTarget);
        }
        private void Awake()
        {
            OnValidate();
            if (rectTarget == null) Debug.LogWarning($"[DOTweenMover] `{name}` doesn't have a RectTransform component.");
            if (tweenData == null) Debug.LogWarning($"[DOTweenMover] `{name}` doesn't have tween data assigned.");
            if (rectTarget != null)
            {
                _originalPos = rectTarget.anchoredPosition;
                if (setFromValue && !setToValue) rectTarget.anchoredPosition = fromValue;
            }
        }


        [Button, ShowIf(nameof(IsHaveTweenDataAndPlaying))]
        public void PlayTween()
        {
            if (rectTarget == null) return;
            StopTween();

            Vector2 toPos = setToValue ? toValue : _originalPos;
            _tween = rectTarget.DOAnchorPos(toPos, tweenData.duration)
                .SetEase(tweenData.easeIn).SetDelay(tweenData.delay)
                .OnStart(() => events.onStart?.Invoke())
                .OnComplete(() => events.onComplete?.Invoke());
        }

        [Button, ShowIf(nameof(IsHaveTweenDataAndPlaying))]
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

            StopTween();
            _tween = rectTarget.DOAnchorPos(targetPos, calculatedDuration).SetEase(tweenData.easeOut);
        }

        public void ToPlay(bool isOn)
        {
            if (isOn) PlayTween();
            else PlayBackwards();
        }

        [Button, ShowIf(nameof(IsHaveTweenDataAndPlaying))]
        public void StopTween()
        {
            if (_tween != null && _tween.IsActive()) _tween.Kill();
            _tween = null;
        }
        private void OnDestroy() => StopTween();
    }
}