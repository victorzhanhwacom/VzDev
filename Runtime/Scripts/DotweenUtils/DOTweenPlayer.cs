using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;

namespace VzDev.DotweenUtils
{
    /// Tweener 的「播放器」，負責播放tweenData動畫
    public class DOTweenPlayer : MonoBehaviour
    {
        #region Variables
        [Foldout("[Events]"), ShowIf(nameof(IsHaveTweenData))] public UnityEvent onComplete;
        [Foldout("[Settings]"), Expandable, SerializeField] private DOTweenBaseData tweenData; // 可以拖入 Mover, Fader, 或是 Rotate 
        private bool IsHaveTweenData => tweenData != null;
        private bool IsHaveTweenDataAndPlaying => tweenData != null && _worker != null;
        private ITweenWorker _worker;
        #endregion


        [Button, ShowIf(nameof(IsHaveTweenDataAndPlaying))]
        public void PlayTween() => _worker?.Play(onComplete);
          
        [Button, ShowIf(nameof(IsHaveTweenDataAndPlaying))]
        public void PlayBackwards() => _worker?.PlayBackwards();

        [Button, ShowIf(nameof(IsHaveTweenDataAndPlaying))]
        public void StopTween() => _worker?.Stop();

        [Button("RefreshWorker"), ShowIf(nameof(IsHaveTweenDataAndPlaying))]
        private void Awake()
        {
            if (tweenData != null)
            {
                // 讓設定檔根據我的 GameObject，生出一個專屬於我的 Worker 邏輯實例
                _worker = tweenData.CreateWorker(gameObject);
            }
        }

        private void OnDestroy() => StopTween();
    }
}