using DG.Tweening;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;

namespace VzDev.DotweenUtils
{
    /// Tweener 移動的「設定檔」，負責定義動畫的內容（起點、終點、持續時間、緩動方式等等），但不直接操作場景中的物件。
    [CreateAssetMenu(fileName = "DOTweenMoveData", menuName = "VzDev/DOTween/DOTweenMoveData")]
    public class DOTweenMoveData : DOTweenBaseData
    {
        public bool setFromValue;
        public bool setToValue = true;
        [ShowIf(nameof(setFromValue))] public Vector2 fromValue;

        [ShowIf(nameof(setToValue))] public Vector2 toValue;

        /// 幫這個 target 專門打造一個負責移動的 Worker，以確保不同物件的動畫狀態不會互相干擾。
        public override ITweenWorker CreateWorker(GameObject target) => new MoveWorker(target, this);
    }

    // 實際負責執行與紀錄狀態的內部類別 (每個物件獨立一份，不會打架)
    public class MoveWorker : ITweenWorker
    {
        #region Variables
        private RectTransform _rectTarget;
        private DOTweenMoveData _data;
        private Vector2 _originalPos;
        private Tween _tween;
        #endregion

        public MoveWorker(GameObject target, DOTweenMoveData data)
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

        public void Play(UnityEvent onStart, UnityEvent onComplete)
        {
            if (_rectTarget == null) return;
            Stop();


            Vector2 toPos = _data.setToValue ? _data.toValue : _originalPos;
            _tween = _rectTarget.DOAnchorPos(toPos, _data.tweenData.duration)
                .SetEase(_data.tweenData.ease).SetDelay(_data.tweenData.delay)
                .OnStart(() => onStart?.Invoke())
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
            float calculatedDuration = _data.tweenData.duration;
            if (_tween != null && _tween.IsActive())
            {
                // 如果原本的動畫播到一半被攔截，我們按比例縮短回去的時間，避免移動速度突然變慢
                calculatedDuration = _tween.ElapsedPercentage() * _data.tweenData.duration;
            }

            Stop();
            _tween = _rectTarget.DOAnchorPos(targetPos, calculatedDuration).SetEase(_data.tweenData.ease);
        }
    }
}