using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using Random = UnityEngine.Random;
using Debug = VzDev.ToolUtils.Debug;


namespace VzDev.DOTweenUtils
{
    public static class DOTweenHelper
    {
        private static readonly Dictionary<TextMeshProUGUI, Tweener> dict = new();
        
        public static void ToBlink(List<TextMeshProUGUI> targets, string showText = null, float duration = 0.1f,
            float delay = 0, bool isRandomDelay = false)
            => targets.ForEach(txt => ToBlink(txt, showText, duration, delay, isRandomDelay));

        /// 閃爍後顯示指定文字
        /// <para>+ showText：顯示指定文字</para>
        /// <para>+ duration若太低，效果會不明顯</para>
        public static Tween ToBlink(TextMeshProUGUI target, string showText = null, float duration = 0.1f,
            float delay = 0, bool isRandomDelay = false)
        {
            // 先殺掉舊的
            if (dict.TryGetValue(target, out Tweener oldTween) && oldTween != null)
                oldTween.Kill();

            // 重新設定透明度
            target.alpha = 0;

            if (showText != null) target.SetText(showText);

            // 建立新的 Tween
            float useDelay = isRandomDelay ? Random.Range(0f, delay) : delay;

            Tweener t = target.DOFade(1f, duration)
                .SetEase(Ease.OutBounce)
                .SetDelay(useDelay)
                .OnComplete(() =>
                {
                    dict.Remove(target);
                });

            dict[target] = t;
            return t;
        }

        public static Tween LerpValue(float startValue, float endValue, Action<float> onUpdate,
            float duration = 0.15f, float dealy = 0)
        {
            return DOTween.To(() => startValue, x =>
            {
                startValue = x;
                onUpdate.Invoke(startValue);
            }, endValue, duration).SetDelay(dealy).SetEase(Ease.OutQuad);
        }
        
        /// 待測試
        public static Tween DoFade(GameObject target, float duration, float endValue = 1, float fromValue = 0
            , float delay = 0, bool isRandomDelay = false, Ease ease = Ease.OutBounce)
        {
            if (target.TryGetComponent(out Renderer renderer))
            {
                DOTween.Kill(renderer.material);
                Color originalColor = renderer.material.color, toColor = renderer.material.color;
                originalColor.a = fromValue;
                toColor.a = endValue;
                
                // DOTween 淡入
                return renderer.material.DOColor(toColor, "_BaseColor", 1).From(originalColor)
                    .SetDelay(Random.Range(isRandomDelay ? 0 : delay, delay)).SetEase(ease); // 1秒淡入到不透明
                
                /*return renderer.material.DOFade(endValue, duration).From(0)
                    .SetDelay(Random.Range(isRandomDelay ? 0 : delay, delay)).SetEase(ease); // 1秒淡入到不透明*/
            }
            else
            {
                Debug.LogWarning("Target does not have a renderer");
                return null;
            }
        }

        /// NEW===========================================================================================
        public static Tween DoInt(TextMeshProUGUI target, int startValue, int endValue, float duration = 1f, string format = "N0",
            Ease ease = Ease.OutQuad)
        {
            return DOTween.To(() => startValue, x =>
            {
                startValue = x;
                target.SetText(startValue.ToString(format));
            }, endValue, duration).SetEase(ease);
        }

        public static Tween DoFloat(TextMeshProUGUI target, float startValue, float endValue, float duration = 1f,
            string formatter = "0.##", Ease ease = Ease.OutQuad)
        {
            return DOTween.To(() => startValue, x =>
            {
                startValue = x;
                target.SetText(startValue.ToString(formatter));
            }, endValue, duration).SetEase(ease);
        }
    }
}