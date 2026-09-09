using DG.Tweening;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

namespace VzDev.DOTweenUtils
{
    public abstract class DOTweenBase : MonoBehaviour, IVisible
    {
        #region Events
        /// <summary>
        /// onComplete, bool true: EaseOut / false: EaseIn
        /// </summary>
        [Foldout("[Tween Event]")] public UnityEvent<bool> onComplete;
        [Foldout("[Tween Event]")] public UnityEvent onUpdate, onStart;
        #endregion

        #region TweenParams Fields
        [SerializeField] private bool isAutoShowOnEnable = true;
        [Foldout("[Tween]"), SerializeField, Label(">>Tween數值設定檔SO"), Expandable] protected DOTweeParamsSO tweenParamsSO;
        private bool isHaveSO => tweenParamsSO != null;
        [Space(10)]
        [Foldout("[Tween]"), SerializeField, HideIf("isHaveSO")] private float duration = 0.3f;
        protected float Duration => isHaveSO ? tweenParamsSO.duration : duration;
        [Foldout("[Tween]"), SerializeField, HideIf("isHaveSO")] protected Ease easeOut = Ease.OutQuad;
        [Foldout("[Tween]"), SerializeField, HideIf("isHaveSO")] protected Ease easeIn = Ease.InQuad;
        [Foldout("[Tween]"), SerializeField, HideIf("isHaveSO")] protected bool isRandomDelay;
        private bool showDelay => !isHaveSO && !isRandomDelay;
        private bool showDelayRandomRange => !isHaveSO && isRandomDelay;

        [Foldout("[Tween]"), SerializeField, ShowIf("showDelay")] protected float delay = 0f;
        [Foldout("[Tween]"), SerializeField, ShowIf("showDelayRandomRange")] protected Vector2 delayRandomRange = new Vector2(0f, 1f);
        [Foldout("[Tween]"), SerializeField, HideIf("isHaveSO")] protected bool isLoop;
        private bool showLoopOptions => !isHaveSO && isLoop;

        [Foldout("[Tween]"), SerializeField, ShowIf("showLoopOptions")] protected int loopTimes = -1;
        [Foldout("[Tween]"), SerializeField, ShowIf("showLoopOptions")] protected LoopType loopType = LoopType.Yoyo;
        protected Tween tween;
        #endregion

        #region 設定Visible
        [Button]
        public void Hide() => SetVisible(false);
        [Button]
        public void Show() => SetVisible(true);
        public abstract void SetVisible(bool isVisible);
        #endregion

        /// <summary>
        /// 將參數套用到 Tween 上
        /// </summary>
        protected Tween SetTweenParams(Tween tween, bool isEaseOut)
        {
            if (tween == null) return null;
            if (tweenParamsSO != null)
            {
                tween = tweenParamsSO.SetTweenParams(tween, isEaseOut);
            }
            else
            {
                if(isEaseOut)tween.SetDelay(isRandomDelay ? Random.Range(delayRandomRange.x, delayRandomRange.y) : delay);
                tween.SetEase(isEaseOut ? easeOut : easeIn);
                if (isLoop) tween.SetLoops(loopTimes, loopType);
            }
            tween.OnStart(() => onStart?.Invoke());
            tween.OnUpdate(() => onUpdate?.Invoke());
            tween.OnComplete(() => onComplete?.Invoke(isEaseOut));
            return tween;
        }

        [Button]
        protected void StopTween()
        {
            if (tween != null && tween.IsActive()) tween.Kill();
            tween = null;
        }

        protected virtual void OnEnable()
        {
            if (isAutoShowOnEnable) Show();
        }

    }

    public interface IVisible
    {
        void Show();
        void Hide();
    }
}
