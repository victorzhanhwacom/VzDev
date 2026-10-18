using DG.Tweening;
using NaughtyAttributes;
using TMPro;
using UnityEngine;
using VzDev.DOTweenUtils.Extensions;

namespace VzDev
{
    public class Test : MonoBehaviour
    {
        public TextMeshProUGUI powerText;
        private Tweener _powerTween;
        public float _power = 0;
        public float newValue, duration;
        public int decimals;
        public Ease ease;

        public Vector2 randomRangeX = new Vector2(0, 1000);

        public string format = "{0:0}";
        public string surfix = "°C";

        public bool isTrimZeros = false;

        [Button]
        public void UpdatePower()
        {
            newValue = Random.Range(randomRangeX.x, randomRangeX.y);
            _powerTween?.Kill();   // Tweener 不是 UnityEngine.Object，?. 在這裡安全
            _powerTween = powerText.DONumber(_power, newValue, duration, decimals, surfix, isTrimZeros).SetEase(ease);
            _power = newValue;
        }
        
        [Button]
        public void SetFormat()
        {
            powerText.SetText(format+surfix, _power);
        }
    }
}
