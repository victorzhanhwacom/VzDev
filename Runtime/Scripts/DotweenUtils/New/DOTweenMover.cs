using DG.Tweening;
using NaughtyAttributes;
using UnityEngine;

namespace VzDev.DotweenUtils
{
    /// <summary>
    /// DOTweenMover 是一個專門用來控制 RectTransform 位移的工具類別，利用 DOTween 來實現平滑的動畫效果。它提供了靈活的設定選項，可以讓你輕鬆地在 Unity 編輯器中調整動畫的行為和參數。
    /// </summary>
    public class DOTweenMover : MonoBehaviour
    {
        #region Variables

        [Label("[Events]")] public DOTweenEvents dotweenEvents;

        [Foldout("[Settings]"), SerializeField] private EnumDOTweenDataType doTweenDataType;
        [Foldout("[Settings]"), SerializeField, Expandable, ShowIf(nameof(IsDOTweenDataSO))] private DOTweenSettingSO doTweenSettingSO;
        [Foldout("[Settings]"), SerializeField, HideIf(nameof(IsDOTweenDataSO))] private DOTweenSetting doTweenSetting;
        private DOTweenSetting CurrentDOTweenSetting => doTweenSettingSO != null ? doTweenSettingSO.doTweenSetting : doTweenSetting;

        [Foldout("[Pos Settings]"), SerializeField] private bool setFromValueX, setFromValueY = true;
        [Foldout("[Pos Settings]"), SerializeField, ShowIf(nameof(setFromValueX))] private float fromValueX;
        [Foldout("[Pos Settings]"), SerializeField, ShowIf(nameof(setFromValueY))] private float fromValueY;
        [Foldout("[Pos Settings]"), SerializeField] private bool setToValueX, setToValueY = true;
        [Foldout("[Pos Settings]"), SerializeField, ShowIf(nameof(setToValueX))] private float toValueX;
        [Foldout("[Pos Settings]"), SerializeField, ShowIf(nameof(setToValueY))] private float toValueY;

        [Foldout("[Comopnents]"), SerializeField] private RectTransform rectTarget;

        private Tween _tween;
        private Vector2 _originalPos;
        private bool isTweenOn;

        #endregion

        #region NaughtyAttributes Conditions
        private bool IsDOTweenDataSO => doTweenDataType == EnumDOTweenDataType.ScriptableObject;
        private bool IsAbleToPlayTween => rectTarget != null && Application.isPlaying;
        private bool IsTweenPlaying => _tween != null && _tween.IsActive() && _tween.IsPlaying();
        #endregion

        #region Unity Callbacks
        private void OnValidate()
        {
            if (rectTarget == null) TryGetComponent(out rectTarget);
        }

        private void Awake()
        {
            OnValidate();
            if (rectTarget == null) Debug.LogWarning($"[DOTweenMover] `{name}` doesn't have a RectTransform component.");

            if (setToValueX || setToValueY) // 如果有設定結束值，就直接把物件移動到結束位置，確保在編輯器中能看到正確的起始狀態
            {
                _originalPos = new Vector2(setToValueX ? toValueX : _originalPos.x, setToValueY ? toValueY : _originalPos.y);
            }
            else
            {
                _originalPos = rectTarget.anchoredPosition;
            }
            if ((setFromValueX || setFromValueY) && !(setToValueX || setToValueY)) // 如果有設定起始值，但沒有設定結束值，就安全退回到初始位置
            {
                rectTarget.anchoredPosition = new Vector2(setFromValueX ? fromValueX : _originalPos.x, setFromValueY ? fromValueY : _originalPos.y);
            }
        }
        #endregion

        public void SetIsOn(bool isOn)
        {
            if (isOn) PlayTween();
            else PlayBackwards();
        }

        [Button, ShowIf(nameof(IsAbleToPlayTween))]
        public void PlayTween()
        {
            //if (rectTarget == null || isTweenOn) return;
            if (rectTarget == null) return;
            isTweenOn = true;

            Vector2 toPos = (setFromValueX || setFromValueY) ? _originalPos
            : new Vector2(setToValueX ? toValueX : _originalPos.x, setToValueY ? toValueY : _originalPos.y);

            _tween = ToTween(toPos, CurrentDOTweenSetting.duration)
                .OnStart(() => dotweenEvents.onStart?.Invoke())
                .OnComplete(() => dotweenEvents.onComplete?.Invoke())
                .OnUpdate(() => dotweenEvents.onUpdate?.Invoke());
        }

        /// <summary>
        /// 進階優化：如果原本的動畫播到一半被攔截，我們按比例縮短回去的時間，避免移動速度突然變慢
        /// </summary>
        [Button, ShowIf(nameof(IsAbleToPlayTween))]
        public void PlayBackwards()
        {
            //if (rectTarget == null || !isTweenOn) return;
            if (rectTarget == null) return;
            isTweenOn = false;
            // 1. 決定目標：如果沒勾 setFromValue，就安全退回到初始位置
            Vector2 toPos = new Vector2(setFromValueX ? fromValueX : _originalPos.x, setFromValueY ? fromValueY : _originalPos.y);

            // 2. 決定動態時間 (進階優化，自由選擇是否保留)
            float calculatedDuration = CurrentDOTweenSetting.duration;
            if (_tween != null && _tween.IsActive())
            {
                // 如果原本的動畫播到一半被攔截，我們按比例縮短回去的時間，避免移動速度突然變慢
                calculatedDuration = _tween.ElapsedPercentage() * CurrentDOTweenSetting.duration;
            }
            Debug.Log($"[DOTweenMover] Playing backwards with calculated duration: {calculatedDuration}");
            _tween = ToTween(toPos, calculatedDuration);
        }

        /// <summary>
        /// 進階優化：把重複的 DOTween 設定抽出來，讓 PlayTween 和 PlayBackwards 都能共用，確保兩者的動畫行為完全一致
        /// </summary>
        private Tween ToTween(Vector2 toPos, float duration)
        {
            StopTween();

            _tween = rectTarget.DOAnchorPos(toPos, duration).SetEase(CurrentDOTweenSetting.easeIn);
            if (CurrentDOTweenSetting.Delay > 0) _tween.SetDelay(CurrentDOTweenSetting.Delay);
            if (CurrentDOTweenSetting.isLoop) _tween.SetLoops(CurrentDOTweenSetting.loopTimes, CurrentDOTweenSetting.loopType);
            return _tween;
        }

        [Button, EnableIf(nameof(IsTweenPlaying))]
        public void StopTween()
        {
            if (_tween != null && _tween.IsActive()) _tween.Kill();
            _tween = null;
        }
        private void OnDestroy() => StopTween();
        private void OnDisable() => StopTween();
    }
}