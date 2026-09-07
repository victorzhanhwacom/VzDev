using DG.Tweening;
using UnityEngine;

namespace VzDev.DOTweenUtils
{
    [CreateAssetMenu(fileName = "DOTweeParamsSO", menuName = "VzDev/DOTween/DOTweeParamsSO", order = 1)]
    public class DOTweeParamsSO : ScriptableObject
    {
        public float duration = 0.3f;
        public Ease easeOut = Ease.OutQuad;
        public Ease easeIn = Ease.InQuad;
        public bool isRandomDelay;

        public float delay = 0f;
        public Vector2 delayRandomRange = new Vector2(0f, 1f);
        public bool isLoop;

        public int loopTimes = -1;
        public LoopType loopType = LoopType.Yoyo;

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
