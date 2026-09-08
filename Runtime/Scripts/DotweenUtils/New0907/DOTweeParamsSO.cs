using DG.Tweening;
using NaughtyAttributes;
using UnityEngine;

namespace VzDev.DOTweenUtils
{
    [CreateAssetMenu(fileName = "DOTweeParamsSO", menuName = "VzDev/DOTween/DOTweeParamsSO", order = 1)]
    public class DOTweeParamsSO : ScriptableObject
    {
        [Foldout("[Tween]")] public float duration = 0.3f;
        [Foldout("[Tween]"), SerializeField] protected Ease easeOut = Ease.OutQuad;
        [Foldout("[Tween]"), SerializeField] protected Ease easeIn = Ease.InQuad;
        [Foldout("[Tween]"), SerializeField] protected bool isRandomDelay;

        [Foldout("[Tween]"), SerializeField, HideIf("isRandomDelay")] protected float delay = 0f;
        [Foldout("[Tween]"), SerializeField, ShowIf("isRandomDelay")] protected Vector2 delayRandomRange = new Vector2(0f, 1f);
        [Foldout("[Tween]"), SerializeField] protected bool isLoop;
        [Foldout("[Tween]"), SerializeField, ShowIf("isLoop")] protected int loopTimes = -1;
        [Foldout("[Tween]"), SerializeField, ShowIf("isLoop")] protected LoopType loopType = LoopType.Yoyo;

        public Tween SetTweenParams(Tween tween, bool isEaseOut)
        {
            if (tween == null) return null;
            tween.SetDelay(isRandomDelay ? Random.Range(delayRandomRange.x, delayRandomRange.y) : delay);
            tween.SetEase(isEaseOut ? easeOut : easeIn);
            if (isLoop) tween.SetLoops(loopTimes, loopType);
            return tween;
        }
    }
}
