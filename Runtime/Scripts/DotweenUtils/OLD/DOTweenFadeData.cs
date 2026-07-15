using DG.Tweening;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;

namespace VzDev.DotweenUtils
{/* 
    [CreateAssetMenu(fileName = "DOTweenFadeData", menuName = "VzDev/DOTween/DOTweenFadeData")]
    public class DOTweenFadeData : DOTweenBaseData
    {
        public bool isHideOnAwake = true;
        public bool isForceFrom;
        [ShowIf(nameof(isForceFrom))] public float fromAlpha = 0f;
        public float toAlpha = 1f;

        public override ITweenWorker CreateWorker(GameObject target) => new FadeWorker(target, this);
    }

    public class FadeWorker : ITweenWorker
    {
        private CanvasGroup _cg;
        private DOTweenFadeData _data;
        private Tween _tween;

        public FadeWorker(GameObject target, DOTweenFadeData data)
        {
            if (target.TryGetComponent(out _cg))
            {
                _data = data;
                if (_data.isHideOnAwake) target.SetActive(false);
            }
            else
            {
                Debug.LogWarning($"[DOTweenFaderData] The target `{target.name}` doesn't have a CanvasGroup component.");
            }
        }

        public void Play(UnityEvent onStart, UnityEvent onComplete)
        {
            if (_cg == null) return;
            Stop();

            if (_data.isForceFrom) _cg.alpha = _data.fromAlpha;
            _tween = _cg.DOFade(_data.toAlpha, _data.tweenData.duration)
                .SetEase(_data.tweenData.ease)
                .SetDelay(_data.tweenData.delay)
                .OnStart(() => onStart?.Invoke())
                .OnComplete(() => onComplete?.Invoke());
        }

        public void Stop()
        {
            if (_tween != null && _tween.IsActive()) _tween.Kill();
            _tween = null;
        }

        public void PlayBackwards()
        {
            if (_cg == null) return;

            // 2. 同樣計算動態時間，避免淡入到一半被攔截時，突然變太慢
            float calculatedDuration = _data.tweenData.duration;
            if (_tween != null && _tween.IsActive())
            {
                calculatedDuration = _tween.ElapsedPercentage() * _data.tweenData.duration;
            }

            Stop();
            _tween = _cg.DOFade(0, calculatedDuration).SetEase(_data.tweenData.ease);
        }
    } */
}