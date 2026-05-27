using DG.Tweening;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;

namespace VzDev.DotweenUtils
{
    [CreateAssetMenu(fileName = "DOTweenFader", menuName = "VzDev/DOTween/Fader")]
    public class DOTweenFaderData : DOTweenBaseData
    {
        public bool isHideOnAwake = true;
        public bool isForceFrom;
        [ShowIf(nameof(isForceFrom))] public float fromAlpha = 0f;
        public float toAlpha = 1f;

        public override ITweenWorker CreateWorker(GameObject target) => new FaderWorker(target, this);
    }

    public class FaderWorker : ITweenWorker
    {
        private CanvasGroup _cg;
        private DOTweenFaderData _data;
        private Tween _tween;

        public FaderWorker(GameObject target, DOTweenFaderData data)
        {
            if (target.TryGetComponent(out _cg))
            {
                _data = data;
                if (_data.isHideOnAwake) _cg.alpha = 0f;
            }
            else
            {
                Debug.LogWarning($"[DOTweenFaderData] The target `{target.name}` doesn't have a CanvasGroup component.");
            }
        }

        public void Play(UnityEvent onComplete)
        {
            if (_cg == null) return;
            Stop();

            if (_data.isForceFrom) _cg.alpha = _data.fromAlpha;
            _tween = _cg.DOFade(_data.toAlpha, _data.duration)
                .SetEase(_data.ease)
                .SetDelay(_data.delay)
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
            float calculatedDuration = _data.duration;
            if (_tween != null && _tween.IsActive())
            {
                calculatedDuration = _tween.ElapsedPercentage() * _data.duration;
            }

            Stop();
            _tween = _cg.DOFade(0, calculatedDuration).SetEase(_data.ease);
        }
    }
}