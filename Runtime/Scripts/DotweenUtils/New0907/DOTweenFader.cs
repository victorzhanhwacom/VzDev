using DG.Tweening;
using NaughtyAttributes;
using UnityEngine;
using VzDev.CanvasUtils;

namespace VzDev.DOTweenUtils
{
    [RequireComponent(typeof(CanvasGroup))]
    [RequireComponent(typeof(CanvasGroupMediator))]
    public class DOTweenFader : DOTweenBase
    {
        #region Fields
        [Foldout("[Components]"), SerializeField] private CanvasGroup canvasGroup;
        [Foldout("[Components]"), SerializeField] private CanvasGroupMediator canvasGroupMediator;
        #endregion

        private void Awake()
        {
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroupMediator == null) canvasGroupMediator = GetComponent<CanvasGroupMediator>();
        }

        public override void SetVisible(bool isVisible)
        {
            StopTween();
            tween = SetTweenParams(canvasGroup.DOFade(isVisible ? 1f : 0f, Duration), isVisible);
        }

        private void OnValidate() => Awake();

        #region Event Listeners
        private void OnEnable() => onUpdate.AddListener(OnUpdateHandler);
        private void OnDisable()
        {
            StopTween();
            onUpdate.RemoveListener(OnUpdateHandler);
        }

        private void OnUpdateHandler() => canvasGroupMediator.SetCanvasGroupAlpha(canvasGroup.alpha);
        #endregion
    }
}
