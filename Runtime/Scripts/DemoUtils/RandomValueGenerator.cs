using System;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;
using VzDev.UnityAPI.Extensions;
using Random = UnityEngine.Random;

namespace VzDev.DemoUtils
{
    /// <summary>
    /// 隨機值生成器，可設定為生成 Float、Float01（0~1）或 Int，並可選擇是否使用加權隨機。
    /// </summary>
    public class RandomValueGenerator : MonoBehaviour
    {
        #region Variables

        [SerializeField] private ValueType valueType;

        [SerializeField, ReadOnly, HideIf(nameof(IsInt))] private float currentValueFloat;
        [SerializeField, ReadOnly, ShowIf(nameof(IsInt))] private int currentValueInt;
        [Foldout("[Events]")] public UnityEvent<string> OnValueChangedString;

        [Foldout("[Events]"), ShowIf(nameof(IsFloat))] public UnityEvent<float> OnValueChangedFloat;
        [Foldout("[Events]"), ShowIf(nameof(IsFloat01))] public UnityEvent<float> OnValueChangedFloat01;
        [Foldout("[Events]"), ShowIf(nameof(IsInt))] public UnityEvent<int> OnValueChangedInt;
        [Foldout("[Events]"), ShowIf(nameof(IsInt))] public UnityEvent<Single> OnValueChangedSingle;

        [Foldout("[Settings]"), SerializeField] private float minValue, maxValue = 100f;
        [Foldout("[Settings]"), HideIf(nameof(IsInt)), SerializeField, Range(0, 8)] private int decimalPlaces = 2;

        [Foldout("[Settings]"), SerializeField, Tooltip("是否使用加權隨機")] private bool useWeightedRandom = false;
        [Foldout("[Settings]"), ShowIf(nameof(useWeightedRandom)), Tooltip("隨機值加權設定"), SerializeField] private WeightedSegment[] segments;

        private float _range;
        private float[] _segmentCDF;

        #endregion

        #region NaughtyAttributes Conditions
        private bool IsFloat01 => valueType == ValueType.Float01;
        private bool IsInt => valueType == ValueType.Int;
        private bool IsFloat => valueType == ValueType.Float;

        private bool IsHaveEventListener => valueType switch
        {
            ValueType.Float => OnValueChangedFloat != null && OnValueChangedFloat.GetPersistentEventCount() > 0 ||
                                OnValueChangedString != null && OnValueChangedString.GetPersistentEventCount() > 0,
            ValueType.Float01 => OnValueChangedFloat01 != null && OnValueChangedFloat01.GetPersistentEventCount() > 0 ||
                                OnValueChangedFloat != null && OnValueChangedFloat.GetPersistentEventCount() > 0,
            ValueType.Int => OnValueChangedInt != null && OnValueChangedInt.GetPersistentEventCount() > 0 || 
                            OnValueChangedSingle != null && OnValueChangedSingle.GetPersistentEventCount() > 0 ||
                            OnValueChangedString != null && OnValueChangedString.GetPersistentEventCount() > 0,
            _ => false
        };
        #endregion

        [Button, ShowIf(nameof(IsHaveEventListener))]
        public void GenerateRandomValue()
        {
            switch (valueType)
            {
                case ValueType.Float: GenerateFloatValue(); break;
                case ValueType.Float01: GenerateFloat01Value(); break;
                case ValueType.Int: GenerateIntValue(); break;
            }
        }

        private void GenerateFloatValue()
        {
            currentValueFloat = SampleValue().RoundToDecimals(decimalPlaces);
            OnValueChangedFloat?.Invoke(currentValueFloat);
            OnValueChangedString?.Invoke(currentValueFloat.ToString());
        }

        private void GenerateFloat01Value()
        {
            float raw = SampleValue().RoundToDecimals(decimalPlaces);
            currentValueFloat = raw;
           /*  float normalized = _range > 0f
                ? Mathf.Clamp01((raw - minValue) / _range).RoundToDecimals(decimalPlaces)
                : 0f; */
            OnValueChangedFloat01?.Invoke(currentValueFloat);
            OnValueChangedString?.Invoke(currentValueFloat.ToString());
        }

        private void GenerateIntValue()
        {
            currentValueInt = Mathf.RoundToInt(SampleValue());
            OnValueChangedInt?.Invoke(currentValueInt);
            OnValueChangedSingle?.Invoke(currentValueInt);
            OnValueChangedString?.Invoke(currentValueInt.ToString());
        }

        /// <summary>
        /// 依照是否啟用權重，回傳對應的數值
        /// </summary>
        private float SampleValue()
        {
            if (!useWeightedRandom || _segmentCDF == null || _segmentCDF.Length == 0)
                return Random.Range(minValue, maxValue);

            // 用 CDF 決定落在哪一段
            float r = Random.value;
            int picked = _segmentCDF.Length - 1;
            for (int i = 0; i < _segmentCDF.Length; i++)
            {
                if (r <= _segmentCDF[i])
                {
                    picked = i;
                    break;
                }
            }

            // 在該段內均勻取樣
            float segMin = Mathf.Max(segments[picked].min, minValue);
            float segMax = Mathf.Min(segments[picked].max, maxValue);
            return Random.Range(segMin, segMax);
        }

        /// <summary>
        /// 根據各段 weight 建立 CDF，供 SampleValue 使用
        /// </summary>
        private void BakeCDF()
        {
            if (!useWeightedRandom || segments == null || segments.Length == 0)
            {
                _segmentCDF = null;
                return;
            }

            float total = 0f;
            foreach (var seg in segments)
                total += Mathf.Max(0f, seg.weight);

            _segmentCDF = new float[segments.Length];
            float cumulative = 0f;
            for (int i = 0; i < segments.Length; i++)
            {
                float w = Mathf.Max(0f, segments[i].weight);
                cumulative += total > 0f ? w / total : 1f / segments.Length;
                _segmentCDF[i] = cumulative;

                // 回寫正規化後的實際機率，Inspector 可以直接看到
                segments[i].normalizedWeight = total > 0f ? w / total : 1f / segments.Length;
            }
        }

        private void OnValidate()
        {
            string newName = $"{GetType().Name} ({valueType})";
            if (name != newName) name = newName;
            _range = maxValue - minValue;

            if (useWeightedRandom && segments != null)
            {
                foreach (var seg in segments)
                {
                    if (seg.min > seg.max)
                        Debug.LogWarning($"[{name}] 某段的 min ({seg.min}) > max ({seg.max})", this);

                    if (seg.min < minValue || seg.max > maxValue)
                        Debug.LogWarning($"[{name}] 某段範圍 [{seg.min}, {seg.max}] 超出全域範圍 [{minValue}, {maxValue}]", this);
                }
            }

            BakeCDF();
        }

        private void Awake()
        {
            _range = maxValue - minValue;
            BakeCDF();
        }

        public enum ValueType
        {
            Float,
            Float01,
            Int
        }

        [Serializable]
        public class WeightedSegment
        {
            public float min;
            public float max;
            [Tooltip("相對權重，系統會自動正規化")] 
            public float weight = 1f;
            [HideInInspector] public float normalizedWeight;
        }
    }
}