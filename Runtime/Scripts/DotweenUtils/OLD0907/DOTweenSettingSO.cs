using System;
using DG.Tweening;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

namespace VzDev.DotweenUtils1
{
    [Serializable]
    public class DOTweenSetting
    {
        public float duration = 0.3f;
        [SerializeField] private float delayValue;
        public bool IsRandomDelay;
        public float delayRandomMin;
        public Ease easeOut = Ease.OutQuad;
        public Ease easeIn = Ease.InQuad;
        public bool isLoop;
        public int loopTimes = -1;
        public LoopType loopType = LoopType.Yoyo;

        public float Delay => IsRandomDelay ? Random.Range(delayRandomMin, delayValue) : delayValue;
    }

    [CreateAssetMenu(fileName = "DOTweenSettingSO", menuName = "VzDev/DOTween/DOTweenSettingSO")]
    public class DOTweenSettingSO : ScriptableObject
    {
        public DOTweenSetting doTweenSetting;
        public static implicit operator DOTweenSetting(DOTweenSettingSO so) => so.doTweenSetting;
         // 這樣就可以直接把 DOTweenSettingSO 當成 DOTweenSetting 用了，方便在程式碼裡使用
    }

    public enum EnumDOTweenDataType
    {
        ScriptableObject,
        Class
    }
}