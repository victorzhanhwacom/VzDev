using System.Collections;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

namespace VzDev.DemoUtils.AutoJumper
{
    /// 自動亂數跳值 - 數值
    public class ValueAutoJumper : MonoBehaviour
    {
        #region Variables

        [Foldout("[Event] - Invoke字串")] public UnityEvent<string> onValueChangedString;
        [Foldout("[Event] - Invoke整數")] public UnityEvent<int> onValueChangedInt;
        [Foldout("[Event] - Invoke浮點數")] public UnityEvent<float> onValueChangedFloat;
        [Foldout("[Event] - Invoke浮點數01")] public UnityEvent<float> onValueChangedFloat01;

        [Foldout("[設定]"), SerializeField] private bool isStartInEnabled = true;

        [Foldout("[設定]"), SerializeField, Label("更新時間間隔(0=只改一次)")]
        private float intervalSec = 10f;

        [Foldout("[設定]"), SerializeField, Label("小數點後幾位")]
        private int afterDotNumber = 2;

        [Foldout("[設定]"), SerializeField] private float minValue, maxValue = 100f;
        [Foldout("[設定]"), SerializeField] private bool isClamp;
        [Foldout("[設定]"), SerializeField, ShowIf(nameof(isClamp))] private Vector2 clampRange;
        private Coroutine coroutine;
        private string DotFormat => (afterDotNumber > 0) ? "." + new string('#', afterDotNumber) : "";

        /// NaughtAttribute使用Application.isPlaying會有Warning
        private bool IsPlaying => Application.isPlaying;

        private bool IsHaveAnyEventListeners => true;

        #endregion

        #region Initialized
        private void OnEnable()
        {
            if (isStartInEnabled) StartJump();
        }

        private void OnDisable() => StopJump();

        #endregion

        #region AutoJump

        [Button, ShowIf(nameof(IsPlaying))]
        private void StopJump()
        {
            if (coroutine != null) StopCoroutine(coroutine);
            coroutine = null;
        }

        [Button, ShowIf(nameof(IsPlaying))]
        private void StartJump()
        {
            StopJump();

            if (intervalSec == 0)
            {
                ValueUpdate();
                return;
            }
            coroutine = StartCoroutine(JumpValueCoroutine());

            IEnumerator JumpValueCoroutine()
            {
                while (true)
                {
                    ValueUpdate();
                    yield return new WaitForSeconds(intervalSec);
                }
            }
        }

        #endregion

        /// 設置Value
        [Button, ShowIf(nameof(IsHaveAnyEventListeners))]
        public void ValueUpdate()
        {
            float value = Random.Range(isClamp? clampRange.x : minValue, isClamp? clampRange.y : maxValue);
            float multiplier = Mathf.Pow(10f, afterDotNumber);
            float percent01 = (value-minValue) / (maxValue-minValue);
            onValueChangedInt?.Invoke(Mathf.RoundToInt(value));
            onValueChangedFloat?.Invoke(Mathf.Round(value * multiplier) / multiplier);
            onValueChangedString?.Invoke(value.ToString($"0{DotFormat}"));
            onValueChangedFloat01?.Invoke(percent01);
        }
    }
}