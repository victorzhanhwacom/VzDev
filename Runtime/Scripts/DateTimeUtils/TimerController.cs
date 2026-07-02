using System.Collections;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;

namespace VzDev.DateTimeUtils
{
    /// <summary>
    /// Timer控制器，支援記錄剩餘時間以實現Pause/Resume功能，並且在每個週期結束時觸發事件。
    /// </summary>
    public class TimerController : MonoBehaviour
    {
        #region Variables
        [Foldout("[Events]"), Tooltip("Timer開始時觸發")] public UnityEvent onTimerStart;
        [Foldout("[Events]"), Tooltip("每次Loop時觸發")] public UnityEvent<int> onTimerUpdate;
        [Foldout("[Events]"), Tooltip("循環結束時觸發"), HideIf(nameof(isInfiniteLoop))] public UnityEvent onTimerEnd;

        [Foldout("[Settings]"), SerializeField] private bool isActiveInStart = true;
        [Foldout("[Settings]"), SerializeField] private float timeValue = 10f;
        [Foldout("[Settings]"), SerializeField] private EnumTime timeUnit = EnumTime.秒;
        [Foldout("[Settings]"), SerializeField, HideIf(nameof(isInfiniteLoop))] private int maxLoopCount = 3; // ✅ 有限循環的最大次數
        [Foldout("[Settings]"), SerializeField] private bool isInfiniteLoop = true;
        [Space]
        [Foldout("[Settings]"), SerializeField, ReadOnly] private int loopCount;
        [Foldout("[Settings]"), SerializeField, ReadOnly] private float remainingTime;  // ✅ 用於 Pause/Resume

        private Coroutine timerCoroutine;
        private bool isPaused;
        #endregion

        #region NaughtyAttributes Conditions
        private bool IsEnableToPlay => Application.isPlaying && timerCoroutine == null && !isPaused;
        private bool IsEnableToStop => Application.isPlaying && (timerCoroutine != null || isPaused);
        private bool IsEnableToPause => Application.isPlaying && timerCoroutine != null && !isPaused;
        private bool IsEnableToResume => Application.isPlaying && isPaused;
        #endregion

        private void Start()
        {
            if (isActiveInStart) StartTimer(); // ✅ 實作 isActiveInStart
        }

        [Button, ShowIf(nameof(IsEnableToPlay))]
        public void StartTimer()
        {
            loopCount = 0;
            remainingTime = timeValue * GetTimeUnitMultiplier(timeUnit);
            isPaused = false;
            timerCoroutine = StartCoroutine(TimerCoroutine(remainingTime));
        }

        [Button, ShowIf(nameof(IsEnableToStop))]
        public void StopTimer()
        {
            if (timerCoroutine != null)
            {
                StopCoroutine(timerCoroutine);
                timerCoroutine = null;
            }
            isPaused = false;
            remainingTime = 0f;
        }

        [Button, ShowIf(nameof(IsEnableToPause))]
        public void PauseTimer()
        {
            if (timerCoroutine != null)
            {
                StopCoroutine(timerCoroutine);
                timerCoroutine = null; // ✅ 清除 reference
                isPaused = true;
            }
        }

        [Button, ShowIf(nameof(IsEnableToResume))]
        public void ResumeTimer()
        {
            if (!isPaused) return;
            isPaused = false;
            timerCoroutine = StartCoroutine(TimerCoroutine(remainingTime)); // ✅ 從剩餘時間繼續
        }

        private IEnumerator TimerCoroutine(float duration)
        {
            onTimerStart?.Invoke();
            
            float elapsed = 0f;
            float interval = timeValue * GetTimeUnitMultiplier(timeUnit); // ✅ 快取，避免每幀重複計算

            while (true)
            {
                yield return null;
                elapsed += Time.deltaTime;
                remainingTime = duration - elapsed;

                if (elapsed < duration) continue; // ✅ 未到週期，直接跳過後續邏輯

                // ✅ 週期結束
                elapsed = 0f;
                duration = interval;
                loopCount++;
                onTimerUpdate?.Invoke(loopCount);

                if (!isInfiniteLoop && loopCount >= maxLoopCount)
                {
                    timerCoroutine = null;
                    onTimerEnd?.Invoke();
                    yield break; // ✅ 用 yield break 取代 break，不會跑到下面的區塊
                }
            }
        }

        private float GetTimeUnitMultiplier(EnumTime timeUnit)
        {
            return timeUnit switch
            {
                EnumTime.秒 => 1f,
                EnumTime.分 => 60f,
                EnumTime.時 => 3600f,
                _ => 1f
            };
        }

        private void OnEnable() => ResumeTimer(); // ✅ 確保在物件被啟用時恢復計時器，如果之前是暫停狀態
        private void OnDisable() => PauseTimer(); // ✅ 確保在物件被禁用時停止計時器，避免 Coroutine 繼續運行
    }

    public enum EnumTime
    {
        秒,
        分,
        時
    }
}