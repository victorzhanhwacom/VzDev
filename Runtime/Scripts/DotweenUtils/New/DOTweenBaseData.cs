using DG.Tweening;
using UnityEngine;

namespace VzDev.DotweenUtils1
{
    [CreateAssetMenu(fileName = "DOTweenBaseData", menuName = "VzDev/DOTween/BaseData")]
    public class DOTweenBaseData : ScriptableObject
    {
        public float duration = 0.3f, delay;
        public Ease easeOut = Ease.OutQuad, easeIn = Ease.InQuad;
    }
}