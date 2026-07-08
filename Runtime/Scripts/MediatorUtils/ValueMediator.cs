using VzDev.MathUtils;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;

namespace VzDev.DebugUtils
{
    /// [Mediator] - 數值轉接器
    public class ValueMediator : MonoBehaviour
    {
        #region Variabls

        [Foldout("發送字串")] public UnityEvent<string> invokeString;
        [Foldout("發送float")] public UnityEvent<float> invokeFloat;
        [Foldout("發送float01")] public UnityEvent<float> invokeFloat01;
        [Foldout("發送Integer")] public UnityEvent<float> invokeInteger;
        [Foldout("發送Bool(isOverThreshold)")] public UnityEvent<bool> invokeBool, invokeBoolReverse;
        [Foldout("發送By狀態")] public UnityEvent invokeInNormal, invokeOverThreshold, invokeInMax;

        [Foldout("[設定]"), SerializeField, Label("小數點後幾位")]
        private int dotNumber;

        [Foldout("[設定]"), SerializeField, Label("最小值(01計算)")]
        private int minValue;

        [Foldout("[設定]"), SerializeField, Label("最大值(01計算)")]
        private int maxValue;

        [Foldout("[設定]"), SerializeField, Label("門檻值(bool判斷)")]
        private float thresholdValue = 60;

        private float currentValue;

        #endregion

        #region Setter
        public void SetDotNumber(int dot) => dotNumber = dot;
        public void SetMinValue(int value) => minValue = value;
        public void SetMaxValue(int value) => maxValue = value;
        public void SetThresholdValue(float value) => thresholdValue = value;
        #endregion

        /// 設定字串
        public void SetString(string stringValue)
        {
            if (float.TryParse(stringValue, out float floatResult))
                SetValue(floatResult);
            else
                Debug.Log($"字串[{stringValue}]無法轉成float值", this);
        }

        /// 設定值(int)
        public void SetValue(int value) => SetValue((float)value);

        /// 設定值(float)
        public void SetValue(float value)
        {
            this.currentValue = value;
            InvokeValueHandler();
        }
        
        public float Value {set=>SetValue(value);}

        /// 統一發送事件
        private void InvokeValueHandler()
        {
            invokeString?.Invoke(MathHelper.ToDotNumberString(currentValue, dotNumber));
            invokeFloat?.Invoke(MathHelper.ToDotNumberFloat(currentValue, dotNumber));
            invokeFloat01?.Invoke(MathHelper.ToPercent01(currentValue-minValue, maxValue-minValue, dotNumber));
            invokeInteger?.Invoke(Mathf.RoundToInt(currentValue));

            bool isOverThreshold = currentValue >= thresholdValue;
            invokeBool?.Invoke(isOverThreshold);
            invokeBoolReverse?.Invoke(!isOverThreshold);
            
            if(Mathf.Approximately(currentValue, maxValue)) invokeInMax?.Invoke();
            else if(isOverThreshold) invokeOverThreshold?.Invoke();
            else invokeInNormal?.Invoke();
        }
    }
}