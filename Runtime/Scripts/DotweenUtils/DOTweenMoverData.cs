using DG.Tweening;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;

namespace VzDev.DotweenUtils
{
    /// Tweener 移動的「設定檔」，負責定義動畫的內容（起點、終點、持續時間、緩動方式等等），但不直接操作場景中的物件。
    [CreateAssetMenu(fileName = "DOTweenMover", menuName = "VzDev/DOTween/Mover")]
    public class DOTweenMoverData : DOTweenBaseData
    {
        public bool setFromValue;
        public bool setToValue = true;
        [ShowIf(nameof(setFromValue))] public Vector2 fromValue;

        [ShowIf(nameof(setToValue))] public Vector2 toValue;

        /// 幫這個 target 專門打造一個負責移動的 Worker，以確保不同物件的動畫狀態不會互相干擾。
        public override ITweenWorker CreateWorker(GameObject target) => new MoverWorker(target, this);
    }

    // 實際負責執行與紀錄狀態的內部類別 (每個物件獨立一份，不會打架)
    public class MoverWorker : ITweenWorker
    {
        #region Variables
        private RectTransform _rectTarget;
        private DOTweenMoverData _data;
        private Vector2 _originalPos;
        private Tween _tween;
        #endregion

        public MoverWorker(GameObject target, DOTweenMoverData data)
        {
            if (target.TryGetComponent(out _rectTarget))
            {
                _data = data;

                _originalPos = _rectTarget.anchoredPosition;
                if (_data.setFromValue) _rectTarget.anchoredPosition = _data.fromValue;
            }
            else
            {
                Debug.LogWarning($"[DOTweenMoverData] The target `{target.name}` doesn't have a RectTransform component.");
            }
        }

        public void Play(UnityEvent onComplete)
        {
            if (_rectTarget == null) return;
            Stop();


            Vector2 toPos = _data.setToValue ? _data.toValue : _originalPos;
            _tween = _rectTarget.DOAnchorPos(toPos, _data.duration)
                .SetEase(_data.ease).SetDelay(_data.delay)
                .OnComplete(() => onComplete?.Invoke());
        }

        public void Stop()
        {
            if (_tween != null && _tween.IsActive()) _tween.Kill();
            _tween = null;
        }

        public void PlayBackwards()
        {
            if (_rectTarget == null) return;
            // 1. 決定目標：如果沒勾 setFromValue，就安全退回到初始位置
            Vector2 targetPos = _data.setFromValue ? _data.fromValue : _originalPos;

            // 2. 決定動態時間 (進階優化，自由選擇是否保留)
            float calculatedDuration = _data.duration;
            if (_tween != null && _tween.IsActive())
            {
                // 如果原本的動畫播到一半被攔截，我們按比例縮短回去的時間，避免移動速度突然變慢
                calculatedDuration = _tween.ElapsedPercentage() * _data.duration;
            }

            Stop();
            _tween = _rectTarget.DOAnchorPos(targetPos, calculatedDuration).SetEase(_data.ease);
        }
    }
}