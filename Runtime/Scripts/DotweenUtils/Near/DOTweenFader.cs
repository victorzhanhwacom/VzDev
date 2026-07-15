using DG.Tweening;
using NaughtyAttributes;
using UnityEngine;
using VzDev.DotweenUtils;

namespace VzDev.DotweenUtils1
{
    ///DOTween淡入淡出效果
    [RequireComponent(typeof(CanvasGroup))]
    public class DOTweenFader : MonoBehaviour
    {
        #region Variables

        [Label("[Events]"), ShowIf(nameof(IsHaveTweenData))] public DOTweenEvents events;
        [Foldout("[Settings]"), SerializeField] private bool isHideOnAwake = true;
        [Foldout("[Settings]"), Expandable, SerializeField] private DOTweenSettingSO tweenData;
        [Foldout("[Components]"), SerializeField] private CanvasGroup canvasGroup;

        private Tween _tween;

        private bool IsHaveTweenData => tweenData != null;
        private bool IsHaveTweenDataAndPlaying => IsHaveTweenData && Application.isEditor && Application.isPlaying;
        #endregion

        private void OnValidate()
        {
            if (canvasGroup == null) TryGetComponent(out canvasGroup);
        }
        private void Awake()
        {
            OnValidate();
            if (canvasGroup == null) Debug.LogWarning($"[DOTweenFader] `{name}` doesn't have a CanvasGroup component.");
            if (tweenData == null) Debug.LogWarning($"[DOTweenFader] `{name}` doesn't have tween data assigned.");
            if (isHideOnAwake && canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                SetCanvasGroupInteractable(false);
            }
        }


        [Button, ShowIf(nameof(IsHaveTweenDataAndPlaying))]
        public void PlayTween()
        {
            if (canvasGroup == null || tweenData == null) return;
            if (Mathf.Approximately(canvasGroup.alpha, 1f)) return;
            StopTween();

            gameObject.SetActive(true);
            SetCanvasGroupInteractable(false);

            _tween = canvasGroup.DOFade(1f, tweenData.doTweenSetting.duration)
                .SetEase(tweenData.doTweenSetting.easeOut)
                .SetDelay(tweenData.doTweenSetting.Delay)
                .OnStart(() => events.onStart?.Invoke())
                .OnComplete(() =>
                {
                    SetCanvasGroupInteractable(true);
                    events.onComplete?.Invoke();
                });
        }

        [Button, ShowIf(nameof(IsHaveTweenDataAndPlaying))]
        public void PlayBackwards()
        {
            if (canvasGroup == null || tweenData == null) return;
           // if (Mathf.Approximately(canvasGroup.alpha, 0f)) return;
            StopTween();

            gameObject.SetActive(true);
            SetCanvasGroupInteractable(false);

            _tween = canvasGroup.DOFade(0f, tweenData.doTweenSetting.duration)
                .SetEase(tweenData.doTweenSetting.easeIn);
        }

        /// 根據傳入的布林值 isOn 來決定是播放淡入動畫還是淡出動畫
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

        private void SetCanvasGroupInteractable(bool isInteractable)
        {
            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = canvasGroup.interactable = isInteractable;
            }
        }
    }
}